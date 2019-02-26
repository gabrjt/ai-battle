using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    public class MaxDistanceReachedSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(Destroy))]
        private struct Job : IJobProcessComponentDataWithEntity<Position, MaxDistance>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public EntityArchetype Archetype;

            public void Execute(Entity entity, int index, [ReadOnly] ref Position position, [ReadOnly] ref MaxDistance MaxDistance)
            {
                if (math.distance(position.Value, MaxDistance.Origin) < MaxDistance.Value) return;

                var MaxDistanceReached = EntityCommandBuffer.CreateEntity(index, Archetype);
                EntityCommandBuffer.SetComponent(index, MaxDistanceReached, new MaxDistanceReached
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