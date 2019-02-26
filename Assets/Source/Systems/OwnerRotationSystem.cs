using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class OwnerRotationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Owner owner, ref OwnerRotation ownerRotation) =>
            {
                ownerRotation.Value = EntityManager.GetComponentData<Rotation>(owner.Value).Value;
            });
        }
    }
}