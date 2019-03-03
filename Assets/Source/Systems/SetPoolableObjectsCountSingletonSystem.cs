using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SetBarrier))]
    public class SetPoolableObjectsCountSingletonSystem : ComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<PoolableObjectCount>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<PoolableObjectCount>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var poolableObjectCountType = GetArchetypeChunkComponentType<PoolableObjectCount>();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(poolableObjectCountType))
                {
                    var entity = chunk.GetNativeArray(entityType)[0];
                    var poolableObjectCount = chunk.GetNativeArray(poolableObjectCountType)[0];
                    poolableObjectCount.Owner = entity;
                    poolableObjectCount.Value = 0;

                    PostUpdateCommands.AddComponent(entity, new Initialized());
                    PostUpdateCommands.SetComponent(entity, poolableObjectCount);

                    SetSingleton(poolableObjectCount);
                }
                else
                {
                    var entity = chunk.GetNativeArray(entityType)[0];
                    PostUpdateCommands.RemoveComponent<Initialized>(entity);
                }
            }

            chunkArray.Dispose();
        }
    }
}