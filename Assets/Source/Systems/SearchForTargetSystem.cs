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
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<SearchingForTarget, Position>
        {
            public NativeQueue<SearchForTargetData>.Concurrent DataQueue;

            [ReadOnly]
            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref SearchingForTarget searchingForTarget, [ReadOnly] ref Position position)
            {
                if (searchingForTarget.StartTime + searchingForTarget.Interval > Time) return;

                DataQueue.Enqueue(new SearchForTargetData
                {
                    Entity = entity,
                    SearchingForTarget = searchingForTarget,
                    Position = position
                });
            }
        }

        private struct SearchForTargetData
        {
            public Entity Entity;

            public SearchingForTarget SearchingForTarget;

            public Position Position;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private LayerMask m_LayerMask;

        private int m_Layer;

        private Random m_Random;

        private readonly ColliderDistanceComparer m_Comparer = new ColliderDistanceComparer();

        private NativeQueue<SearchForTargetData> m_DataQueue;

        private Collider[] m_CachedColliderArray = new Collider[10];

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

            var eventBarrier = World.GetExistingManager<EventBarrier>();
            var eventCommandBuffer = eventBarrier.CreateCommandBuffer();

            var foundTarget = false;

            while (m_DataQueue.TryDequeue(out var data))
            {
                var entity = data.Entity;
                var position = data.Position.Value;
                var searchingForTarget = data.SearchingForTarget;

                var count = Physics.OverlapSphereNonAlloc(position, searchingForTarget.Radius, m_CachedColliderArray, m_Layer);

                if (count > 0)
                {
                    m_Comparer.Position = position;

                    Array.Sort(m_CachedColliderArray, 0, count, m_Comparer);

                    var colliderIndex = 0;

                    do
                    {
                        var target = m_CachedColliderArray[colliderIndex];
                        var targetEntity = target.GetComponent<GameObjectEntity>().Entity;

                        if (entity == targetEntity || EntityManager.HasComponent<Dead>(targetEntity) || EntityManager.HasComponent<Destroy>(targetEntity)) continue;

                        var targetFound = eventCommandBuffer.CreateEntity(m_Archetype);
                        eventCommandBuffer.SetComponent(targetFound, new TargetFound
                        {
                            This = entity,
                            Other = targetEntity
                        });

                        foundTarget = true;

                        break;
                    }
                    while (++colliderIndex < count);

                    Array.Clear(m_CachedColliderArray, 0, count);
                }
                else
                {
                    searchingForTarget.StartTime = Time.time;
                    EntityManager.SetComponentData(entity, searchingForTarget);
                }
            }

            if (foundTarget)
            {
                eventBarrier.AddJobHandleForProducer(inputDeps);
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