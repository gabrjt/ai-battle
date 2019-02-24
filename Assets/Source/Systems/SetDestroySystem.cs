using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class SetDestroySystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_SetDestroyList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Dead>() },
                None = new[] { ComponentType.ReadOnly<Destroy>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Killed>() }
            });

            m_SetDestroyList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var deadType = GetArchetypeChunkComponentType<Dead>(true);
            var killedType = GetArchetypeChunkComponentType<Killed>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(deadType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (m_SetDestroyList.Contains(entity)) continue;

                        m_SetDestroyList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Destroy());
                    }
                }

                if (chunk.Has(killedType))
                {
                    var killedArray = chunk.GetNativeArray(killedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].Target;

                        if (m_SetDestroyList.Contains(entity)) continue;

                        m_SetDestroyList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Destroy());
                    }
                }
            }

            chunkArray.Dispose();

            m_SetDestroyList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SetDestroyList.IsCreated)
            {
                m_SetDestroyList.Dispose();
            }
        }
    }
}