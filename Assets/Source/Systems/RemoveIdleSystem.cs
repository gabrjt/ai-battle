using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    public class RemoveIdleSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<IdleTimeExpired>(), ComponentType.ReadOnly<TargetFound>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var idleTimeExpiredType = EntityManager.GetArchetypeChunkComponentType<IdleTimeExpired>(true);
            var targetFoundType = EntityManager.GetArchetypeChunkComponentType<TargetFound>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(idleTimeExpiredType))
                {
                    var idleTimeExpiredArray = chunk.GetNativeArray(idleTimeExpiredType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.RemoveComponent<Idle>(idleTimeExpiredArray[entityIndex].This);
                    }
                }
                else if (chunk.Has(targetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(targetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.RemoveComponent<Idle>(targetFoundArray[entityIndex].This);
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}