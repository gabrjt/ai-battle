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
    public partial class SearchForTargetSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Dead))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<SearchingForTarget, Translation, Group>
        {
            public NativeQueue<SearchForTargetData>.Concurrent DataQueue;

            [ReadOnly]
            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref SearchingForTarget searchingForTarget, [ReadOnly] ref Translation translation, [ReadOnly] ref Group group)
            {
                if (searchingForTarget.StartTime + searchingForTarget.Interval > Time) return;

                DataQueue.Enqueue(new SearchForTargetData
                {
                    Entity = entity,
                    SearchingForTarget = searchingForTarget,
                    Translation = translation,
                    Group = group
                });
            }
        }

        private struct SearchForTargetData
        {
            public Entity Entity;

            public SearchingForTarget SearchingForTarget;

            public Translation Translation;

            public Group Group;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private LayerMask m_LayerMask;

        private int m_Layer;

        private Random m_Random;

        private NativeQueue<SearchForTargetData> m_DataQueue;

        private Collider[] m_CachedColliderArray = new Collider[TargetBufferProxy.InternalBufferCapacity];

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<TargetFound>());

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

            var targetBufferFromEntity = GetBufferFromEntity<TargetBuffer>();

            while (m_DataQueue.TryDequeue(out var data))
            {
                var entity = data.Entity;
                var translation = data.Translation.Value;
                var searchingForTarget = data.SearchingForTarget;

                var targetBuffer = targetBufferFromEntity[entity];

                if (targetBuffer.Length >= TargetBufferProxy.InternalBufferCapacity) continue;

                var count = Physics.OverlapSphereNonAlloc(translation, searchingForTarget.Radius, m_CachedColliderArray, m_Layer);

                if (count > 0)
                {
                    var targetList = new NativeList<Entity>(Allocator.Temp);

                    var targetFromEntity = GetComponentDataFromEntity<Target>(true);
                    var groupFromEntity = GetComponentDataFromEntity<Group>(true);
                    var deadFromEntity = GetComponentDataFromEntity<Dead>(true);

                    var colliderIndex = 0;
                    do
                    {
                        var target = m_CachedColliderArray[colliderIndex];
                        var targetEntity = target.GetComponent<GameObjectEntity>().Entity;

                        if (entity == targetEntity ||
                            !groupFromEntity.Exists(targetEntity) ||
                            data.Group.Value == groupFromEntity[targetEntity].Value ||
                            deadFromEntity.Exists(targetEntity)) continue;

                        targetList.Add(targetEntity);
                    }
                    while (++colliderIndex < count);

                    if (targetList.Length > 0)
                    {
                        if (targetBuffer.Length == 0)
                        {
                            for (var targetIndex = 0; targetIndex < targetList.Length; targetIndex++)
                            {
                                targetBuffer.Add(new TargetBuffer { Value = targetList[targetIndex] });
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

                                    targetBuffer.Add(new TargetBuffer { Value = target });

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
                    var setSystem = World.GetExistingManager<SetCommandBufferSystem>();
                    searchingForTarget.StartTime = Time.time;

                    setSystem.CreateCommandBuffer().SetComponent(entity, searchingForTarget);

                    setSystem.AddJobHandleForProducer(inputDeps);
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