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
    [UpdateAfter(typeof(ProcessMotionSystem))]
    [UpdateBefore(typeof(MoveSystem))]
    public class ProcessTargetDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Target, Translation, AttackDistance, Velocity, Destination>
        {
            public NativeQueue<Entity>.Concurrent AddTargetInRangeQueue;
            public NativeQueue<Entity>.Concurrent RemoveTargetInRangeQueue;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [ReadOnly] public ComponentDataFromEntity<TargetInRange> TargetInRangeFromEntity;

            public void Execute(Entity entity, int index,
                [ReadOnly] ref Target target,
                [ReadOnly] ref Translation translation,
                [ReadOnly] ref AttackDistance attackDistance,
                ref Velocity velocity,
                ref Destination destination)
            {
                var targetTranslation = TranslationFromEntity[target.Value].Value;
                var distance = math.distance(translation.Value, targetTranslation);

                if (distance < attackDistance.Min || distance > attackDistance.Max)
                {
                    var direction = math.normalizesafe(targetTranslation - translation.Value);
                    destination.Value = targetTranslation - direction * attackDistance.Min;

                    if (TargetInRangeFromEntity.Exists(entity))
                    {
                        RemoveTargetInRangeQueue.Enqueue(entity);
                    }
                }
                else
                {
                    destination.Value = translation.Value;
                    velocity.Value = float3.zero;

                    if (!TargetInRangeFromEntity.Exists(entity))
                    {
                        AddTargetInRangeQueue.Enqueue(entity);
                    }
                }
            }
        }

        private struct AddTargetInRangeJob : IJob
        {
            [ReadOnly] public EntityCommandBuffer CommandBuffer;
            public NativeQueue<Entity> AddTargetInRangeQueue;

            public void Execute()
            {
                while (AddTargetInRangeQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.AddComponent(entity, new TargetInRange());
                }
            }
        }

        private struct RemoveTargetInRangeJob : IJob
        {
            [ReadOnly] public EntityCommandBuffer CommandBuffer;
            public NativeQueue<Entity> RemoveTargetInRangeQueue;

            public void Execute()
            {
                while (RemoveTargetInRangeQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<TargetInRange>(entity);
                }
            }
        }

        private NativeQueue<Entity> m_AddTargetInRangeQueue;
        private NativeQueue<Entity> m_RemoveTargetInRangeQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddTargetInRangeQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_RemoveTargetInRangeQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                AddTargetInRangeQueue = m_AddTargetInRangeQueue.ToConcurrent(),
                RemoveTargetInRangeQueue = m_RemoveTargetInRangeQueue.ToConcurrent(),
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true),
                TargetInRangeFromEntity = GetComponentDataFromEntity<TargetInRange>(true)
            }.Schedule(this, inputDeps);

            var addTargetInRangeDeps = new AddTargetInRangeJob
            {
                AddTargetInRangeQueue = m_AddTargetInRangeQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            var removeTargetInRangeDeps = new RemoveTargetInRangeJob
            {
                RemoveTargetInRangeQueue = m_RemoveTargetInRangeQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps = JobHandle.CombineDependencies(addTargetInRangeDeps, removeTargetInRangeDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_AddTargetInRangeQueue.IsCreated)
            {
                m_AddTargetInRangeQueue.Dispose();
            }

            if (m_RemoveTargetInRangeQueue.IsCreated)
            {
                m_RemoveTargetInRangeQueue.Dispose();
            }
        }
    }
}