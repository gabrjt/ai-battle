using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class HealthBarOwnerPositionSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref HealthBar healthBar, ref HealthBarOwnerPosition healthBarOwnerPosition) =>
            {
                healthBarOwnerPosition.Value = EntityManager.GetComponentData<Position>(healthBar.Owner).Value;
            });
        }
    }
}