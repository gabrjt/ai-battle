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
            m_InputField.text = m_MaxViewLODSqrDistance.Value.Value.ToString();
        }

        public void Confirm()
        {
            var viewVisibleSystem = World.Active.GetExistingManager<ViewVisibleSystem>();

            if (int.TryParse(m_InputField.text, out var inputFieldValue) && inputFieldValue >= 0)
            {
                var count = math.min(inputFieldValue, m_MaxValue);
                m_InputField.text = count.ToString();
                var maxViewLODSqrDistance = m_MaxViewLODSqrDistance.Value;
                maxViewLODSqrDistance.Value = count;
                m_MaxViewLODSqrDistance.Value = maxViewLODSqrDistance;
            }
            else
            {
                m_InputField.text = m_MaxViewLODSqrDistance.Value.Value.ToString();
            }
        }
    }
}