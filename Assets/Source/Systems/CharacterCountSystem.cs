using Game.Components;
using TMPro;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class CharacterCountSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() }
            });

            RequireSingletonForUpdate<CharacterCount>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CharacterCount>()) return; // TODO: remove this when RequireSingletonForUpdate is working.

            var characterCount = GetSingleton<CharacterCount>();

            if (!EntityManager.HasComponent<TextMeshProUGUI>(characterCount.Owner)) return;

            var count = 0;

            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                count += chunkArray[chunkIndex].Count;
            }

            chunkArray.Dispose();

            characterCount.Value = count;
            SetSingleton(characterCount);

            var characterCountText = EntityManager.GetComponentObject<TextMeshProUGUI>(characterCount.Owner);
            characterCountText.text = $"{count:#0} Entities";
        }
    }
}