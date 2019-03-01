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
            var healthFromOwner = GetComponentDataFromEntity<Health>(true);
            var maxHealthFromOwner = GetComponentDataFromEntity<MaxHealth>(true);

            ForEach((Image image, ref Owner owner) =>
            {
                if (!healthFromOwner.Exists(owner.Value) || !maxHealthFromOwner.Exists(owner.Value)) return;

                image.fillAmount = healthFromOwner[owner.Value].Value / maxHealthFromOwner[owner.Value].Value;
            }, m_Group);
        }
    }
}