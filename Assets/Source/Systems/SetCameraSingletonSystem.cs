using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class SetCameraSingletonSystem : ComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<CameraSingleton>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<CameraSingleton>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var cameraSingletonType = GetArchetypeChunkComponentType<CameraSingleton>();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(cameraSingletonType))
                {
                    var entity = chunk.GetNativeArray(entityType)[0];
                    var cameraSingleton = chunk.GetNativeArray(cameraSingletonType)[0];
                    cameraSingleton.Owner = entity;

                    PostUpdateCommands.AddComponent(entity, new Initialized());
                    PostUpdateCommands.SetComponent(entity, cameraSingleton);

                    SetSingleton(cameraSingleton);
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