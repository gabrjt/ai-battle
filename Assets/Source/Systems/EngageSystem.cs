﻿using Game.Components;
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
        [RequireComponentTag(typeof(Character))]
        [ExcludeComponent(typeof(Dead))]
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
        [RequireComponentTag(typeof(Character))]
        [ExcludeComponent(typeof(Dead))]
        private struct EngageNearestTargetJob : IJobProcessComponentDataWithEntity<Translation, EngageSqrRadius, Faction>
        {
            [ReadOnly] public NativeMultiHashMap<int2, Entity> NodeMap;
            [ReadOnly] public NativeHashMap<Entity, float3> TranslationMap;
            public NativeArray<Entity> EntityArray;
            public NativeArray<Entity> TargetArray;
            public NativeArray<@bool> EngagedArray;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Target> TargetFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Faction> FactionFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Dead> DeadFromEntity;
            [ReadOnly] public float NodeSize;

            public void Execute(Entity entity, int index,
                [ReadOnly] ref Translation translation,
                [ReadOnly] ref EngageSqrRadius engageSqrRadius,
                [ReadOnly] ref Faction faction)
            {
                var radius = math.sqrt(engageSqrRadius.Value);
                var nodeRadius = (int)math.ceil(radius / NodeSize);
                var node = (int2)(translation.Value / NodeSize).xz;
                var maxNodeX = node.x + nodeRadius;
                var maxNodeY = node.y + nodeRadius;
                var targetSqrDistance = engageSqrRadius.Value;

                EntityArray[index] = entity;

                for (var x = node.x - nodeRadius; x <= maxNodeX; x++)
                {
                    for (var y = node.y - nodeRadius; y <= maxNodeY; y++)
                    {
                        if (NodeMap.TryGetFirstValue(new int2(x, y), out var targetEntity, out var iterator))
                        {
                            do
                            {
                                //if (targetEntity != entity && faction.Value != FactionFromEntity[targetEntity].Value && TranslationMap.TryGetValue(targetEntity, out var targetTranslation))
                                if (targetEntity != entity && !DeadFromEntity.Exists(targetEntity) && TranslationMap.TryGetValue(targetEntity, out var targetTranslation))
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
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> TargetArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<@bool> EngagedArray;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Target> TargetFromEntity;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (EngagedArray[index])
                {
                    var entity = EntityArray[index];
                    if (TargetFromEntity.Exists(entity))
                    {
                        TargetFromEntity[entity] = new Target { Value = TargetArray[index] };
                    }
                    else
                    {
                        CommandBuffer.AddComponent(m_ThreadIndex, entity, new Target { Value = TargetArray[index] });
                    }
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
                None = new ComponentType[] { ComponentType.ReadOnly<Dead>() }
            });

            m_TargetGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Translation>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Dead>() }
            });

            RequireForUpdate(m_Group);
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
            if (count > 0)
            {
                var entityArray = new NativeArray<Entity>(count, Allocator.TempJob);
                var targetEntityArray = new NativeArray<Entity>(count, Allocator.TempJob);
                var engagedArray = new NativeArray<@bool>(count, Allocator.TempJob);
                var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();
                var commandBuffer = commandBufferSystem.CreateCommandBuffer();

                inputDeps = new ConsolidateNodesJob
                {
                    NodeMap = m_NodeMap.ToConcurrent(),
                    TranslationMap = m_TranslationMap.ToConcurrent(),
                    NodeSize = NodeSize
                }.Schedule(this, inputDeps);

                inputDeps = new EngageNearestTargetJob
                {
                    NodeMap = m_NodeMap,
                    TranslationMap = m_TranslationMap,
                    EntityArray = entityArray,
                    TargetArray = targetEntityArray,
                    EngagedArray = engagedArray,
                    TargetFromEntity = GetComponentDataFromEntity<Target>(),
                    FactionFromEntity = GetComponentDataFromEntity<Faction>(true),
                    DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                    NodeSize = NodeSize
                }.Schedule(this, inputDeps);

                inputDeps = new AddTargetJob
                {
                    CommandBuffer = commandBuffer.ToConcurrent(),
                    EntityArray = entityArray,
                    TargetArray = targetEntityArray,
                    EngagedArray = engagedArray,
                    TargetFromEntity = GetComponentDataFromEntity<Target>()
                }.Schedule(count, 64, inputDeps);

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
        }
    }
}