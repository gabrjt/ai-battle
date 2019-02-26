using Game.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Experimental.PlayerLoop;

namespace Game.Systems
{
    [UpdateAfter(typeof(PostLateUpdate))]
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