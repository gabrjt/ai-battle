using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class SearchForEngageSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Dead))]
        private struct GroupNode : IJobProcessComponentDataWithEntity<Translation>
        {
            public float NodeSize;
            public NativeMultiHashMap<int2, Entity>.Concurrent NodeMap;
            public NativeHashMap<Entity, float3>.Concurrent TranslationMap;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
            {
                var node = (int2)(translation.Value / NodeSize).xz;

                NodeMap.Add(node, entity);
                TranslationMap.TryAdd(entity, translation.Value);
            }
        }

        [BurstCompile]
        [ExcludeComponent(typeof(Target), typeof(Dead))]
        private struct CheckTarget : IJobProcessComponentDataWithEntity<Translation, SearchingForTarget>
        {
            public float NodeSize;

            [ReadOnly]
            public NativeMultiHashMap<int2, Entity> NodeMap;

            [ReadOnly]
            public NativeHashMap<Entity, float3> TranslationMap;

            public NativeArray<Entity> EntityArray;
            public NativeArray<Entity> TargetArray;
            public NativeArray<@bool> EngagedArray;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref SearchingForTarget targetFilter)
            {
                var radius = math.sqrt(targetFilter.SqrRadius);
                var nodeRadius = (int)math.ceil(radius / NodeSize);
                var node = (int2)(translation.Value / NodeSize).xz;
                var maxNodeX = node.x + nodeRadius;
                var maxNodeY = node.y + nodeRadius;
                var selectedTargetSqrDistance = targetFilter.SqrRadius;

                EntityArray[index] = entity;

                for (var x = node.x - nodeRadius; x < maxNodeX; x++)
                {
                    for (var y = node.y - nodeRadius; y < maxNodeY; y++)
                    {
                        if (NodeMap.TryGetFirstValue(new int2(x, y), out var target, out var iterator))
                        {
                            do
                            {
                                if (TranslationMap.TryGetValue(target, out var targetTranslation) && target != entity)
                                {
                                    var targetSqrDistance = math.lengthsq(targetTranslation - translation.Value);

                                    if (targetSqrDistance < selectedTargetSqrDistance)
                                    {
                                        TargetArray[index] = target;
                                        selectedTargetSqrDistance = targetSqrDistance;
                                        EngagedArray[index] = true;
                                    }
                                }
                            }
                            while (NodeMap.TryGetNextValue(out target, ref iterator));
                        }
                    }
                }
            }
        }

        private struct AddTarget : IJobParallelFor
        {
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            [DeallocateOnJobCompletion]
            public NativeArray<Entity> TargetArray;

            [DeallocateOnJobCompletion]
            public NativeArray<@bool> EngagedArray;

            [ReadOnly]
            public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public EntityCommandBuffer.Concurrent CommandBuffer;

            [NativeSetThreadIndex]
            private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (EngagedArray[index])
                {
                    var entity = EntityArray[index];
                    var target = TargetArray[index];

                    var targetFound = CommandBuffer.CreateEntity(m_ThreadIndex);
                    CommandBuffer.AddComponent(m_ThreadIndex, targetFound, new Event());
                    CommandBuffer.AddComponent(m_ThreadIndex, targetFound, new TargetFound
                    {
                        This = entity,
                        Other = target
                    });
                }
            }
        }

        private const float NodeSize = 100;

        private int m_Capacity;
        private NativeMultiHashMap<int2, Entity> m_NodeMap;
        private NativeHashMap<Entity, float3> m_TranslationMap;

        private ComponentGroup m_Group;
        private ComponentGroup m_TargetGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Character>(),
                    ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<SearchingForTarget>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            });

            m_TargetGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Health>(), ComponentType.ReadOnly<Translation>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Dead>() }
            });

            RequireForUpdate(m_Group);
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_NodeMap.IsCreated)
            {
                m_NodeMap.Dispose();
            }

            if (m_TranslationMap.IsCreated)
            {
                m_TranslationMap.Dispose();
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var targetCount = m_TargetGroup.CalculateLength();

            if (m_Capacity < targetCount)
            {
                if (m_NodeMap.IsCreated)
                {
                    m_NodeMap.Dispose();
                }

                if (m_TranslationMap.IsCreated)
                {
                    m_TranslationMap.Dispose();
                }

                m_Capacity = math.max(100, targetCount + targetCount >> 1);
                m_NodeMap = new NativeMultiHashMap<int2, Entity>(m_Capacity, Allocator.Persistent);
                m_TranslationMap = new NativeHashMap<Entity, float3>(m_Capacity, Allocator.Persistent);
            }
            else
            {
                m_NodeMap.Clear();
                m_TranslationMap.Clear();
            }

            var count = m_Group.CalculateLength();
            var entityArray = new NativeArray<Entity>(count, Allocator.TempJob);
            var targetArray = new NativeArray<Entity>(count, Allocator.TempJob);
            var engagedArray = new NativeArray<@bool>(count, Allocator.TempJob);
            var barrier = World.GetExistingManager<EventCommandBufferSystem>();
            var commandBuffer = barrier.CreateCommandBuffer();

            inputDeps = new GroupNode
            {
                NodeSize = NodeSize,
                NodeMap = m_NodeMap.ToConcurrent(),
                TranslationMap = m_TranslationMap.ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps = new CheckTarget
            {
                NodeSize = NodeSize,
                NodeMap = m_NodeMap,
                TranslationMap = m_TranslationMap,
                EntityArray = entityArray,
                TargetArray = targetArray,
                EngagedArray = engagedArray
            }.Schedule(this, inputDeps);

            inputDeps = new AddTarget
            {
                EntityArray = entityArray,
                TargetArray = targetArray,
                EngagedArray = engagedArray,
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true),
                CommandBuffer = commandBuffer.ToConcurrent()
            }.Schedule(count, 64, inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}