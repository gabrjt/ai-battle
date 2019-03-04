using Game.Comparers;
using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SetBarrier))]
    public partial class SearchForTargetSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        [RequireSubtractiveComponent(typeof(Dead))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<SearchingForTarget, Position, Group>
        {
            public NativeQueue<SearchForTargetData>.Concurrent DataQueue;

            [ReadOnly]
            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref SearchingForTarget searchingForTarget, [ReadOnly] ref Position position, [ReadOnly] ref Group group)
            {
                if (searchingForTarget.StartTime + searchingForTarget.Interval > Time) return;

                DataQueue.Enqueue(new SearchForTargetData
                {
                    Entity = entity,
                    SearchingForTarget = searchingForTarget,
                    Position = position,
                    Group = group
                });
            }
        }

        private struct SearchForTargetData
        {
            public Entity Entity;

            public SearchingForTarget SearchingForTarget;

            public Position Position;

            public Group Group;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private LayerMask m_LayerMask;

        private int m_Layer;

        private Random m_Random;

        private readonly ColliderDistanceComparer m_ColliderComparer = new ColliderDistanceComparer();

        private readonly EntityDistanceComparer m_TargetDistanceComparer = new EntityDistanceComparer();

        private NativeQueue<SearchForTargetData> m_DataQueue;

        private Collider[] m_CachedColliderArray = new Collider[TargetBufferProxy.InternalBufferCapacity];

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<TargetFound>());

            m_LayerMask = LayerMask.NameToLayer("Entity");
            m_Layer = 1 << m_LayerMask;

            m_Random = new Random((uint)System.Environment.TickCount);

            m_DataQueue = new NativeQueue<SearchForTargetData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                DataQueue = m_DataQueue.ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);

            inputDeps.Complete();

            while (m_DataQueue.TryDequeue(out var data))
            {
                var entity = data.Entity;
                var position = data.Position.Value;
                var searchingForTarget = data.SearchingForTarget;

                var targetBuffer = EntityManager.GetBuffer<TargetBufferElement>(entity);

                if (targetBuffer.Length >= TargetBufferProxy.InternalBufferCapacity * 5) continue;

                var count = Physics.OverlapSphereNonAlloc(position, searchingForTarget.Radius, m_CachedColliderArray, m_Layer);

                if (count > 0)
                {
                    var targetList = new NativeList<Entity>(Allocator.Temp);
                    m_ColliderComparer.Position = position;

                    Array.Sort(m_CachedColliderArray, 0, count, m_ColliderComparer);

                    var colliderIndex = 0;

                    do
                    {
                        var target = m_CachedColliderArray[colliderIndex];
                        var targetEntity = target.GetComponent<GameObjectEntity>().Entity;

                        if (EntityManager.HasComponent<Target>(entity) && EntityManager.GetComponentData<Target>(entity).Value == targetEntity)
                        {
                            break;
                        }

                        if (entity == targetEntity ||
                            !EntityManager.HasComponent<Group>(targetEntity) ||
                            data.Group.Value == EntityManager.GetComponentData<Group>(targetEntity).Value ||
                            EntityManager.HasComponent<Dead>(targetEntity)) continue;

                        targetList.Add(targetEntity);
                    }
                    while (++colliderIndex < count && targetBuffer.Length < TargetBufferProxy.InternalBufferCapacity);

                    if (targetList.Length > 0)
                    {
                        var targetEntity = targetList[0];

                        var eventBarrier = World.GetExistingManager<EventBarrier>();
                        var eventCommandBuffer = eventBarrier.CreateCommandBuffer();

                        var targetFound = eventCommandBuffer.CreateEntity(m_Archetype);
                        eventCommandBuffer.SetComponent(targetFound, new TargetFound
                        {
                            This = entity,
                            Other = targetEntity
                        });

                        eventBarrier.AddJobHandleForProducer(inputDeps);

                        if (targetBuffer.Length == 0)
                        {
                            for (var targetIndex = 0; targetIndex < targetList.Length; targetIndex++)
                            {
                                targetBuffer.Add(new TargetBufferElement { Value = targetList[targetIndex] });
                            }
                        }
                        else
                        {
                            for (var targetIndex = 0; targetIndex < targetList.Length; targetIndex++)
                            {
                                var target = targetList[targetIndex];

                                for (var bufferIndex = 0; bufferIndex < targetBuffer.Length; bufferIndex++)
                                {
                                    if (targetBuffer[bufferIndex].Value == target) continue;

                                    targetBuffer.Add(new TargetBufferElement { Value = target });

                                    break;
                                }
                            }
                        }
                    }

                    targetList.Dispose();
                    Array.Clear(m_CachedColliderArray, 0, count);
                }
                else
                {
                    var setBarrier = World.GetExistingManager<SetBarrier>();
                    searchingForTarget.StartTime = Time.time;
                    setBarrier.CreateCommandBuffer().SetComponent(entity, searchingForTarget);

                    setBarrier.AddJobHandleForProducer(inputDeps);
                }
            }

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_DataQueue.IsCreated)
            {
                m_DataQueue.Dispose();
            }
        }
    }
}