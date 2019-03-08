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
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class EngageSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Dying))]
        private struct MakeNodesJob : IJobProcessComponentDataWithEntity<Translation>
        {
            public NativeMultiHashMap<int2, Entity>.Concurrent NodeMap;
            public NativeHashMap<Entity, float3>.Concurrent TranslationMap;
            [ReadOnly] public float NodeSize;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
            {
                var node = (int2)(translation.Value / NodeSize).xz;

                NodeMap.Add(node, entity);
                TranslationMap.TryAdd(entity, translation.Value);
            }
        }

        [BurstCompile]
        [RequireComponentTag(typeof(Character))]
        [ExcludeComponent(typeof(Target))]
        private struct EngageJob : IJobProcessComponentDataWithEntity<Translation, EngageSqrRadius>
        {
            [ReadOnly] public NativeMultiHashMap<int2, Entity> NodeMap;
            [ReadOnly] public NativeHashMap<Entity, float3> TranslationMap;
            public NativeArray<Entity> EntityArray;
            public NativeArray<Entity> TargetArray;
            public NativeArray<@bool> EngagedArray;
            [ReadOnly] public float NodeSize;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref EngageSqrRadius engageSqrRadius)
            {
                var radius = math.sqrt(engageSqrRadius.Value);
                var nodeRadius = (int)math.ceil(radius / NodeSize);
                var node = (int2)(translation.Value / NodeSize).xz;
                var maxNodeX = node.x + nodeRadius;
                var maxNodeY = node.y + nodeRadius;
                var targetSqrDistance = engageSqrRadius.Value;

                EntityArray[index] = entity;

                for (var x = node.x - nodeRadius; x < maxNodeX; x++)
                {
                    for (var y = node.y - nodeRadius; y < maxNodeY; y++)
                    {
                        if (NodeMap.TryGetFirstValue(new int2(x, y), out var targetEntity, out var iterator))
                        {
                            do
                            {
                                if (TranslationMap.TryGetValue(targetEntity, out var targetTranslation) && targetEntity != entity)
                                {
                                    var sqrDistance = math.lengthsq(targetTranslation - translation.Value);

                                    if (sqrDistance < targetSqrDistance)
                                    {
                                        TargetArray[index] = targetEntity;
                                        targetSqrDistance = sqrDistance;
                                        EngagedArray[index] = true;
                                    }
                                }
                            }
                            while (NodeMap.TryGetNextValue(out targetEntity, ref iterator));
                        }
                    }
                }
            }
        }

        private struct AddTargetJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] public NativeArray<Entity> TargetEntityArray;
            [DeallocateOnJobCompletion] public NativeArray<@bool> EngagedArray;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (EngagedArray[index])
                {
                    var entity = EntityArray[index];
                    var target = TargetEntityArray[index];

                    CommandBuffer.AddComponent(m_ThreadIndex, entity, new Target
                    {
                        Value = target
                    });
                }
            }
        }

        private ComponentGroup m_Group;
        private ComponentGroup m_TargetGroup;
        private NativeMultiHashMap<int2, Entity> m_NodeMap;
        private NativeHashMap<Entity, float3> m_TranslationMap;
        private const float NodeSize = 100;
        private int m_Capacity;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<EngageSqrRadius>() },
                None = new ComponentType[] { ComponentType.ReadWrite<Target>(), ComponentType.ReadOnly<Dying>() }
            });

            m_TargetGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Translation>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Dying>() }
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
            var targetEntityArray = new NativeArray<Entity>(count, Allocator.TempJob);
            var engagedArray = new NativeArray<@bool>(count, Allocator.TempJob);
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();
            var commandBuffer = commandBufferSystem.CreateCommandBuffer();

            inputDeps = new MakeNodesJob
            {
                NodeSize = NodeSize,
                NodeMap = m_NodeMap.ToConcurrent(),
                TranslationMap = m_TranslationMap.ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps = new EngageJob
            {
                NodeSize = NodeSize,
                NodeMap = m_NodeMap,
                TranslationMap = m_TranslationMap,
                EntityArray = entityArray,
                TargetArray = targetEntityArray,
                EngagedArray = engagedArray
            }.Schedule(this, inputDeps);

            inputDeps = new AddTargetJob
            {
                EntityArray = entityArray,
                TargetEntityArray = targetEntityArray,
                EngagedArray = engagedArray,
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true),
                CommandBuffer = commandBuffer.ToConcurrent()
            }.Schedule(count, 64, inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}