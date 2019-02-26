using Game.Components;
using Unity.Entities;
using UnityEngine.UI;

namespace Game.Systems
{
    public class HealthBarSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Image image, ref HealthBar healthBar, ref Owner owner) =>
            {
                image.fillAmount = EntityManager.GetComponentData<Health>(owner.Value).Value / EntityManager.GetComponentData<MaxHealth>(owner.Value).Value;
            });
        }
    }
}