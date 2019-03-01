using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(RemoveBarrier))]
    public class RemoveIdleSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_RemoveIdleList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Idle>() },
                Any = new[] { ComponentType.ReadOnly<SearchingForDestination>(), ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<IdleTimeExpired>() }
            });

            m_RemoveIdleList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var characterType = GetArchetypeChunkComponentType<Character>(true);
            var idleTimeExpiredType = GetArchetypeChunkComponentType<IdleTimeExpired>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(characterType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (m_RemoveIdleList.Contains(entity)) continue;

                        m_RemoveIdleList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Idle>(entity);
                    }
                }
                else if (chunk.Has(idleTimeExpiredType))
                {
                    var idleTimeExpiredArray = chunk.GetNativeArray(idleTimeExpiredType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = idleTimeExpiredArray[entityIndex].This;

                        if (m_RemoveIdleList.Contains(entity)) continue;

                        m_RemoveIdleList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Idle>(entity);
                    }
                }
            }

            chunkArray.Dispose();

            m_RemoveIdleList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveIdleList.IsCreated)
            {
                m_RemoveIdleList.Dispose();
            }
        }
    }
}