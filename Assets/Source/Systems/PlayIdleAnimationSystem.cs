using Game.Components;

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class PlayIdleAnimationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent PlayIdleAnimationQueue;

            [ReadOnly]
            public ArchetypeChunkComponentType<ViewReference> ViewReferenceType;

            [ReadOnly]
            public ComponentDataFromEntity<Visible> VisibleFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var viewReferenceArray = chunk.GetNativeArray(ViewReferenceType);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = viewReferenceArray[entityIndex].Value;

                    if (!VisibleFromEntity.Exists(entity)) continue;

                    PlayIdleAnimationQueue.Enqueue(entity);
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_PlayIdleAnimationQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<ViewReference>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            });

            m_PlayIdleAnimationQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayIdleAnimationQueue = m_PlayIdleAnimationQueue.ToConcurrent(),
                ViewReferenceType = GetArchetypeChunkComponentType<ViewReference>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayIdleAnimationQueue.TryDequeue(out var entity))
            {
                var animator = EntityManager.GetComponentObject<Animator>(entity);
                animator.speed = 1;
                animator.Play("Idle");
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
            if (m_PlayIdleAnimationQueue.IsCreated)
            {
                m_PlayIdleAnimationQueue.Dispose();
            }
        }
    }
}