using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class SetAttackDestroySystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_SetDestroyList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<Collided>(), ComponentType.ReadOnly<MaxDistanceReached>() }
            });

            m_SetDestroyList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var collidedType = GetArchetypeChunkComponentType<Collided>(true);
            var MaxDistanceReachedType = GetArchetypeChunkComponentType<MaxDistanceReached>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(collidedType))
                {
                    var collidedArray = chunk.GetNativeArray(collidedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = collidedArray[entityIndex].This;

                        if (m_SetDestroyList.Contains(entity)) continue;

                        m_SetDestroyList.Add(entity);

                        //PostUpdateCommands.DestroyEntity(entity);

                        PostUpdateCommands.AddComponent(entity, new Destroy());
                        PostUpdateCommands.AddComponent(entity, new Disabled());
                    }
                }
                else if (chunk.Has(MaxDistanceReachedType))
                {
                    var MaxDistanceArray = chunk.GetNativeArray(MaxDistanceReachedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = MaxDistanceArray[entityIndex].This;

                        if (m_SetDestroyList.Contains(entity)) continue;

                        m_SetDestroyList.Add(entity);

                        //PostUpdateCommands.DestroyEntity(entity);

                        PostUpdateCommands.AddComponent(entity, new Destroy());
                        PostUpdateCommands.AddComponent(entity, new Disabled());
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