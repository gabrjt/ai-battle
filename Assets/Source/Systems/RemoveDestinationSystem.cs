using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class RemoveDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_RemoveDestinationList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<DestinationReached>() }
            });

            m_RemoveDestinationList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var deadType = GetArchetypeChunkComponentType<Dead>(true);
            var destinationReachedType = GetArchetypeChunkComponentType<DestinationReached>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(deadType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (m_RemoveDestinationList.Contains(entity)) continue;

                        m_RemoveDestinationList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Destination>(entity);
                    }
                }
                else if (chunk.Has(destinationReachedType))
                {
                    var destinationReachedArray = chunk.GetNativeArray(destinationReachedType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = destinationReachedArray[entityIndex].This;

                        if (m_RemoveDestinationList.Contains(entity)) continue;

                        m_RemoveDestinationList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Destination>(entity);
                    }
                }
            }

            m_RemoveDestinationList.Clear();

            chunkArray.Dispose();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveDestinationList.IsCreated)
            {
                m_RemoveDestinationList.Dispose();
            }
        }
    }
}