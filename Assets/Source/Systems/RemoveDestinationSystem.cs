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
                All = new[] { ComponentType.ReadOnly<Destination>() },
                Any = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationReached>(), ComponentType.ReadOnly<Killed>() }
            });

            m_RemoveDestinationList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var targetType = GetArchetypeChunkComponentType<Target>(true);
            var deadType = GetArchetypeChunkComponentType<Dead>(true);
            var destinationReachedType = GetArchetypeChunkComponentType<DestinationReached>(true);
            var killedType = GetArchetypeChunkComponentType<Killed>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(targetType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);
                    var targetArray = chunk.GetNativeArray(targetType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var target = targetArray[entityIndex];

                        if (!EntityManager.HasComponent<Dead>(target.Value) || m_RemoveDestinationList.Contains(entity)) continue;

                        m_RemoveDestinationList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Destination>(entity);
                    }
                }

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

                if (chunk.Has(destinationReachedType))
                {
                    var destinationReachedArray = chunk.GetNativeArray(destinationReachedType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = destinationReachedArray[entityIndex].This;

                        if (!EntityManager.HasComponent<Destination>(entity) || m_RemoveDestinationList.Contains(entity)) continue;

                        m_RemoveDestinationList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Destination>(entity);
                    }
                }

                if (chunk.Has(killedType))
                {
                    var killedArray = chunk.GetNativeArray(killedType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].This;

                        if (!EntityManager.HasComponent<Destination>(entity) || m_RemoveDestinationList.Contains(entity)) continue;

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