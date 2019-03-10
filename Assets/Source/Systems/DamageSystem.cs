using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(ClampHealthSystem))]
    public class DamageSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> DamagedEntityArray;
            [ReadOnly] public ComponentDataFromEntity<Damaged> DamagedFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Health> HealthFromEntity;

            public void Execute(int index)
            {
                var damaged = DamagedFromEntity[DamagedEntityArray[index]];
                var damagedEntity = damaged.Other;

                if (!HealthFromEntity.Exists(damagedEntity)) return;

                var health = HealthFromEntity[damagedEntity];
                health.Value -= damaged.Value;
                HealthFromEntity[damagedEntity] = health;
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Damaged>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ProcessJob
            {
                DamagedEntityArray = m_Group.ToEntityArray(Allocator.TempJob),
                DamagedFromEntity = GetComponentDataFromEntity<Damaged>(true),
                HealthFromEntity = GetComponentDataFromEntity<Health>()
            }.Schedule(m_Group.CalculateLength(), 64, inputDeps);

            return inputDeps;
        }
    }
}