using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayAttackingAnimationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref ViewReference viewReference, ref Target target, ref Attack attack) =>
            {
                EntityManager.GetComponentObject<Animator>(viewReference.Value).Play("Attacking");
            });
        }
    }
}