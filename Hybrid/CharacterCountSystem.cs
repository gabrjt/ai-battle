using Game.Components;
using TMPro;
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
            if (!HasSingleton<CharacterCount>() || !EntityManager.Exists(GetSingleton<CharacterCount>().Owner)) return; // TODO: remove this when RequireSingletonForUpdate is working.

            var characterCount = GetSingleton<CharacterCount>();

            if (!EntityManager.HasComponent<TextMeshProUGUI>(characterCount.Owner)) return;

            var count = m_Group.CalculateLength();

            characterCount.Value = count;
            SetSingleton(characterCount);

            var characterCountText = EntityManager.GetComponentObject<TextMeshProUGUI>(characterCount.Owner);
            characterCountText.text = $"{count:#0} Characters";
        }
    }
}