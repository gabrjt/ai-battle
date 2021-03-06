﻿using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayAttackingAnimationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<ViewReference, Attacking, AttackSpeed>().WithNone<Dead>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((ref ViewReference viewReference, ref AttackSpeed attackSpeed) =>
            {
                var animator = EntityManager.GetComponentObject<Animator>(viewReference.Value);
                animator.speed = attackSpeed.Value;
                animator.Play("Attacking");
            });
        }
    }
}