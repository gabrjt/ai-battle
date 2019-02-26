using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class OwnerPositionSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Owner owner, ref OwnerPosition ownerPosition) =>
            {
                ownerPosition.Value = EntityManager.GetComponentData<Position>(owner.Value).Value;
            });
        }
    }
}