using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class ReachedDestinationDebugSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Position position, ref Destination destination) =>
            {
                Debug.DrawLine(position.Value, destination.Value);
            });
        }
    }

    public class ReachedDestinationSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(Target))]
        private struct Job : IJobProcessComponentDataWithEntity<Destination, Position>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public MRandom Random;

            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Destination destination, [ReadOnly] ref Position position)
            {
                if (math.distance(new float3(destination.Value.x, 0, destination.Value.z), new float3(position.Value.x, 0, position.Value.z)) > 0.01f) return;

                EntityCommandBuffer.RemoveComponent<Destination>(index, entity);
                EntityCommandBuffer.AddComponent(index, entity, new Idle
                {
                    StartTime = Time,
                    IdleTime = Random.NextFloat(1, 10)
                });
            }
        }

        private MRandom m_Random;

        [Inject]
        private EndFrameBarrier m_EndFrameBarrier;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().ToConcurrent(),
                Random = m_Random,
                Time = Time.time
            }.Schedule(this, inputDeps);
        }
    }
}