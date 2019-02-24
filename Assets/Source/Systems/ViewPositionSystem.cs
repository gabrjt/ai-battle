using Game.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Experimental.PlayerLoop;

namespace Game.Systems
{
    [UpdateAfter(typeof(PostLateUpdate))]
    public class ViewPositionSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref View view, ref Position position) =>
            {
                if (EntityManager.HasComponent<Position>(view.Owner))
                {
                    position.Value = EntityManager.GetComponentData<Position>(view.Owner).Value + view.Offset;
                }
            });
        }
    }
}