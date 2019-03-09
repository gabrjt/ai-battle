using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class IdleSystem : ComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(IdleDuration))]
        private struct ConsolidateIdleDurationJob : IJobProcessComponentDataWithEntity<Idle>
        {
            [NativeDisableParallelForRestriction] public NativeArray<Entity> EntityArray;
            [NativeDisableParallelForRestriction] public NativeArray<IdleDuration> IdleDurationArray;
            [ReadOnly] public Random Random;

            public void Execute(Entity entity, int index, [ReadOnly] ref Idle idle)
            {
                EntityArray[index] = entity;
                IdleDurationArray[index] = new IdleDuration { Value = Random.NextFloat(1, 10) };
            }
        }

        private struct AddIdleDurationJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<IdleDuration> IdleDurationArray;
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                CommandBuffer.AddComponent(m_ThreadIndex, EntityArray[index], IdleDurationArray[index]);
            }
        }

        private ComponentGroup m_RemoveIdleGroup;
        private ComponentGroup m_AddIdleGroup;
        private ComponentGroup m_AddIdleDurationGroup;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_RemoveIdleGroup = Entities.WithAll<Idle>().WithAny<Destination, Target, Dying>().ToComponentGroup();
            m_AddIdleGroup = Entities.WithAll<Character>().WithNone<Idle, Destination, Target, Dying>().ToComponentGroup();
            m_AddIdleDurationGroup = Entities.WithAll<Idle>().WithNone<IdleDuration>().ToComponentGroup();
            m_Random = new Random((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            if (m_RemoveIdleGroup.CalculateLength() > 0)
            {
                EntityManager.RemoveComponent(m_RemoveIdleGroup, ComponentType.ReadWrite<IdleDuration>());
                EntityManager.RemoveComponent(m_RemoveIdleGroup, ComponentType.ReadWrite<Idle>());
            }

            if (m_AddIdleGroup.CalculateLength() > 0)
            {
                EntityManager.AddComponent(m_AddIdleGroup, ComponentType.ReadWrite<Idle>());
            }

            var addIdleDurationGroupLength = m_AddIdleDurationGroup.CalculateLength();
            if (addIdleDurationGroupLength > 0)
            {
                var entityArray = new NativeArray<Entity>(addIdleDurationGroupLength, Allocator.TempJob);
                var idleDurationArray = new NativeArray<IdleDuration>(addIdleDurationGroupLength, Allocator.TempJob);
                var commandBuffer = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

                var addIdleDurationDeps = new ConsolidateIdleDurationJob
                {
                    EntityArray = entityArray,
                    IdleDurationArray = idleDurationArray,
                    Random = m_Random
                }.Schedule(this);

                addIdleDurationDeps = new AddIdleDurationJob
                {
                    EntityArray = entityArray,
                    IdleDurationArray = idleDurationArray,
                    CommandBuffer = commandBuffer.ToConcurrent()
                }.Schedule(addIdleDurationGroupLength, 64, addIdleDurationDeps);

                addIdleDurationDeps.Complete();
            }
        }
    }
}