using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    /*
    public class CalculateIdleTimeSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Idle>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunks = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var idleType = GetArchetypeChunkComponentType<Idle>(true);

            for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);
                var idleArray = chunk.GetNativeArray(idleType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var idle = idleArray[entityIndex];

                    if (idle.StartTime + idle.IdleTime >= Time.time)
                    {
                        PostUpdateCommands.RemoveComponent<Idle>(entity);
                        PostUpdateCommands.AddComponent(entity, new SearchingForDestination());
                    }
                }
            }

            chunks.Dispose();
        }
    }
    */

    public class CalculateIdleTimeSystem : JobComponentSystem
    {
        private struct Job : IJobProcessComponentDataWithEntity<Idle>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Idle idle)
            {
                if (idle.StartTime + idle.IdleTime >= Time) return;

                EntityCommandBuffer.RemoveComponent<Idle>(index, entity);
                EntityCommandBuffer.AddComponent(index, entity, new SearchingForDestination());
            }
        }

        [Inject]
        private EndFrameBarrier m_EndFrameBarrier;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);
        }
    }
}