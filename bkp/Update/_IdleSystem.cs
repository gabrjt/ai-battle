using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class IdleSystem : ComponentSystem
    {
        
        private ComponentGroup m_ShouldBeIdleGroup;
        private ComponentGroup m_ShouldAddIdleDurationGroup;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_ShouldBeIdleGroup = Entities.WithAll<Character>().WithNone<Idle, Destination, Target, Dying>().ToComponentGroup();
            m_ShouldAddIdleDurationGroup = Entities.WithAll<Idle>().WithNone<IdleDuration>().ToComponentGroup();
            m_Random = new Random((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_ShouldBeIdleGroup, ComponentType.ReadWrite<Idle>());
            
            Entities.With(m_ShouldAddIdleDurationGroup).ForEach((Entity entity) =>
            {
                PostUpdateCommands.AddComponent(entity, new IdleDuration { Value = m_Random.NextFloat(1, 5) });
            });            
        }
    }
}