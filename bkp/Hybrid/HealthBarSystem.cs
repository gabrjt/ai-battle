using Game.Components;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.UI;

namespace Game.Systems
{
    public class HealthBarSystem : JobComponentSystem, IDisposable
    {
        [RequireComponentTag(typeof(HealthBar), typeof(Visible))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Owner>
        {
            public NativeQueue<HealthBarData>.Concurrent DataQueue;

            [ReadOnly]
            public ComponentDataFromEntity<Health> HealthFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<MaxHealth> MaxHealthFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Owner owner)
            {
                var ownerEntity = owner.Value;

                DataQueue.Enqueue(new HealthBarData
                {
                    Entity = entity,
                    FillAmount = HealthFromEntity[ownerEntity].Value / MaxHealthFromEntity[ownerEntity].Value
                });
            }
        }

        private struct HealthBarData
        {
            public Entity Entity;

            public float FillAmount;
        }

        private NativeQueue<HealthBarData> m_DataQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_DataQueue = new NativeQueue<HealthBarData>(Allocator.TempJob);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                DataQueue = m_DataQueue.ToConcurrent(),
                HealthFromEntity = GetComponentDataFromEntity<Health>(true),
                MaxHealthFromEntity = GetComponentDataFromEntity<MaxHealth>(true)
            }.Schedule(this, inputDeps);

            inputDeps.Complete();

            while (m_DataQueue.TryDequeue(out var data))
            {
                EntityManager.GetComponentObject<Image>(data.Entity).fillAmount = data.FillAmount;
            }

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_DataQueue.IsCreated)
            {
                m_DataQueue.Dispose();
            }
        }
    }
}