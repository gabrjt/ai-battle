using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ViewVisibleSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessAddVisibleJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> NotVisibleArray;
            public NativeArray<@bool> AddVisibleArray;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [ReadOnly] public float3 CameraTranslation;
            [ReadOnly] public float MaxSqrViewDistanceFromCamera;

            public void Execute(int index)
            {
                AddVisibleArray[index] = math.distancesq(TranslationFromEntity[NotVisibleArray[index]].Value, CameraTranslation) <= MaxSqrViewDistanceFromCamera;
            }
        }

        [BurstCompile]
        private struct ProcessRemoveVisibleJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> VisibleArray;
            public NativeArray<@bool> RemoveVisibleArray;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [ReadOnly] public float3 CameraTranslation;
            [ReadOnly] public float MaxSqrViewDistanceFromCamera;

            public void Execute(int index)
            {
                RemoveVisibleArray[index] = math.distancesq(TranslationFromEntity[VisibleArray[index]].Value, CameraTranslation) > MaxSqrViewDistanceFromCamera;
            }
        }

        private struct AddVisibleJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> NotVisibleArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<@bool> AddVisibleArray;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (AddVisibleArray[index])
                {
                    CommandBuffer.AddComponent(m_ThreadIndex, NotVisibleArray[index], new ViewVisible());
                }
            }
        }

        private struct RemoveVisibleJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> VisibleArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<@bool> RemoveVisibleArray;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (RemoveVisibleArray[index])
                {
                    CommandBuffer.RemoveComponent<ViewVisible>(m_ThreadIndex, VisibleArray[index]);
                }
            }
        }

        private ComponentGroup m_CameraGroup;
        private ComponentGroup m_VisibleGroup;
        private ComponentGroup m_NotVisbleGroup;
        internal float m_MaxViewLODSqrDistance;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_CameraGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<CameraArm>(), ComponentType.ReadOnly<Translation>() }
            });

            m_VisibleGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<ViewInfo>(), ComponentType.ReadWrite<ViewVisible>() }
            });

            m_NotVisbleGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<ViewInfo>() },
                None = new[] { ComponentType.ReadWrite<ViewVisible>() }
            });

            RequireSingletonForUpdate<CameraArm>();
            RequireSingletonForUpdate<MaxViewLODSqrDistance>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var maxViewLODSqrDistance = GetSingleton<MaxViewLODSqrDistance>();
            if (m_MaxViewLODSqrDistance != maxViewLODSqrDistance.Value)
            {
                maxViewLODSqrDistance.Value = m_MaxViewLODSqrDistance;
                SetSingleton(maxViewLODSqrDistance);
            }

            if (m_MaxViewLODSqrDistance == 0) return inputDeps;

            var cameraTranslationArray = m_CameraGroup.ToComponentDataArray<Translation>(Allocator.TempJob);
            var cameraTranslation = cameraTranslationArray[0].Value;
            cameraTranslationArray.Dispose();

            var hasVisible = m_VisibleGroup.CalculateLength() > 0;
            var removeVisibleDeps = default(JobHandle);
            var hasNotVisible = m_NotVisbleGroup.CalculateLength() > 0;
            var addVisibleDeps = default(JobHandle);
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            if (hasVisible)
            {
                var visibleArray = m_VisibleGroup.ToEntityArray(Allocator.TempJob);
                var removeVisibleArray = new NativeArray<@bool>(visibleArray.Length, Allocator.TempJob);

                var processRemoveVisibleDeps = new ProcessRemoveVisibleJob
                {
                    VisibleArray = visibleArray,
                    RemoveVisibleArray = removeVisibleArray,
                    TranslationFromEntity = GetComponentDataFromEntity<Translation>(true),
                    CameraTranslation = cameraTranslation,
                    MaxSqrViewDistanceFromCamera = m_MaxViewLODSqrDistance
                }.Schedule(visibleArray.Length, 64, inputDeps);

                removeVisibleDeps = new RemoveVisibleJob
                {
                    CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                    VisibleArray = visibleArray,
                    RemoveVisibleArray = removeVisibleArray,
                }.Schedule(visibleArray.Length, 64, processRemoveVisibleDeps);
            }

            if (hasNotVisible)
            {
                var notVisibleArray = m_NotVisbleGroup.ToEntityArray(Allocator.TempJob);
                var addVisibleArray = new NativeArray<@bool>(notVisibleArray.Length, Allocator.TempJob);

                var processAddVisibleDeps = new ProcessAddVisibleJob
                {
                    NotVisibleArray = notVisibleArray,
                    AddVisibleArray = addVisibleArray,
                    TranslationFromEntity = GetComponentDataFromEntity<Translation>(true),
                    CameraTranslation = cameraTranslation,
                    MaxSqrViewDistanceFromCamera = m_MaxViewLODSqrDistance
                }.Schedule(notVisibleArray.Length, 64, inputDeps);

                addVisibleDeps = new AddVisibleJob
                {
                    CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                    NotVisibleArray = notVisibleArray,
                    AddVisibleArray = addVisibleArray,
                }.Schedule(notVisibleArray.Length, 64, processAddVisibleDeps);
            }

            if (hasVisible && hasNotVisible)
            {
                inputDeps = JobHandle.CombineDependencies(addVisibleDeps, removeVisibleDeps);
            }
            else if (hasVisible)
            {
                inputDeps = JobHandle.CombineDependencies(removeVisibleDeps, inputDeps);
            }
            else if (hasNotVisible)
            {
                inputDeps = JobHandle.CombineDependencies(addVisibleDeps, inputDeps);
            }

            if (hasVisible || hasNotVisible)
            {
                commandBufferSystem.AddJobHandleForProducer(inputDeps);
            }

            return inputDeps;
        }
    }
}