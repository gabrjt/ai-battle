using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SetBarrier))]
    public class SetCharacterCountSingletonSystem : ComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<CharacterCount>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<CharacterCount>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var characterCountType = GetArchetypeChunkComponentType<CharacterCount>();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(characterCountType))
                {
                    var entity = chunk.GetNativeArray(entityType)[0];
                    var characterCount = chunk.GetNativeArray(characterCountType)[0];
                    characterCount.Owner = entity;
                    characterCount.Value = 0;

                    PostUpdateCommands.AddComponent(entity, new Initialized());
                    PostUpdateCommands.SetComponent(entity, characterCount);

                    SetSingleton(characterCount);
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