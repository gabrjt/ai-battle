using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(DestroyEntityGroup))]
    public class DestroyEntitySystem : JobComponentSystem
    {
        private struct Job : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;
            public EntityCommandBuffer CommandBuffer;

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
            var destroyCommandBufferSystem = World.Active.GetExistingManager<DestroyCommandBufferSystem>();

            inputDeps = new Job
            {
                EntityArray = m_Group.ToEntityArray(Allocator.TempJob),
                CommandBuffer = destroyCommandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps.Complete();

            destroyCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}