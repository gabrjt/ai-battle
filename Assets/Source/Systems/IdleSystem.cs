using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class IdleSystem : JobComponentSystem
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

        private struct AddJob : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new Idle
                    {
                        Duration = 1
                    });
                }
            }
        }

        private ComponentGroup m_AddGroup;
        private ComponentGroup m_ProcessGroup;
        private ComponentGroup m_RemoveGroup;
        private NativeQueue<Entity> m_ProcessedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Idle>(), ComponentType.ReadOnly<Dying>() }
            });

            m_RemoveGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Idle>() },
                Any = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>() }
            });

            m_ProcessedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = Process(inputDeps);

            Remove();

            inputDeps = Add(inputDeps, commandBufferSystem);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        private JobHandle Process(JobHandle inputDeps)
        {
            inputDeps = new ProcessJob
            {
                ProcessedQueue = m_ProcessedQueue.ToConcurrent(),
                DeltaTime = Time.deltaTime
            }.Schedule(this, inputDeps);

            inputDeps.Complete();
            return inputDeps;
        }

        private void Remove()
        {
            var removeGroupCount = m_RemoveGroup.CalculateLength();
            var removeList = new NativeList<Entity>(removeGroupCount, Allocator.Temp);

            if (removeGroupCount > 0)
            {
                var removeGroupArray = m_RemoveGroup.ToEntityArray(Allocator.TempJob);
                NativeArray<Entity>.Copy(removeGroupArray, removeList, removeGroupCount);
                removeGroupArray.Dispose();
            }

            if (m_ProcessedQueue.Count > 0)
            {
                removeList.Capacity += m_ProcessedQueue.Count - 1;
                while (m_ProcessedQueue.TryDequeue(out var entity))
                {
                    removeList.Add(entity);
                }
            }

            if (removeList.Length > 0)
            {
                EntityManager.RemoveComponent(removeList, ComponentType.ReadWrite<Idle>());
            }

            removeList.Dispose();
        }

        private JobHandle Add(JobHandle inputDeps, BeginSimulationEntityCommandBufferSystem commandBufferSystem)
        {
            inputDeps = new AddJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType()
            }.Schedule(m_AddGroup, inputDeps);
            return inputDeps;
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