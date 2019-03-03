using Game.Behaviours;
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
            public ArchetypeChunkComponentType<Damaged> DamagedType;

            [ReadOnly]
            public ComponentDataFromEntity<ViewReference> ViewReferenceFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Visible> VisibleFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var damagedArray = chunk.GetNativeArray(DamagedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = damagedArray[entityIndex].This;

                    if (!ViewReferenceFromEntity.Exists(entity)) continue;

                    var view = ViewReferenceFromEntity[entity].Value;

                    if (!VisibleFromEntity.Exists(view)) continue;

                    PlayAttackSoundQueue.Enqueue(new PlayAttackSoundData
                    {
                        Entity = view,
                        Position = PositionFromEntity[view].Value
                    });
                }
            }
        }

        private struct PlayAttackSoundData
        {
            public Entity Entity;

            public float3 Position;
        }

        private ComponentGroup m_Group;

        private NativeQueue<PlayAttackSoundData> m_PlayAttackSoundQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Damaged>() }
            });

            m_PlayAttackSoundQueue = new NativeQueue<PlayAttackSoundData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                PlayAttackSoundQueue = m_PlayAttackSoundQueue.ToConcurrent(),
                DamagedType = GetArchetypeChunkComponentType<Damaged>(true),
                ViewReferenceFromEntity = GetComponentDataFromEntity<ViewReference>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_PlayAttackSoundQueue.TryDequeue(out var playAttackSoundData))
            {
                EntityManager.GetComponentObject<Transform>(playAttackSoundData.Entity).GetComponentInChildren<PlayAttackSoundBehaviour>().PlayAtPoint(playAttackSoundData.Position);
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