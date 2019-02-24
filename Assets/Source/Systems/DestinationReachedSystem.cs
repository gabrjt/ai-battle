using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class DestinationReachedDebugSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Position position, ref Destination destination) =>
            {
                Debug.DrawLine(position.Value, destination.Value);
            });
        }
    }

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
                /*
                EntityCommandBuffer.RemoveComponent<Destination>(index, entity);
                EntityCommandBuffer.AddComponent(index, entity, new Idle
                {
                    StartTime = Time,
                    IdleTime = Random.NextFloat(1, 10)
                });
                */
            }
        }

        private EntityArchetype m_Archetype;

        [Inject]
        private EndFrameBarrier m_EndFrameBarrier;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<DestinationReached>());
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