using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ReachedDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Target))]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Translation, Destination>
        {
            public NativeQueue<Entity>.Concurrent RemoveQueue;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Destination destination)
            {
                if (math.distancesq(translation.Value, destination.Value) > 0.01f) return;

                RemoveQueue.Enqueue(entity);
            }
        }

        private struct RemoveJob : IJob
        {
            public NativeQueue<Entity> RemoveQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Destination>(entity);
                }
            }
        }

        private NativeQueue<Entity> m_RemoveQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_RemoveQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                RemoveQueue = m_RemoveQueue.ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps = new RemoveJob
            {
                RemoveQueue = m_RemoveQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveQueue.IsCreated)
            {
                m_RemoveQueue.Dispose();
            }
        }
    }
}