using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class DebugDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Destination>() },
                None = new[] { ComponentType.ReadOnly<Target>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>() },
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var targetType = GetArchetypeChunkComponentType<Target>();
            var positionType = GetArchetypeChunkComponentType<Position>();
            var destinationType = GetArchetypeChunkComponentType<Destination>();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var positionArray = chunk.GetNativeArray(positionType);
                var destinationArray = chunk.GetNativeArray(destinationType);

                if (chunk.Has(targetType))
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        Debug.DrawLine(positionArray[entityIndex].Value, destinationArray[entityIndex].Value, Color.red);
                    }
                }
                else
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        Debug.DrawLine(positionArray[entityIndex].Value, destinationArray[entityIndex].Value, Color.green);
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}