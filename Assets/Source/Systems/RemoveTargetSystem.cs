using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class RemoveTargetSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_RemoveTargetList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Target>() },
                Any = new[] { ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Killed>() }
            });

            m_RemoveTargetList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var deadType = GetArchetypeChunkComponentType<Dead>(true);
            var targetType = GetArchetypeChunkComponentType<Target>(true);
            var killedType = GetArchetypeChunkComponentType<Killed>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(targetType) || chunk.Has(deadType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);
                    var targetArray = chunk.GetNativeArray(targetType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var target = targetArray[entityIndex].Value;

                        if (!EntityManager.HasComponent<Dead>(entity) && (EntityManager.Exists(target) || m_RemoveTargetList.Contains(entity) && !EntityManager.HasComponent<Dead>(target))) continue;

                        m_RemoveTargetList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Target>(entity);
                    }
                }
                else if (chunk.Has(killedType))
                {
                    var killedArray = chunk.GetNativeArray(killedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].This;

                        if (!EntityManager.HasComponent<Target>(entity) || m_RemoveTargetList.Contains(entity)) continue;

                        m_RemoveTargetList.Add(entity);

                        PostUpdateCommands.RemoveComponent<Target>(entity);
                    }
                }
            }

            chunkArray.Dispose();

            m_RemoveTargetList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveTargetList.IsCreated)
            {
                m_RemoveTargetList.Dispose();
            }
        }
    }
}