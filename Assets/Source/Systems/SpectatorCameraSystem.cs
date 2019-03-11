using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class SpectatorCameraSystem : ComponentSystem
    {
        private ComponentGroup m_Group;
        private ComponentGroup m_TargetGroup;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<CameraArm, SpectatorCamera, Translation, Rotation>().ToComponentGroup();
            m_TargetGroup = Entities.WithAll<Translation>().WithNone<Dead, Destroy, Disabled>().ToComponentGroup();
            m_Random = new Random((uint)System.Environment.TickCount);

            RequireSingletonForUpdate<SpectatorCamera>();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((ref SpectatorCamera spectatorCamera, ref Translation translation, ref Rotation rotation) =>
            {
                if (!EntityManager.Exists(spectatorCamera.Target) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
                {
                    var targetArray = m_TargetGroup.ToEntityArray(Unity.Collections.Allocator.TempJob);
                    var target = targetArray[m_Random.NextInt(0, targetArray.Length)];

                    spectatorCamera.Target = target;
                    targetArray.Dispose();
                }
                else
                {
                    translation.Value = math.lerp(translation.Value, EntityManager.GetComponentData<Translation>(spectatorCamera.Target).Value, Time.deltaTime);
                    rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), spectatorCamera.RotationSpeed * Time.deltaTime));
                }
            });
        }
    }
}