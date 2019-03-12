using Game.Components;
using Game.MonoBehaviours;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class PlayAttackAudioSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<Components.Event, Damaged>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((ref Damaged damaged) =>
            {
                var damager = damaged.This;

                if (!EntityManager.HasComponent<ViewReference>(damager)) return;

                EntityManager.GetComponentObject<Transform>(
                    EntityManager.GetComponentData<ViewReference>(damager).Value)
                    .GetComponentInChildren<AttackAudioPlayer>()
                    .PlayAtPoint(EntityManager.GetComponentData<Translation>(damager).Value, damaged.Value);
            });
        }
    }
}