using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(DestroyEventEntityGroup))]
    public class DestroyEventSystem : JobComponentSystem
    {
        private struct Job : IJobParallelFor
        {
            [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(int index)
            {
                for (int entityIndex = 0; entityIndex < EntityArray.Length; entityIndex++)
                {
                    CommandBuffer.DestroyEntity(index, EntityArray[entityIndex]);
                }
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var destroyEventCommandBufferSystem = World.Active.GetExistingManager<DestroyEventCommandBufferSystem>();

            inputDeps = new Job
            {
                EntityArray = m_Group.ToEntityArray(Allocator.TempJob),
                CommandBuffer = destroyEventCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(m_Group.CalculateLength(), 64, inputDeps);

            inputDeps.Complete();

            destroyEventCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }

    [UpdateInGroup(typeof(DestroyEntityGroup))]
    public class DestroyEntitySystem : JobComponentSystem
    {
        private struct Job : IJobParallelFor
        {
            [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(int index)
            {
                for (int entityIndex = 0; entityIndex < EntityArray.Length; entityIndex++)
                {
                    CommandBuffer.DestroyEntity(index, EntityArray[entityIndex]);
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
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var destroyCommandBufferSystem = World.Active.GetExistingManager<DestroyCommandBufferSystem>();

            inputDeps = new Job
            {
                EntityArray = m_Group.ToEntityArray(Allocator.TempJob),
                CommandBuffer = destroyCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(m_Group.CalculateLength(), 64, inputDeps);

            inputDeps.Complete();

            destroyCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}