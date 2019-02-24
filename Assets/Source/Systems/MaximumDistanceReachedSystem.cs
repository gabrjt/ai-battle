using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    public class MaximumDistanceReachedSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(Destroy))]
        private struct Job : IJobProcessComponentDataWithEntity<Position, MaximumDistance>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public EntityArchetype Archetype;

            public void Execute(Entity entity, int index, [ReadOnly] ref Position position, [ReadOnly] ref MaximumDistance maximumDistance)
            {
                if (math.distance(position.Value, maximumDistance.Origin) < maximumDistance.Value) return;

                var maximumDistanceReached = EntityCommandBuffer.CreateEntity(index, Archetype);
                EntityCommandBuffer.SetComponent(index, maximumDistanceReached, new MaximumDistanceReached
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

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<MaximumDistanceReached>());
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