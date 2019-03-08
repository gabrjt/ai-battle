﻿using Game.MonoBehaviours;
using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class PlayAttackSoundSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<PlayAttackSoundData>.Concurrent PlayAttackSoundQueue;

            [ReadOnly]
            public ArchetypeChunkComponentType<Attacked> AttackedType;

            [ReadOnly]
            public ComponentDataFromEntity<ViewReference> ViewReferenceFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Visible> VisibleFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var attackedArray = chunk.GetNativeArray(AttackedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = attackedArray[entityIndex].This;

                    if (!ViewReferenceFromEntity.Exists(entity)) continue;

                    var view = ViewReferenceFromEntity[entity].Value;

                    if (!VisibleFromEntity.Exists(view)) continue;

                    PlayAttackSoundQueue.Enqueue(new PlayAttackSoundData
                    {
                        Entity = view,
                        Translation = PositionFromEntity[view].Value
                    });
                }
            }
        }

        private struct PlayAttackSoundData
        {
            public Entity Entity;

            public float3 Translation;
        }

        private ComponentGroup m_Group;

        private NativeQueue<PlayAttackSoundData> m_PlayAttackSoundQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Attacked>() }
            });

            m_PlayAttackSoundQueue = new NativeQueue<PlayAttackSoundData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayAttackSoundQueue = m_PlayAttackSoundQueue.ToConcurrent(),
                AttackedType = GetArchetypeChunkComponentType<Attacked>(true),
                ViewReferenceFromEntity = GetComponentDataFromEntity<ViewReference>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true),
                PositionFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayAttackSoundQueue.TryDequeue(out var playAttackSoundData))
            {
                EntityManager.GetComponentObject<Transform>(playAttackSoundData.Entity).GetComponentInChildren<PlayAttackSound>().PlayAtPoint(playAttackSoundData.Translation);
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
            if (m_PlayAttackSoundQueue.IsCreated)
            {
                m_PlayAttackSoundQueue.Dispose();
            }
        }
    }
}