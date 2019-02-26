using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class SetToOwnerRotationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref OwnerRotation ownerRotation, ref Rotation rotation) =>
            {
                rotation.Value = ownerRotation.Value;
            });
        }
    }
}