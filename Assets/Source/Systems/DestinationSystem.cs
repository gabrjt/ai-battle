using Game.Components;
using Game.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DestinationSystem : ComponentSystem
    {
        private ComponentGroup m_IdleDurationExpiredGroup;
        private ComponentGroup m_TargetDestinationGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

           m_IdleDurationExpiredGroup = Entities.WithAll<Components.Event, IdleDurationExpired>().ToComponentGroup();
           m_TargetDestinationGroup = Entities.WithAll<Target>().WithNone<Destination>().ToComponentGroup();
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

            var translationFromEntity = GetComponentDataFromEntity<Translation>(true);
            Entities.With(m_TargetDestinationGroup).ForEach((Entity entity, ref Target target) =>
            {
                PostUpdateCommands.AddComponent(entity, new Destination { Value = translationFromEntity[target.Value].Value });
            });
        }
    }
}