using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    public class PlayWalkingAnimationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<PlayWalkingAnimationData>.Concurrent PlayWalkingAnimationQueue;

            [ReadOnly]
            public ArchetypeChunkComponentType<ViewReference> ViewReferenceType;

            [ReadOnly]
            public ComponentDataFromEntity<Visible> VisibleFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Owner> OwnerFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var viewReferenceArray = chunk.GetNativeArray(ViewReferenceType);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var view = viewReferenceArray[entityIndex].Value;

                    if (!VisibleFromEntity.Exists(view)) continue;

                    PlayWalkingAnimationQueue.Enqueue(new PlayWalkingAnimationData
                    {
                        Owner = OwnerFromEntity[view].Value,
                        View = view
                    });
                }
            }
        }

        private struct PlayWalkingAnimationData
        {
            public Entity Owner;

            public Entity View;
        }

        private ComponentGroup m_Group;

        private NativeQueue<PlayWalkingAnimationData> m_PlayWalkingAnimationDataQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<ViewReference>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Target>() }
            });

            m_PlayWalkingAnimationDataQueue = new NativeQueue<PlayWalkingAnimationData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayWalkingAnimationQueue = m_PlayWalkingAnimationDataQueue.ToConcurrent(),
                ViewReferenceType = GetArchetypeChunkComponentType<ViewReference>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true),
                OwnerFromEntity = GetComponentDataFromEntity<Owner>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayWalkingAnimationDataQueue.TryDequeue(out var playWalkingAnimationData))
            {
                var navMeshAgent = EntityManager.GetComponentObject<NavMeshAgent>(playWalkingAnimationData.Owner);
                var animator = EntityManager.GetComponentObject<Animator>(playWalkingAnimationData.View);
                animator.speed = 1;

                if (navMeshAgent.pathPending)
                {
                    animator.Play("Idle");
                }
                else
                {
                    animator.Play("Walking");
                }
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
            if (m_PlayWalkingAnimationDataQueue.IsCreated)
            {
                m_PlayWalkingAnimationDataQueue.Dispose();
            }
        }
    }
}