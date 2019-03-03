using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    public class PlayChargingAnimationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent PlayIdleAnimationQueue;

            public NativeQueue<PlayChargingAnimationData>.Concurrent PlayChargingAnimationDataQueue;

            [ReadOnly]
            public ArchetypeChunkComponentType<ViewReference> ViewReferenceType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Position> PositionType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackDistance> AttackDistanceType;

            [ReadOnly]
            public ComponentDataFromEntity<Visible> VisibleFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Owner> OwnerFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var viewReferenceArray = chunk.GetNativeArray(ViewReferenceType);
                var positionArray = chunk.GetNativeArray(PositionType);
                var targetArray = chunk.GetNativeArray(TargetType);
                var AttackDistanceArray = chunk.GetNativeArray(AttackDistanceType);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var view = viewReferenceArray[entityIndex].Value;
                    var position = positionArray[entityIndex].Value;
                    var target = targetArray[entityIndex].Value;
                    var maxAttackDistance = AttackDistanceArray[entityIndex].Max;

                    if (!VisibleFromEntity.Exists(view)) continue;

                    if (math.distance(position, PositionFromEntity[target].Value) <= maxAttackDistance)
                    {
                        PlayIdleAnimationQueue.Enqueue(view);
                    }
                    else
                    {
                        PlayChargingAnimationDataQueue.Enqueue(new PlayChargingAnimationData
                        {
                            Owner = OwnerFromEntity[view].Value,
                            View = view
                        });
                    }
                }
            }
        }

        private struct PlayChargingAnimationData
        {
            public Entity Owner;

            public Entity View;
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_PlayIdleAnimationQueue;

        private NativeQueue<PlayChargingAnimationData> m_PlayChargingAnimationDataQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<ViewReference>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Attacking>() }
            });

            m_PlayIdleAnimationQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_PlayChargingAnimationDataQueue = new NativeQueue<PlayChargingAnimationData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayIdleAnimationQueue = m_PlayIdleAnimationQueue.ToConcurrent(),
                PlayChargingAnimationDataQueue = m_PlayChargingAnimationDataQueue.ToConcurrent(),
                ViewReferenceType = GetArchetypeChunkComponentType<ViewReference>(true),
                PositionType = GetArchetypeChunkComponentType<Position>(true),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                AttackDistanceType = GetArchetypeChunkComponentType<AttackDistance>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                OwnerFromEntity = GetComponentDataFromEntity<Owner>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayIdleAnimationQueue.TryDequeue(out var entity))
            {
                var animator = EntityManager.GetComponentObject<Animator>(entity);
                animator.speed = 1;
                animator.Play("Idle");
            }

            while (m_PlayChargingAnimationDataQueue.TryDequeue(out var playChargingAnimationData))
            {
                var navMeshAgent = EntityManager.GetComponentObject<NavMeshAgent>(playChargingAnimationData.Owner);
                var animator = EntityManager.GetComponentObject<Animator>(playChargingAnimationData.View);
                animator.speed = 1;

                if (navMeshAgent.pathPending)
                {
                    animator.Play("Idle");
                }
                else
                {
                    animator.Play("Charging");
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
            if (m_PlayIdleAnimationQueue.IsCreated)
            {
                m_PlayIdleAnimationQueue.Dispose();
            }

            if (m_PlayChargingAnimationDataQueue.IsCreated)
            {
                m_PlayChargingAnimationDataQueue.Dispose();
            }
        }
    }
}