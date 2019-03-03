using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class PlayAttackingAnimationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<PlayAttackAnimationData>.Concurrent PlayAttackAnimationDataQueue;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackSpeed> AttackSpeedType;

            [ReadOnly]
            public ArchetypeChunkComponentType<ViewReference> ViewReferenceType;

            [ReadOnly]
            public ComponentDataFromEntity<Visible> VisibleFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var attackSpeedArray = chunk.GetNativeArray(AttackSpeedType);
                var viewReferenceArray = chunk.GetNativeArray(ViewReferenceType);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = viewReferenceArray[entityIndex].Value;

                    if (!VisibleFromEntity.Exists(entity)) continue;

                    PlayAttackAnimationDataQueue.Enqueue(new PlayAttackAnimationData
                    {
                        Entity = entity,
                        AnimatorSpeed = attackSpeedArray[entityIndex].Value
                    });
                }
            }
        }

        private struct PlayAttackAnimationData
        {
            public Entity Entity;

            public float AnimatorSpeed;
        }

        private ComponentGroup m_Group;

        private NativeQueue<PlayAttackAnimationData> m_PlayAttackAnimationDataQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<ViewReference>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Attacking>(), ComponentType.ReadOnly<AttackSpeed>() }
            });

            m_PlayAttackAnimationDataQueue = new NativeQueue<PlayAttackAnimationData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayAttackAnimationDataQueue = m_PlayAttackAnimationDataQueue.ToConcurrent(),
                AttackSpeedType = GetArchetypeChunkComponentType<AttackSpeed>(true),
                ViewReferenceType = GetArchetypeChunkComponentType<ViewReference>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayAttackAnimationDataQueue.TryDequeue(out var playAttackAnimationData))
            {
                var animator = EntityManager.GetComponentObject<Animator>(playAttackAnimationData.Entity);
                animator.speed = playAttackAnimationData.AnimatorSpeed;
                animator.Play("Attacking");
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
            if (m_PlayAttackAnimationDataQueue.IsCreated)
            {
                m_PlayAttackAnimationDataQueue.Dispose();
            }
        }
    }
}