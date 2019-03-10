using Game.Components;
using Game.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class MaxViewLODSqrDistanceConfirm : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField m_InputField;

        [SerializeField]
        private MaxViewLODSqrDistanceProxy m_MaxViewLODSqrDistance;

        [SerializeField]
        private int m_MaxValue = 10000 * 10000;

        private void Start()
        {
            var viewVisibleSystem = World.Active.GetExistingManager<ViewVisibleSystem>();
            viewVisibleSystem.m_MaxViewLODSqrDistance = m_MaxViewLODSqrDistance.Value.Value;
            m_InputField.text = viewVisibleSystem.m_MaxViewLODSqrDistance.ToString();
        }

        public void Confirm()
        {
            var viewVisibleSystem = World.Active.GetExistingManager<ViewVisibleSystem>();

            if (int.TryParse(m_InputField.text, out var inputFieldValue) && inputFieldValue >= 0)
            {
                var count = math.min(inputFieldValue, m_MaxValue);
                m_InputField.text = count.ToString();
                viewVisibleSystem.m_MaxViewLODSqrDistance = count;
            }
            else
            {
                m_InputField.text = viewVisibleSystem.m_MaxViewLODSqrDistance.ToString();
            }
        }
    }
}