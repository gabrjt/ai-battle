using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class PlayDyingAnimationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent PlayDyingAnimationQueue;

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

                    PlayDyingAnimationQueue.Enqueue(entity);
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_PlayDyingAnimationQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<ViewReference>(), ComponentType.ReadOnly<Dead>() }
            });

            m_PlayDyingAnimationQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayDyingAnimationQueue = m_PlayDyingAnimationQueue.ToConcurrent(),
                ViewReferenceType = GetArchetypeChunkComponentType<ViewReference>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayDyingAnimationQueue.TryDequeue(out var entity))
            {
                var animator = EntityManager.GetComponentObject<Animator>(entity);
                animator.speed = 1;
                animator.Play("Dying");
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
            if (m_PlayDyingAnimationQueue.IsCreated)
            {
                m_PlayDyingAnimationQueue.Dispose();
            }
        }
    }
}