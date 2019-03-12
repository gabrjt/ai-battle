using Game.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(DisengageSystem))]
    public class TargetDestinationSystem : JobComponentSystem
    {
        [ExcludeComponent(typeof(Destination), typeof(Dead), typeof(Destroy))]
        private struct AddDestinationJob : IJobProcessComponentDataWithEntity<Target, Translation>
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref Translation translation)
            {
                CommandBuffer.AddComponent(m_ThreadIndex, entity, new Destination { Value = translation.Value });
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>() },
                None = new[] { ComponentType.ReadWrite<Destination>(), ComponentType.ReadOnly<Dead>(), ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var groupLength = m_Group.CalculateLength();
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new AddDestinationJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(this, inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}