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
    //[DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class DebugCharacterTranslationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Translation>.Concurrent KnightQueue;
            public NativeQueue<Translation>.Concurrent OrcWolfRiderQueue;
            public NativeQueue<Translation>.Concurrent SkeletonQueue;
            [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationType;
            [ReadOnly] public ArchetypeChunkComponentType<Knight> KnightType;
            [ReadOnly] public ArchetypeChunkComponentType<OrcWolfRider> OrcWolfRiderType;
            [ReadOnly] public ArchetypeChunkComponentType<Skeleton> SkeletonType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var translationArray = chunk.GetNativeArray(TranslationType);

                if (chunk.Has(KnightType))
                {
                    for (var entityIndex = 0; entityIndex < translationArray.Length; entityIndex++)
                    {
                        KnightQueue.Enqueue(translationArray[entityIndex]);
                    }
                }
                else if (chunk.Has(OrcWolfRiderType))
                {
                    for (var entityIndex = 0; entityIndex < translationArray.Length; entityIndex++)
                    {
                        OrcWolfRiderQueue.Enqueue(translationArray[entityIndex]);
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < translationArray.Length; entityIndex++)
                    {
                        SkeletonQueue.Enqueue(translationArray[entityIndex]);
                    }
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<Translation> m_KnightQueue;
        private NativeQueue<Translation> m_OrcWolfRiderQueue;
        private NativeQueue<Translation> m_SkeletonQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                Any = new[] { ComponentType.ReadOnly<Knight>(), ComponentType.ReadOnly<OrcWolfRider>(), ComponentType.ReadOnly<Skeleton>() }
            });

            m_KnightQueue = new NativeQueue<Translation>(Allocator.Persistent);
            m_OrcWolfRiderQueue = new NativeQueue<Translation>(Allocator.Persistent);
            m_SkeletonQueue = new NativeQueue<Translation>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ConsolidateJob
            {
                KnightQueue = m_KnightQueue.ToConcurrent(),
                OrcWolfRiderQueue = m_OrcWolfRiderQueue.ToConcurrent(),
                SkeletonQueue = m_SkeletonQueue.ToConcurrent(),
                TranslationType = GetArchetypeChunkComponentType<Translation>(true),
                KnightType = GetArchetypeChunkComponentType<Knight>(true),
                OrcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true),
                SkeletonType = GetArchetypeChunkComponentType<Skeleton>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            while (m_KnightQueue.TryDequeue(out var translation))
            {
                Debug.DrawRay(translation.Value, math.up(), Color.blue);
            }

            while (m_OrcWolfRiderQueue.TryDequeue(out var translation))
            {
                Debug.DrawRay(translation.Value, math.up(), Color.magenta);
            }

            while (m_SkeletonQueue.TryDequeue(out var translation))
            {
                Debug.DrawRay(translation.Value, math.up(), Color.black);
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
            if (m_KnightQueue.IsCreated)
            {
                m_KnightQueue.Dispose();
            }

            if (m_OrcWolfRiderQueue.IsCreated)
            {
                m_OrcWolfRiderQueue.Dispose();
            }

            if (m_SkeletonQueue.IsCreated)
            {
                m_SkeletonQueue.Dispose();
            }
        }
    }
}