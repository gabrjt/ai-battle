using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(DisengageSystem))]
    public class TargetDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Destination), typeof(Dying), typeof(Destroy))]
        private struct ConsolidateTargetDestinationJob : IJobProcessComponentDataWithEntity<Target, Translation, AttackDistance>
        {
            [NativeDisableParallelForRestriction] public NativeArray<Entity> EntityArray;
            [NativeDisableParallelForRestriction] public NativeArray<Destination> DestinationArray;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public void Execute(Entity entity, int index,
               [ReadOnly] ref Target target,
               [ReadOnly] ref Translation translation,
               [ReadOnly] ref AttackDistance attackDistance)
            {
                if (!TranslationFromEntity.Exists(target.Value)) return;

                var targetTranslation = TranslationFromEntity[target.Value].Value;
                var distance = math.distance(translation.Value, targetTranslation);
                var direction = math.normalizesafe(targetTranslation - translation.Value);

                EntityArray[index] = entity;
                DestinationArray[index] = new Destination { Value = targetTranslation - direction * attackDistance.Min };
            }
        }

        private struct AddTargetDestinationJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Destination> DestinationArray;
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                CommandBuffer.AddComponent(m_ThreadIndex, EntityArray[index], DestinationArray[index]);
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>() },
                None = new[] { ComponentType.ReadWrite<Destination>(), ComponentType.ReadOnly<Dying>(), ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var groupLength = m_Group.CalculateLength();
            var entityArray = new NativeArray<Entity>(groupLength, Allocator.TempJob);
            var destinationArray = new NativeArray<Destination>(groupLength, Allocator.TempJob);
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ConsolidateTargetDestinationJob
            {
                EntityArray = entityArray,
                DestinationArray = destinationArray,
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this, inputDeps);

            inputDeps = new AddTargetDestinationJob
            {
                EntityArray = entityArray,
                DestinationArray = destinationArray,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            }.Schedule(groupLength, 64, inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}