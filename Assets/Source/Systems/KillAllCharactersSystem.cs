using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(DeadBarrier))]
    public class KillAllCharactersSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            });
        }

        protected override void OnUpdate()
        {
            var mustKillAll = false;
            ForEach((ref KillAllCharacters killAllCharacters) =>
            {
                mustKillAll = true;
            });

            if (!mustKillAll) return;

            var entityArray = m_Group.ToEntityArray(Unity.Collections.Allocator.TempJob);

            for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                EntityManager.AddComponentData(entityArray[entityIndex], new Dead
                {
                    Duration = 5,
                    StartTime = Time.time
                });
            }

            entityArray.Dispose();
        }
    }
}