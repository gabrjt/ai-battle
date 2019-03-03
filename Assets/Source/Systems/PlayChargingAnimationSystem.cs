using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class PlayChargingAnimationSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent PlayIdleAnimationQueue;

            public NativeQueue<Entity>.Concurrent PlayChargingAnimationQueue;

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
                        PlayChargingAnimationQueue.Enqueue(view);
                    }
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_PlayIdleAnimationQueue;

        private NativeQueue<Entity> m_PlayChargingAnimationQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<ViewReference>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Attacking>() }
            });

            m_PlayIdleAnimationQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_PlayChargingAnimationQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayIdleAnimationQueue = m_PlayIdleAnimationQueue.ToConcurrent(),
                PlayChargingAnimationQueue = m_PlayChargingAnimationQueue.ToConcurrent(),
                ViewReferenceType = GetArchetypeChunkComponentType<ViewReference>(true),
                PositionType = GetArchetypeChunkComponentType<Position>(true),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                AttackDistanceType = GetArchetypeChunkComponentType<AttackDistance>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayIdleAnimationQueue.TryDequeue(out var entity))
            {
                var animator = EntityManager.GetComponentObject<Animator>(entity);
                animator.speed = 1;
                animator.Play("Idle");
            }

            while (m_PlayChargingAnimationQueue.TryDequeue(out var entity))
            {
                var animator = EntityManager.GetComponentObject<Animator>(entity);
                animator.speed = 1;
                animator.Play("Charging");
            }

            return inputDeps;
        }
    }
}