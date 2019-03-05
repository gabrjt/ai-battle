using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class SetCharacterCountInputFieldSingletonSystem : ComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<CharacterCountInputField>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<CharacterCountInputField>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var characterCountInputFieldType = GetArchetypeChunkComponentType<CharacterCountInputField>();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(characterCountInputFieldType))
                {
                    var entity = chunk.GetNativeArray(entityType)[0];
                    var characterCountInputField = chunk.GetNativeArray(characterCountInputFieldType)[0];
                    characterCountInputField.Owner = entity;

                    PostUpdateCommands.AddComponent(entity, new Initialized());
                    PostUpdateCommands.SetComponent(entity, characterCountInputField);

                    SetSingleton(characterCountInputField);
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