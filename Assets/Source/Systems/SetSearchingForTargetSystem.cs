using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SetSearchingForTargetSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                Any = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() },
                None = new[] { ComponentType.ReadOnly<SearchingForTarget>(), ComponentType.ReadOnly<Target>() }
            });

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    PostUpdateCommands.AddComponent(entityArray[entityIndex], new SearchingForTarget
                    {
                        Radius = 10,
                        SearchForTargetTime = 1,
                        StartTime = Time.time
                    });
                }
            }

            chunkArray.Dispose();
        }
    }
}