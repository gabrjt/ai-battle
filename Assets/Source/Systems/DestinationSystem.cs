using Game.Components;
using Game.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(IdleSystem))]
    public class DestinationSystem : ComponentSystem
    {
        private ComponentGroup m_IdleDurationExpiredGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_IdleDurationExpiredGroup = Entities.WithAll<Components.Event, IdleDurationExpired>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;
            Entities.With(m_IdleDurationExpiredGroup).ForEach((ref IdleDurationExpired idleDurationExpired) =>
            {
                var entity = idleDurationExpired.This;

                if (!EntityManager.Exists(entity)) return;

                PostUpdateCommands.AddComponent(entity, new Destination { Value = terrain.GetRandomPosition() });
            });
        }
    }
}