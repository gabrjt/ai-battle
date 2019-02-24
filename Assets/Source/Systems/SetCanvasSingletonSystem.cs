using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class SetCanvasSingletonSystem : ComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<CanvasSingleton>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<CanvasSingleton>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var canvasSingletonType = GetArchetypeChunkComponentType<CanvasSingleton>();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(canvasSingletonType))
                {
                    var entity = chunk.GetNativeArray(entityType)[0];
                    var canvasSingleton = chunk.GetNativeArray(canvasSingletonType)[0];
                    canvasSingleton.Owner = entity;

                    PostUpdateCommands.AddComponent(entity, new Initialized());
                    PostUpdateCommands.SetComponent(entity, canvasSingleton);

                    SetSingleton(canvasSingleton);
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