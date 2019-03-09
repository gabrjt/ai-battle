using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DisengageSystem : ComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Target, Translation, EngageSqrRadius>
        {
            public NativeQueue<Entity>.Concurrent RemoveQueue;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref Translation translation, [ReadOnly] ref EngageSqrRadius engageSqrRadius)
            {
                if (math.distancesq(translation.Value, TranslationFromEntity[target.Value].Value) <= engageSqrRadius.Value) return;

                RemoveQueue.Enqueue(entity);
            }
        }

        private struct RemoveTargetJob : IJob
        {
            public NativeQueue<Entity> RemoveQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Target>(entity);
                    CommandBuffer.RemoveComponent<Destination>(entity);
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<Entity> m_RemoveQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<Target>().ToComponentGroup();

            m_RemoveQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((Entity entity, ref Target target) =>
            {
                if (EntityManager.Exists(target.Value)) return;

                PostUpdateCommands.RemoveComponent<Target>(entity);
            });

            PostUpdateCommands.Playback(EntityManager);

            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            var inputDeps = new ProcessJob
            {
                RemoveQueue = m_RemoveQueue.ToConcurrent(),
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this);

            inputDeps = new RemoveTargetJob
            {
                RemoveQueue = m_RemoveQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps.Complete();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveQueue.IsCreated)
            {
                m_RemoveQueue.Dispose();
            }
        }
    }
}