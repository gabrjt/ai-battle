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
    public class VisibleSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Character))]
        [ExcludeComponent(typeof(Destroy), typeof(Disabled))]
        private struct ConsolidateNodesJob : IJobProcessComponentDataWithEntity<Translation>
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
        private struct FindVisibleJob : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int2, Entity> NodeMap;
            [ReadOnly] public NativeHashMap<Entity, float3> TranslationMap;
            public NativeHashMap<Entity, @bool>.Concurrent VisibleMap;
            [ReadOnly] public ComponentDataFromEntity<ViewVisible> ViewVisibleFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Destroy> DestroyFromEntity;
            [ReadOnly] public float3 CameraTranslation;
            [ReadOnly] public float NodeSize;
            [ReadOnly] public float MaxViewLODSqrDistance;

            public void Execute(int index)
            {
                var radius = math.sqrt(MaxViewLODSqrDistance);
                var nodeRadius = (int)math.ceil(radius / NodeSize);
                var node = (int2)(CameraTranslation / NodeSize).xz;
                var maxNodeX = node.x + nodeRadius;
                var maxNodeY = node.y + nodeRadius;
                var maxLODSqrDistance = MaxViewLODSqrDistance;

                for (var x = node.x - nodeRadius; x <= maxNodeX; x++)
                {
                    for (var y = node.y - nodeRadius; y <= maxNodeY; y++)
                    {
                        if (NodeMap.TryGetFirstValue(new int2(x, y), out var entity, out var iterator))
                        {
                            do
                            {
                                if (TranslationMap.TryGetValue(entity, out var translation))
                                {
                                    var sqrDistance = math.lengthsq(translation - CameraTranslation);

                                    if (sqrDistance <= maxLODSqrDistance && !ViewVisibleFromEntity.Exists(entity) && !DestroyFromEntity.Exists(entity))
                                    {
                                        VisibleMap.TryAdd(entity, true);
                                    }
                                    else if (sqrDistance > maxLODSqrDistance && ViewVisibleFromEntity.Exists(entity) && !DestroyFromEntity.Exists(entity))
                                    {
                                        VisibleMap.TryAdd(entity, false);
                                    }
                                }
                            }
                            while (NodeMap.TryGetNextValue(out entity, ref iterator));
                        }
                    }
                }
            }
        }

        private struct VisibleJob : IJob
        {
            [ReadOnly] public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public NativeHashMap<Entity, @bool> VisibleMap;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute()
            {
                var entityArray = VisibleMap.GetKeyArray(Allocator.Temp);
                var visibleArray = VisibleMap.GetValueArray(Allocator.Temp);

                for (int index = 0; index < visibleArray.Length; index++)
                {
                    if (visibleArray[index])
                    {
                        CommandBuffer.AddComponent(entityArray[index], new ViewVisible());
                    }
                    else
                    {
                        CommandBuffer.RemoveComponent<ViewVisible>(entityArray[index]);
                    }
                }

                entityArray.Dispose();
                visibleArray.Dispose();
            }
        }

        private ComponentGroup m_CameraGroup;
        private ComponentGroup m_VisibleGroup;
        private NativeMultiHashMap<int2, Entity> m_NodeMap;
        private NativeHashMap<Entity, float3> m_TranslationMap;
        private NativeHashMap<Entity, @bool> m_VisibleMap;
        private const float NodeSize = 100;
        private int m_Capacity;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_CameraGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<CameraArm>(), ComponentType.ReadOnly<Translation>() }
            });

            m_VisibleGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Translation>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Destroy>() }
            });

            RequireSingletonForUpdate<CameraArm>();
            RequireSingletonForUpdate<MaxViewLODSqrDistance>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var maxViewLODSqrDistance = GetSingleton<MaxViewLODSqrDistance>().Value;

            if (maxViewLODSqrDistance == 0) return inputDeps;

            var visibleCount = m_VisibleGroup.CalculateLength();

            if (m_Capacity < visibleCount)
            {
                if (m_NodeMap.IsCreated)
                {
                    m_NodeMap.Dispose();
                }

                if (m_TranslationMap.IsCreated)
                {
                    m_TranslationMap.Dispose();
                }

                if (m_VisibleMap.IsCreated)
                {
                    m_VisibleMap.Dispose();
                }

                m_Capacity = math.max(100, visibleCount + visibleCount >> 1);
                m_NodeMap = new NativeMultiHashMap<int2, Entity>(m_Capacity, Allocator.Persistent);
                m_TranslationMap = new NativeHashMap<Entity, float3>(m_Capacity, Allocator.Persistent);
                m_VisibleMap = new NativeHashMap<Entity, @bool>(m_Capacity, Allocator.Persistent);
            }
            else
            {
                m_NodeMap.Clear();
                m_TranslationMap.Clear();
                m_VisibleMap.Clear();
            }

            var count = m_VisibleGroup.CalculateLength();
            if (count > 0)
            {
                var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();
                var commandBuffer = commandBufferSystem.CreateCommandBuffer();
                var cameraArray = m_CameraGroup.ToEntityArray(Allocator.TempJob);
                var cameraTranslation = EntityManager.GetComponentData<Translation>(cameraArray[0]).Value;
                cameraArray.Dispose();

                inputDeps = new ConsolidateNodesJob
                {
                    NodeMap = m_NodeMap.ToConcurrent(),
                    TranslationMap = m_TranslationMap.ToConcurrent(),
                    NodeSize = NodeSize
                }.Schedule(this, inputDeps);

                inputDeps = new FindVisibleJob
                {
                    NodeMap = m_NodeMap,
                    TranslationMap = m_TranslationMap,
                    VisibleMap = m_VisibleMap.ToConcurrent(),
                    ViewVisibleFromEntity = GetComponentDataFromEntity<ViewVisible>(true),
                    DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true),
                    CameraTranslation = cameraTranslation,
                    NodeSize = NodeSize,
                    MaxViewLODSqrDistance = maxViewLODSqrDistance
                }.Schedule(count, 64, inputDeps);

                inputDeps = new VisibleJob
                {
                    CommandBuffer = commandBuffer,
                    VisibleMap = m_VisibleMap
                }.Schedule(inputDeps);

                commandBufferSystem.AddJobHandleForProducer(inputDeps);
            }

            return inputDeps;
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

            if (m_VisibleMap.IsCreated)
            {
                m_VisibleMap.Dispose();
            }
        }
    }
}