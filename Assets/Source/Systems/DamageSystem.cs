using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DamageSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Damaged damaged) =>
            {
                //Debug.Log($"{damaged.This} attacked {damaged.Other} with {damaged.Value} damage");
            });
        }
    }
}