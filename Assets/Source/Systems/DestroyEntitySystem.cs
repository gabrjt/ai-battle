using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(EntityLifecycleGroup))]
    public class DestroyEntitySystem : JobComponentSystem
    {
        private struct Job : IJob
        {
            [ReadOnly] public EntityCommandBuffer CommandBuffer;
            [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;

            public void Execute()
            {
                for (int entityIndex = 0; entityIndex < EntityArray.Length; entityIndex++)
                {
                    CommandBuffer.DestroyEntity(EntityArray[entityIndex]);
                }
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>() },
                None = new[] { ComponentType.ReadOnly<HealthBar>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<HealthBar>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.Active.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new Job
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer(),
                EntityArray = m_Group.ToEntityArray(Allocator.TempJob),
            }.Schedule(inputDeps);

            inputDeps.Complete();

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}