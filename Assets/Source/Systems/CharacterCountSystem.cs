using Game.Components;
using TMPro;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class CharacterCountSystem : ComponentSystem
    {
        private ComponentGroup m_Group;
        private ComponentGroup m_CharacterGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<CharacterCount, TextMeshProUGUI>().ToComponentGroup();
            m_CharacterGroup = Entities.WithAll<Character>().ToComponentGroup();

            RequireSingletonForUpdate<CharacterCount>();
        }

        protected override void OnUpdate()
        {
            var characterCount = GetSingleton<CharacterCount>();
            characterCount.Value = m_CharacterGroup.CalculateLength();
            SetSingleton(characterCount);

            Entities.With(m_Group).ForEach((TextMeshProUGUI characterCountText) =>
            {
                characterCountText.text = $"{characterCount.Value:#0}";
            });
        }
    }
}