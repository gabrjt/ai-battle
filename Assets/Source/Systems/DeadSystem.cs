using Game.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DeadSystem : ComponentSystem
    {
        private struct AddDeadDurationJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                CommandBuffer.AddComponent(m_ThreadIndex, EntityArray[index], new DeadDuration { Value = 5 });
            }
        }

        private ComponentGroup m_AddDeadDurationGroup;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddDeadDurationGroup = Entities.WithAll<Dead>().WithNone<DeadDuration, Destroy, Disabled>().ToComponentGroup();

            m_Random = new Random((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            var addDeadDurationGroupLength = m_AddDeadDurationGroup.CalculateLength();
            if (addDeadDurationGroupLength > 0)
            {
                var entityArray = m_AddDeadDurationGroup.ToEntityArray(Allocator.TempJob);
                var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

                var inputDeps = new AddDeadDurationJob
                {
                    EntityArray = entityArray,
                    CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent()
                }.Schedule(addDeadDurationGroupLength, 64);

                commandBufferSystem.AddJobHandleForProducer(inputDeps);

                inputDeps.Complete();
            }
        }
    }
}