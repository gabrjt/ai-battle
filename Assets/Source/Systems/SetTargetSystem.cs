using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class SetTargetSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_SetTargetList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<TargetFound>(), ComponentType.ReadOnly<Damaged>() }
            });

            m_SetTargetList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var targetFoundType = GetArchetypeChunkComponentType<TargetFound>(true);
            var damagedType = GetArchetypeChunkComponentType<Damaged>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(targetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(targetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var targetFound = targetFoundArray[entityIndex];
                        var entity = targetFound.This;

                        if (m_SetTargetList.Contains(entity)) continue;

                        m_SetTargetList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Target { Value = targetFound.Value });
                    }
                }

                if (chunk.Has(damagedType))
                {
                    var damagedArray = chunk.GetNativeArray(damagedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var damaged = damagedArray[entityIndex];
                        var entity = damaged.Target;

                        if (EntityManager.HasComponent<Target>(entity) || m_SetTargetList.Contains(entity)) continue;

                        m_SetTargetList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Target { Value = damaged.This });
                    }
                }
            }

            chunkArray.Dispose();

            m_SetTargetList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SetTargetList.IsCreated)
            {
                m_SetTargetList.Dispose();
            }
        }
    }
}