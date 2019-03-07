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

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<CharacterCount>(), ComponentType.ReadWrite<TextMeshProUGUI>() }
            });

            m_CharacterGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() }
            });

            RequireSingletonForUpdate<CharacterCount>();
        }

        protected override void OnUpdate()
        {
            var count = m_CharacterGroup.CalculateLength();

            ForEach((TextMeshProUGUI characterCountText, ref CharacterCount characterCount) =>
            {
                characterCount.Value = count;
                characterCountText.text = $"{count:#0} Characters";
            }, m_Group);
        }
    }
}