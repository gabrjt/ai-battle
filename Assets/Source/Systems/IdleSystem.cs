using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class IdleSystem : ComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Idle>
        {
            public NativeQueue<Entity>.Concurrent ProcessedQueue;
            [ReadOnly] public float DeltaTime;

            public void Execute(Entity entity, int index, ref Idle idle)
            {
                idle.Duration -= DeltaTime;

                if (idle.Duration > 0) return;

                ProcessedQueue.Enqueue(entity);
            }
        }

        private ComponentGroup m_AddGroup;
        private ComponentGroup m_ProcessGroup;
        private ComponentGroup m_RemoveGroup;
        private NativeQueue<Entity> m_ProcessedQueue;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Idle>(), ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dying>() }
            });

            m_RemoveGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Idle>() },
                Any = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>() }
            });

            m_ProcessedQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_Random = new Random(0xABCDEF);
        }

        protected override void OnUpdate()
        {
            Add();
            Process();
            Remove();
        }

        private void Add()
        {
            var chunkArray = m_AddGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    PostUpdateCommands.AddComponent(entityArray[entityIndex], new Idle
                    {
                        Duration = m_Random.NextFloat(1, 5)
                    });
                }
            }

            chunkArray.Dispose();
        }

        private void Process()
        {
            new ProcessJob
            {
                ProcessedQueue = m_ProcessedQueue.ToConcurrent(),
                DeltaTime = Time.deltaTime
            }.Schedule(this).Complete();
        }

        private void Remove()
        {
            var removeCount = m_RemoveGroup.CalculateLength();
            var removeGroupArray = m_RemoveGroup.ToEntityArray(Allocator.TempJob);
            var removeArray = new NativeArray<Entity>(removeCount + m_ProcessedQueue.Count, Allocator.Temp);

            NativeArray<Entity>.Copy(removeGroupArray, removeArray, removeCount);

            while (m_ProcessedQueue.TryDequeue(out var entity))
            {
                removeArray[removeCount++] = entity;
            }

            EntityManager.RemoveComponent(removeArray, ComponentType.ReadWrite<Idle>());

            removeArray.Dispose();
            removeGroupArray.Dispose();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_ProcessedQueue.IsCreated)
            {
                m_ProcessedQueue.Dispose();
            }
        }
    }
}