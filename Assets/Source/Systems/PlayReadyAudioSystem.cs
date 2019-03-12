using Game.Components;
using Game.MonoBehaviours;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class PlayReadyAudioSystem : ComponentSystem
    {
        private struct AudioPlayed : ISystemStateSharedComponentData { }

        private ComponentGroup m_AddGroup;
        private ComponentGroup m_RemoveGroup;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = Entities.WithAll<ViewReference, Idle>().WithNone<AudioPlayed>().ToComponentGroup();
            m_RemoveGroup = Entities.WithAll<AudioPlayed>().WithNone<Idle>().ToComponentGroup();
            m_Random = new Random((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            Entities.With(m_AddGroup).ForEach((Entity entity) =>
            {
                if (m_Random.NextFloat() > 0.1f) return;

                EntityManager.GetComponentObject<Transform>(
                    EntityManager.GetComponentData<ViewReference>(entity).Value)
                    .GetComponentInChildren<ReadyAudioPlayer>()
                    .PlayAtPoint(EntityManager.GetComponentData<Translation>(entity).Value);
            });

            EntityManager.AddComponent(m_AddGroup, ComponentType.ReadWrite<AudioPlayed>());
            EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<AudioPlayed>());
        }
    }
}