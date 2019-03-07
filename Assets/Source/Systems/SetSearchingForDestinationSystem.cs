using Game.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class SetSearchingForDestinationSystem : JobComponentSystem
    {
        private struct Job : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkComponentType<IdleTimeExpired> IdleTimeExpiredType;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var idleTimeExpiredArray = chunk.GetNativeArray(IdleTimeExpiredType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    UnityEngine.Debug.Assert(idleTimeExpiredArray[entityIndex].This != default);
                    CommandBuffer.AddComponent(m_ThreadIndex, idleTimeExpiredArray[entityIndex].This, new SearchingForDestination());
                }
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<IdleTimeExpired>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new Job
            {
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                IdleTimeExpiredType = GetArchetypeChunkComponentType<IdleTimeExpired>(true),
            }.Schedule(m_Group, inputDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}