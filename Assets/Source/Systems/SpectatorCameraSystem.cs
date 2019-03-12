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
        private ComponentGroup m_AddGroup;
        private ComponentGroup m_TargetGroup;
        private ComponentGroup m_Group;
        private ComponentGroup m_RemoveGroup;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = Entities.WithAll<CameraArm, SpectatorCamera, Translation, Rotation>().WithNone<CameraTarget>().ToComponentGroup();
            m_TargetGroup = Entities.WithAll<Character, Translation>().WithNone<Dead, Destroy, Disabled>().ToComponentGroup();
            m_Group = Entities.WithAll<SpectatorCamera, Transform, Rotation, CameraTarget>().ToComponentGroup();
            m_RemoveGroup = Entities.WithAll<CameraTarget>().ToComponentGroup();
            m_Random = new Random((uint)System.Environment.TickCount);

            RequireSingletonForUpdate<SpectatorCamera>();
        }

        protected override void OnUpdate()
        {
            var hasCameraTarget = HasSingleton<CameraTarget>();

            if (hasCameraTarget && !EntityManager.Exists(GetSingleton<CameraTarget>().Value))
            {
                EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<CameraTarget>());
            }

            if (!hasCameraTarget || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
            {
                Entities.With(m_AddGroup).ForEach((Entity entity, ref SpectatorCamera spectatorCamera, ref Translation translation, ref Rotation rotation) =>
                {
                    if (m_TargetGroup.CalculateLength() == 0) return;

                    var targetArray = m_TargetGroup.ToEntityArray(Unity.Collections.Allocator.TempJob);
                    var target = targetArray[m_Random.NextInt(0, targetArray.Length)];

                    PostUpdateCommands.AddComponent(entity, new CameraTarget { Value = target });
                    targetArray.Dispose();
                });
            }

            if (hasCameraTarget)
            {
                Entities.With(m_Group).ForEach((ref SpectatorCamera spectatorCamera, ref Translation translation, ref Rotation rotation, ref CameraTarget cameraTarget) =>
                {
                    translation.Value = math.lerp(translation.Value, EntityManager.GetComponentData<Translation>(cameraTarget.Value).Value, Time.deltaTime);
                    rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), spectatorCamera.RotationSpeed * Time.deltaTime));
                });
            }
        }
    }
}