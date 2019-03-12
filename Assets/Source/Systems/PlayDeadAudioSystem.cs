using Game.Components;
using Game.MonoBehaviours;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class PlayDeadAudioSystem : ComponentSystem
    {
        private struct AudioPlayed : ISystemStateSharedComponentData { }

        private ComponentGroup m_AddGroup;
        private ComponentGroup m_RemoveGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = Entities.WithAll<ViewReference, Dead>().WithNone<AudioPlayed>().ToComponentGroup();
            m_RemoveGroup = Entities.WithAll<AudioPlayed>().WithNone<Dead>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_AddGroup).ForEach((Entity entity) =>
            {
                EntityManager.GetComponentObject<Transform>(
                    EntityManager.GetComponentData<ViewReference>(entity).Value)
                    .GetComponentInChildren<DeadAudioPlayer>()
                    .PlayAtPoint(EntityManager.GetComponentData<Translation>(entity).Value);
            });

            EntityManager.AddComponent(m_AddGroup, ComponentType.ReadWrite<AudioPlayed>());
            EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<AudioPlayed>());
        }
    }
}