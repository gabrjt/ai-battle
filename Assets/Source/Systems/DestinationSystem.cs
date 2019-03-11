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
        private ComponentGroup m_AddGroup;
        private ComponentGroup m_RemoveGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = Entities.WithAll<Components.Event, IdleDurationExpired>().ToComponentGroup();
            m_RemoveGroup = Entities.WithAll<Destination, Dead>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;
            Entities.With(m_AddGroup).ForEach((ref IdleDurationExpired idleDurationExpired) =>
            {
                var entity = idleDurationExpired.This;

                if (!EntityManager.Exists(entity)) return;

                PostUpdateCommands.AddComponent(entity, new Destination { Value = terrain.GetRandomPosition() });
            });

            if (m_RemoveGroup.CalculateLength() > 0)
            {
                EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<Destination>());
            }
        }
    }
}