using Game.Components;
using TMPro;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class MaxViewCountSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<MaxViewCount, TextMeshProUGUI>().ToComponentGroup();

            RequireSingletonForUpdate<MaxViewCount>();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((TextMeshProUGUI text) =>
            {
                text.text = $"{GetSingleton<MaxViewCount>().Value:#0}";
            });
        }
    }
}