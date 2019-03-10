using Game.Components;
using System.Collections;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;

#if UNITY_EDITOR

using UnityEditor;

#endif

using UnityEngine;
using Random = Unity.Mathematics.Random;

public class SpectatorCamera : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.Camera m_Camera;

    private LayerMask m_LayerMask;

    private int m_Layer;

    [SerializeField]
    private float m_RotationSpeed;

    [SerializeField]
    private Transform m_Target;

    private Entity m_TargetEntity;

    private EntityManager m_EntityManager;

    private Random m_Random;

    private void Start()
    {
        m_Random = new Random((uint)System.Environment.TickCount);

        m_EntityManager = World.Active.GetExistingManager<EntityManager>();

        m_LayerMask = LayerMask.NameToLayer("Entity");

        m_Layer = 1 << m_LayerMask;

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
        var targets = entities.Where(entity => m_EntityManager.HasComponent<Character>(entity) &&
            m_EntityManager.HasComponent<ViewReference>(entity) &&
            !m_EntityManager.HasComponent<Dying>(entity)).ToArray();

        if (targets.Length > 0)
        {
            m_TargetEntity = targets[m_Random.NextInt(0, targets.Length)];
            m_Target = m_EntityManager.GetComponentObject<Transform>(m_EntityManager.GetComponentData<ViewReference>(m_TargetEntity).Value);
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
        if (!m_Target || !m_EntityManager.Exists(m_TargetEntity) || m_EntityManager.HasComponent<Destroy>(m_TargetEntity) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
        {
            FindRandomTarget();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!Physics.Raycast(m_Camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity, m_Layer)) return;

            m_Target = hit.collider.transform;
            m_TargetEntity = hit.collider.GetComponent<GameObjectEntity>().Entity;

#if UNITY_EDITOR
            EditorGUIUtility.PingObject(m_Target.gameObject);
#endif
        }
    }

    private void LateUpdate()
    {
        if (!m_Target) return;

        transform.position = math.lerp(transform.position, m_Target.position, Time.deltaTime);
        transform.rotation = math.mul(math.normalize(transform.rotation), quaternion.AxisAngle(math.up(), m_RotationSpeed * Time.deltaTime));
    }
}