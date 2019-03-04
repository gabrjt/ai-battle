using Game.Comparers;
using Game.Components;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SetBarrier))]
    public class TargetBufferSystem : ComponentSystem
    {
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
        }

        protected override void OnUpdate()
        {
            var entityArray = m_Group.ToEntityArray(Allocator.TempJob);
            var positionFromEntity = GetComponentDataFromEntity<Position>(true);
            var deadFromEntity = GetComponentDataFromEntity<Dead>(true);

            for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var entity = entityArray[entityIndex];

                var targetBuffer = EntityManager.GetBuffer<TargetBufferElement>(entity);

                if (targetBuffer.Length == 0) continue;

                var targetBufferArray = targetBuffer.AsNativeArray().ToArray();

                m_TargetBufferComparer.Position = positionFromEntity[entity].Value;
                m_TargetBufferComparer.PositionFromEntity = positionFromEntity;

                Array.Sort(targetBufferArray, 0, targetBufferArray.Length, m_TargetBufferComparer);

                targetBuffer.Clear();

                var count = 0;
                do
                {
                    var target = targetBufferArray[count++].Value;
                    if (deadFromEntity.Exists(target)) continue; // TODO: max distance from target.
                    targetBuffer.Add(target);
                } while (count < targetBufferArray.Length);
            }

            entityArray.Dispose();
        }
    }
}