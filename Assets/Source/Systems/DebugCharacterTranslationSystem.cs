using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    //[DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class DebugCharacterTranslationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                Any = new[] { ComponentType.ReadOnly<Knight>(), ComponentType.ReadOnly<OrcWolfRider>(), ComponentType.ReadOnly<Skeleton>() }
            });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var translationType = GetArchetypeChunkComponentType<Translation>(true);
            var knightType = GetArchetypeChunkComponentType<Knight>(true);
            var orcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true);
            var skeletonType = GetArchetypeChunkComponentType<Skeleton>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var translationArray = chunk.GetNativeArray(translationType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var translation = translationArray[entityIndex];

                    if (chunk.Has(knightType))
                    {
                        Debug.DrawRay(translation.Value, math.up(), Color.blue);
                    }
                    else if (chunk.Has(orcWolfRiderType))
                    {
                        Debug.DrawRay(translation.Value, math.up(), Color.magenta);
                    }
                    else if (chunk.Has(skeletonType))
                    {
                        Debug.DrawRay(translation.Value, math.up(), Color.black);
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}