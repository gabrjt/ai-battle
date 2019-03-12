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
    public class FacingTargetSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Dead))]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Target, Translation, Rotation>
        {
            [NativeDisableParallelForRestriction] public NativeArray<Entity> EntityArray;
            [NativeDisableParallelForRestriction] public NativeArray<@bool> AddArray;
            [NativeDisableParallelForRestriction] public NativeArray<@bool> RemoveArray;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [ReadOnly] public ComponentDataFromEntity<FacingTarget> FacingTargetFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref Translation translation, [ReadOnly] ref Rotation rotation)
            {
                var dot = math.dot(math.forward(rotation.Value), math.normalizesafe(TranslationFromEntity[target.Value].Value - translation.Value));
                var hasFacingTarget = FacingTargetFromEntity.Exists(entity);

                EntityArray[index] = entity;
                AddArray[index] = dot >= 0.9f && !hasFacingTarget;
                RemoveArray[index] = dot < 0.9f && hasFacingTarget;
            }
        }

        private struct FacingTargetJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<@bool> AddArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<@bool> RemoveArray;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (AddArray[index])
                {
                    CommandBuffer.AddComponent(m_ThreadIndex, EntityArray[index], new FacingTarget());
                }
                else if (RemoveArray[index])
                {
                    CommandBuffer.RemoveComponent<FacingTarget>(m_ThreadIndex, EntityArray[index]);
                }
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Rotation>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var length = m_Group.CalculateLength();
            var entityArray = new NativeArray<Entity>(length, Allocator.TempJob);
            var addArray = new NativeArray<@bool>(length, Allocator.TempJob);
            var removeArray = new NativeArray<@bool>(length, Allocator.TempJob);
            var commandBufferSystem = World.Active.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                EntityArray = entityArray,
                AddArray = addArray,
                RemoveArray = removeArray,
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true),
                FacingTargetFromEntity = GetComponentDataFromEntity<FacingTarget>(true)
            }.Schedule(this, inputDeps);

            inputDeps = new FacingTargetJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityArray = entityArray,
                AddArray = addArray,
                RemoveArray = removeArray
            }.Schedule(length, 64, inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}