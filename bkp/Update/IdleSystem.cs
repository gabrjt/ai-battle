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
        private struct ProcessJob : IJobProcessComponentDataWithEntity<IdleDuration>
        {
            public NativeQueue<Entity>.Concurrent ProcessedQueue;
            [ReadOnly] public float DeltaTime;

            public void Execute(Entity entity, int index, ref IdleDuration idleDuration)
            {
                idleDuration.Value -= DeltaTime;

                if (idleDuration.Value > 0) return;

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
            EntityManager.AddComponent(m_AddGroup, ComponentType.ReadWrite<Idle>());

            Entities.WithAll<Idle>().WithNone<IdleDuration>().ForEach((Entity entity) =>
            {
                PostUpdateCommands.AddComponent(entity, new IdleDuration { Value = m_Random.NextFloat(1, 5) });
            });

            new ProcessJob
            {
                ProcessedQueue = m_ProcessedQueue.ToConcurrent(),
                DeltaTime = Time.deltaTime
            }.Schedule(this).Complete();

            var removeCount = m_RemoveGroup.CalculateLength();
            var removeGroupArray = m_RemoveGroup.ToEntityArray(Allocator.TempJob);
            var removeArray = new NativeArray<Entity>(removeCount + m_ProcessedQueue.Count, Allocator.TempJob);

            NativeArray<Entity>.Copy(removeGroupArray, removeArray, removeCount);

            while (m_ProcessedQueue.TryDequeue(out var entity))
            {
                removeArray[removeCount++] = entity;
            }

            EntityManager.RemoveComponent(removeArray, ComponentType.ReadWrite<Idle>());
            EntityManager.RemoveComponent(removeArray, ComponentType.ReadWrite<IdleDuration>());

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