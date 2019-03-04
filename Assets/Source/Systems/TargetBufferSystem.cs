using Game.Comparers;
using Game.Components;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SetBarrier))]
    public class TargetBufferSystem : ComponentSystem
    {
        private struct ConsolidateJob : IJobChunk
        {
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                throw new NotImplementedException();
            }
        }

        private ComponentGroup m_Group;

        private TargetBufferComparer m_TargetBufferComparer; // TODO: make it blitable and thread safe

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<TargetBufferElement>() },
                None = new[] { ComponentType.ReadOnly<Dead>() },
            });

            m_TargetBufferComparer = new TargetBufferComparer();

            RequireForUpdate(m_Group);
        }

        protected override void OnUpdate()
        {
            var entityArray = m_Group.ToEntityArray(Allocator.TempJob);
            var positionFromEntity = GetComponentDataFromEntity<Position>(true);
            var deadFromEntity = GetComponentDataFromEntity<Dead>(true);
            var attackDistanceFromEntity = GetComponentDataFromEntity<AttackDistance>(true);

            for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var entity = entityArray[entityIndex];

                var targetBuffer = EntityManager.GetBuffer<TargetBufferElement>(entity);

                if (targetBuffer.Length == 0) continue;

                var targetBufferArray = targetBuffer.AsNativeArray().ToArray();

                var position = positionFromEntity[entity].Value;

                m_TargetBufferComparer.Position = position;
                m_TargetBufferComparer.PositionFromEntity = positionFromEntity;

                Array.Sort(targetBufferArray, 0, targetBufferArray.Length, m_TargetBufferComparer);

                targetBuffer.Clear();

                var count = 0;
                do
                {
                    var target = targetBufferArray[count++].Value;
                    var targetPostion = positionFromEntity[target].Value;

                    if (deadFromEntity.Exists(target) || math.distance(position, targetPostion) > attackDistanceFromEntity[entity].Max) continue;

                    targetBuffer.Add(target);
                } while (count < targetBufferArray.Length);
            }

            entityArray.Dispose();
        }
    }
}