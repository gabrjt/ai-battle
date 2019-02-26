using Game.Components;
using Unity.Entities;
using UnityEngine.UI;

namespace Game.Systems
{
    public class HealthBarSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<Image>(), ComponentType.ReadOnly<Owner>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((Image image, ref Owner owner) =>
            {
                image.fillAmount = EntityManager.GetComponentData<Health>(owner.Value).Value / EntityManager.GetComponentData<MaxHealth>(owner.Value).Value;
            }, m_Group);
        }
    }
}