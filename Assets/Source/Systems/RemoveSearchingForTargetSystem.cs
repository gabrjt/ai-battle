using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(RemoveBarrier))]
    public class RemoveSearchingForTargetSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_RemoveSearchingForTargetList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Target>(), ComponentType.Create<SearchingForTarget>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.Create<SearchingForTarget>(), ComponentType.ReadOnly<Dead>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<TargetFound>() }
            });

            m_RemoveSearchingForTargetList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var targetType = GetArchetypeChunkComponentType<Target>(true);
            var deadType = GetArchetypeChunkComponentType<Dead>(true);
            var targetFoundType = GetArchetypeChunkComponentType<TargetFound>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(targetType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (m_RemoveSearchingForTargetList.Contains(entity)) continue;

                        m_RemoveSearchingForTargetList.Add(entity);

                        PostUpdateCommands.RemoveComponent<SearchingForTarget>(entity);
                    }
                }
                else if (chunk.Has(deadType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (m_RemoveSearchingForTargetList.Contains(entity)) continue;

                        m_RemoveSearchingForTargetList.Add(entity);

                        PostUpdateCommands.RemoveComponent<SearchingForTarget>(entity);
                    }
                }
                else if (chunk.Has(targetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(targetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = targetFoundArray[entityIndex].This;

                        if (m_RemoveSearchingForTargetList.Contains(entity)) continue;

                        m_RemoveSearchingForTargetList.Add(entity);

                        PostUpdateCommands.RemoveComponent<SearchingForTarget>(entity);
                    }
                }
            }

            chunkArray.Dispose();

            m_RemoveSearchingForTargetList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveSearchingForTargetList.IsCreated)
            {
                m_RemoveSearchingForTargetList.Dispose();
            }
        }
    }
}