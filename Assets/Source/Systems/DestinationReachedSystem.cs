using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    // TODO: use Burst.
    public class DestinationReachedSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(Target))]
        private struct Job : IJobProcessComponentDataWithEntity<Destination, Position>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public EntityArchetype Archetype;

            public void Execute(Entity entity, int index, [ReadOnly] ref Destination destination, [ReadOnly] ref Position position)
            {
                if (math.distance(new float3(destination.Value.x, 0, destination.Value.z), new float3(position.Value.x, 0, position.Value.z)) > 0.01f) return;

                var destinationReached = EntityCommandBuffer.CreateEntity(index, Archetype);
                EntityCommandBuffer.SetComponent(index, destinationReached, new DestinationReached { This = entity });
            }
        }

        private EntityArchetype m_Archetype;

        [Inject]
        private EndFrameBarrier m_EndFrameBarrier;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Event>(), ComponentType.Create<DestinationReached>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().ToConcurrent(),
                Archetype = m_Archetype
            }.Schedule(this, inputDeps);
        }
    }
}