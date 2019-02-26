using Game.Components;
using Unity.Entities;
using UnityEngine.UI;

namespace Game.Systems
{
    public class HealthBarSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Image image, ref HealthBar healthBar) =>
            {
                image.fillAmount = EntityManager.GetComponentData<Health>(healthBar.Owner).Value / EntityManager.GetComponentData<MaxHealth>(healthBar.Owner).Value;
            });
        }
    }
}