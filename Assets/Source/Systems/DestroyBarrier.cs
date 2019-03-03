using Game.Components;
using Game.Enums;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateAfter(typeof(EventBarrier))]
    public class DestroyBarrier : BarrierSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobParallelFor
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            public NativeQueue<Entity>.Concurrent CharacterQueue;

            public NativeQueue<Entity>.Concurrent KnightQueue;

            public NativeQueue<Entity>.Concurrent OrcWolfRiderQueue;

            public NativeQueue<Entity>.Concurrent SkeletonQueue;

            public NativeQueue<Entity>.Concurrent HealthBarQueue;

            public NativeQueue<Entity>.Concurrent PureEntityQueue;

            [ReadOnly]
            public ComponentDataFromEntity<Character> CharacterFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<View> ViewFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<HealthBar> HealthBarFromEntity;

            public void Execute(int index)
            {
                var entity = EntityArray[index];

                if (CharacterFromEntity.Exists(entity))
                {
                    CharacterQueue.Enqueue(entity);
                }
                else if (ViewFromEntity.Exists(entity))
                {
                    var view = ViewFromEntity[entity];

                    switch (view.Value)
                    {
                        case ViewType.Knight:
                            KnightQueue.Enqueue(entity);
                            break;

                        case ViewType.OrcWolfRider:
                            OrcWolfRiderQueue.Enqueue(entity);
                            break;

                        case ViewType.Skeleton:
                            SkeletonQueue.Enqueue(entity);
                            break;
                    }
                }
                else if (HealthBarFromEntity.Exists(entity))
                {
                    HealthBarQueue.Enqueue(entity);
                }
                else
                {
                    PureEntityQueue.Enqueue(entity);
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_PureEntityQueue;

        private NativeQueue<Entity> m_CharacterQueue;

        private NativeQueue<Entity> m_KnightQueue;

        private NativeQueue<Entity> m_OrcWolfRiderQueue;

        private NativeQueue<Entity> m_SkeletonQueue;

        private NativeQueue<Entity> m_HealthBarQueue;

        internal Queue<GameObject> m_CharacterPool;

        internal Queue<GameObject> m_KnightPool;

        internal Queue<GameObject> m_OrcWolfRiderPool;

        internal Queue<GameObject> m_SkeletonPool;

        internal Queue<GameObject> m_HealthBarPool;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>() },
                None = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<View>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<HealthBar>() },
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<View>() },
            });

            m_PureEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_CharacterQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_KnightQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_OrcWolfRiderQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_SkeletonQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_HealthBarQueue = new NativeQueue<Entity>(Allocator.Persistent);

            m_CharacterPool = new Queue<GameObject>();
            m_KnightPool = new Queue<GameObject>();
            m_OrcWolfRiderPool = new Queue<GameObject>();
            m_SkeletonPool = new Queue<GameObject>();
            m_HealthBarPool = new Queue<GameObject>();
        }

        protected override void OnUpdate()
        {
            new ConsolidateJob
            {
                EntityArray = m_Group.ToEntityArray(Allocator.TempJob),
                PureEntityQueue = m_PureEntityQueue.ToConcurrent(),
                CharacterQueue = m_CharacterQueue.ToConcurrent(),
                KnightQueue = m_KnightQueue.ToConcurrent(),
                OrcWolfRiderQueue = m_OrcWolfRiderQueue.ToConcurrent(),
                SkeletonQueue = m_SkeletonQueue.ToConcurrent(),
                HealthBarQueue = m_HealthBarQueue.ToConcurrent(),
                CharacterFromEntity = GetComponentDataFromEntity<Character>(true),
                ViewFromEntity = GetComponentDataFromEntity<View>(true),
                HealthBarFromEntity = GetComponentDataFromEntity<HealthBar>(true)
            }.Schedule(m_Group.CalculateLength(), 64).Complete();

            while (m_PureEntityQueue.TryDequeue(out var entity))
            {
                EntityManager.DestroyEntity(entity);
            }

            while (m_CharacterQueue.TryDequeue(out var entity))
            {
                var gameObject = EntityManager.GetComponentObject<NavMeshAgent>(entity).gameObject;
                m_CharacterPool.Enqueue(gameObject);
                ApplyToPool(gameObject);
            }

            while (m_KnightQueue.TryDequeue(out var entity))
            {
                var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;
                m_KnightPool.Enqueue(gameObject);
                ApplyToPool(gameObject);
            }

            while (m_OrcWolfRiderQueue.TryDequeue(out var entity))
            {
                var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;
                m_OrcWolfRiderPool.Enqueue(gameObject);
                ApplyToPool(gameObject);
            }

            while (m_SkeletonQueue.TryDequeue(out var entity))
            {
                var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;
                m_SkeletonPool.Enqueue(gameObject);
                ApplyToPool(gameObject);
            }

            while (m_HealthBarQueue.TryDequeue(out var entity))
            {
                var gameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject;
                m_HealthBarPool.Enqueue(gameObject);
                ApplyToPool(gameObject);
            }

            var maxPoolCount = World.GetExistingManager<SpawnAICharacterSystem>().m_TotalCount * 2;

            while (m_CharacterPool.Count > maxPoolCount)
            {
                Object.Destroy(m_CharacterPool.Dequeue());
            }

            while (m_KnightPool.Count > maxPoolCount)
            {
                Object.Destroy(m_KnightPool.Dequeue());
            }

            while (m_OrcWolfRiderPool.Count > maxPoolCount)
            {
                Object.Destroy(m_OrcWolfRiderPool.Dequeue());
            }

            while (m_SkeletonPool.Count > maxPoolCount)
            {
                Object.Destroy(m_SkeletonPool.Dequeue());
            }

            while (m_HealthBarPool.Count > maxPoolCount)
            {
                Object.Destroy(m_HealthBarPool.Dequeue());
            }

            base.OnUpdate();
        }

        private void ApplyToPool(GameObject gameObject)
        {
            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_PureEntityQueue.IsCreated)
            {
                m_PureEntityQueue.Dispose();
            }

            if (m_CharacterQueue.IsCreated)
            {
                m_CharacterQueue.Dispose();
            }

            if (m_KnightQueue.IsCreated)
            {
                m_KnightQueue.Dispose();
            }

            if (m_OrcWolfRiderQueue.IsCreated)
            {
                m_OrcWolfRiderQueue.Dispose();
            }

            if (m_SkeletonQueue.IsCreated)
            {
                m_SkeletonQueue.Dispose();
            }

            if (m_HealthBarQueue.IsCreated)
            {
                m_HealthBarQueue.Dispose();
            }

            m_CharacterPool.Clear();
            m_KnightPool.Clear();
            m_OrcWolfRiderPool.Clear();
            m_SkeletonPool.Clear();
            m_HealthBarPool.Clear();
        }
    }
}