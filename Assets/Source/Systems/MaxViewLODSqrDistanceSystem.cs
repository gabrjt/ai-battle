using Game.Components;
using TMPro;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class MaxViewLODSqrDistanceSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<MaxViewLODSqrDistance, TextMeshProUGUI>().ToComponentGroup();

            RequireSingletonForUpdate<MaxViewLODSqrDistance>();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((TextMeshProUGUI characterCountText) =>
            {
                characterCountText.text = $"{GetSingleton<MaxViewLODSqrDistance>().Value:#0}";
            });
        }
    }
}