using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class ViewRotationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref View view, ref Rotation rotation) =>
            {
                if (EntityManager.HasComponent<Rotation>(view.Owner))
                {
                    rotation.Value = EntityManager.GetComponentData<Rotation>(view.Owner).Value;
                }
            });
        }
    }
}