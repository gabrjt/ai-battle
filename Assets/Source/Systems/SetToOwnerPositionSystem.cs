using Game.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Experimental.PlayerLoop;

namespace Game.Systems
{
    [UpdateAfter(typeof(PostLateUpdate))]
    public class SetToOwnerPositionSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref OwnerPosition ownerPosition, ref Offset offset, ref Position position) =>
            {
                position.Value = ownerPosition.Value + offset.value;
            });
        }
    }
}