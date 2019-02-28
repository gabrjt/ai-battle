using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    // TODO: use Burst.
    public class MaxDistanceReachedSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(Destroy))]
        private struct Job : IJobProcessComponentDataWithEntity<Position, MaxSqrDistance>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public EntityArchetype Archetype;

            public void Execute(Entity entity, int index, [ReadOnly] ref Position position, [ReadOnly] ref MaxSqrDistance maxSqrDistance)
            {
                if (math.distancesq(position.Value, maxSqrDistance.Origin) < maxSqrDistance.Value) return;

                var maxDistanceReached = EntityCommandBuffer.CreateEntity(index, Archetype);
                EntityCommandBuffer.SetComponent(index, maxDistanceReached, new MaxDistanceReached
                {
                    This = entity
                });
            }
        }

        private EntityArchetype m_Archetype;

        [Inject]
        private EndFrameBarrier m_EndFrameBarrier;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<MaxDistanceReached>());
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