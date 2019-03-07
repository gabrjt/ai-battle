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
    [UpdateInGroup(typeof(LogicGroup))]
    public class SearchForTargetSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Dead))]
        private struct ConsolidateMapJob : IJobProcessComponentDataWithEntity<Translation>
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
        [ExcludeComponent(typeof(Target), typeof(Dead))]
        private struct FindTargetJob : IJobProcessComponentDataWithEntity<Translation, SearchingForTarget>
        {
            [ReadOnly] public NativeMultiHashMap<int2, Entity> NodeMap;
            [ReadOnly] public NativeHashMap<Entity, float3> TranslationMap;
            public NativeArray<Entity> EntityArray;
            public NativeArray<Entity> TargetArray;
            public NativeArray<@bool> EngagedArray;
            [ReadOnly] public float NodeSize;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref SearchingForTarget searchingForTarget)
            {
                var radius = math.sqrt(searchingForTarget.SqrRadius);
                var nodeRadius = (int)math.ceil(radius / NodeSize);
                var node = (int2)(translation.Value / NodeSize).xz;
                var maxNodeX = node.x + nodeRadius;
                var maxNodeY = node.y + nodeRadius;
                var targetSqrDistance = searchingForTarget.SqrRadius;

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
                                    var sqrDistance = math.lengthsq(targetTranslation - translation.Value);

                                    if (sqrDistance < targetSqrDistance)
                                    {
                                        TargetArray[index] = target;
                                        targetSqrDistance = sqrDistance;
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
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] public NativeArray<Entity> TargetArray;
            [DeallocateOnJobCompletion] public NativeArray<@bool> EngagedArray;
            [ReadOnly] public EntityArchetype Archetype;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (EngagedArray[index])
                {
                    var entity = EntityArray[index];
                    var target = TargetArray[index];

                    var targetFound = CommandBuffer.CreateEntity(m_ThreadIndex, Archetype);
                    CommandBuffer.SetComponent(m_ThreadIndex, targetFound, new TargetFound
                    {
                        This = entity,
                        Other = target
                    });
                }
            }
        }

        private ComponentGroup m_Group;
        private ComponentGroup m_TargetGroup;
        private EntityArchetype m_Arhcetype;
        private NativeMultiHashMap<int2, Entity> m_NodeMap;
        private NativeHashMap<Entity, float3> m_TranslationMap;
        private int m_Capacity;
        private const float NodeSize = 100;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<SearchingForTarget>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            });

            m_TargetGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Translation>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Dead>() }
            });

            m_Arhcetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<TargetFound>());

            RequireForUpdate(m_Group);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var targetCount = m_TargetGroup.CalculateLength();

            if (m_Capacity < targetCount)
            {
                Dispose();

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
            var eventCommandBufferSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateMapJob
            {
                NodeMap = m_NodeMap.ToConcurrent(),
                TranslationMap = m_TranslationMap.ToConcurrent(),
                NodeSize = NodeSize
            }.Schedule(this, inputDeps);

            inputDeps = new FindTargetJob
            {
                NodeMap = m_NodeMap,
                TranslationMap = m_TranslationMap,
                EntityArray = entityArray,
                TargetArray = targetArray,
                EngagedArray = engagedArray,
                NodeSize = NodeSize
            }.Schedule(this, inputDeps);

            inputDeps = new AddTarget
            {
                CommandBuffer = eventCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityArray = entityArray,
                TargetArray = targetArray,
                EngagedArray = engagedArray,
                Archetype = m_Arhcetype,
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(count, 64, inputDeps);

            eventCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            Dispose();
        }

        private void Dispose()
        {
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