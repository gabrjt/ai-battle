using Game.Components;
using System.Collections;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

public class SpectatorCameraBehaviour : MonoBehaviour
{
    [SerializeField]
    private float m_RotationSpeed;

    [SerializeField]
    private Transform m_Target;

    private Entity m_TargetEntity;

    private EntityManager m_EntityManager;

    private MRandom m_Random;

    private void Start()
    {
        m_Random = new MRandom((uint)System.Environment.TickCount);

        m_EntityManager = World.Active.GetExistingManager<EntityManager>();

        StartCoroutine(FindRandomTargetRoutine());
    }

    private IEnumerator FindRandomTargetRoutine()
    {
        while (!FindRandomTarget())
        {
            yield return null;
        }
    }

    private bool FindRandomTarget()
    {
        var targetFound = false;

        var entities = m_EntityManager.GetAllEntities();
        var targets = entities.Where(entity => m_EntityManager.HasComponent<Character>(entity) && !m_EntityManager.HasComponent<Dead>(entity) && m_EntityManager.HasComponent<Transform>(entity)).ToArray();

        if (targets.Length > 0)
        {
            m_TargetEntity = targets[m_Random.NextInt(0, targets.Length)];
            m_Target = m_EntityManager.GetComponentObject<Transform>(m_TargetEntity);
            targetFound = true;
        }
        else
        {
            m_Target = null;
            m_TargetEntity = default;
        }

        entities.Dispose();

        return targetFound;
    }

    private void Update()
    {
        if (!m_Target || m_EntityManager.HasComponent<Destroy>(m_TargetEntity) || Input.GetKeyDown(KeyCode.Space))
        {
            FindRandomTarget();
        }
    }

    private void LateUpdate()
    {
        if (!m_Target) return;

        transform.position = m_Target.position;
        transform.rotation = math.mul(math.normalize(transform.rotation), quaternion.AxisAngle(math.up(), m_RotationSpeed * Time.deltaTime));
    }
}