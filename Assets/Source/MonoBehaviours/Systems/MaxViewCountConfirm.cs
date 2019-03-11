using Game.Components;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class MaxViewCountConfirm : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_InputField;
        [SerializeField] private MaxViewCountProxy m_MaxViewCount;
        [SerializeField] private int m_MaxValue = 0xFFFF;

        private void Start()
        {
            m_InputField.text = m_MaxViewCount.Value.Value.ToString();
        }

        public void Confirm()
        {
            if (int.TryParse(m_InputField.text, out var inputFieldValue) && inputFieldValue >= 0)
            {
                var count = math.min(inputFieldValue, m_MaxValue);
                m_InputField.text = count.ToString();
                var maxViewCount = m_MaxViewCount.Value;
                maxViewCount.Value = count;
                m_MaxViewCount.Value = maxViewCount;
            }
            else
            {
                m_InputField.text = m_MaxViewCount.Value.Value.ToString();
            }
        }
    }
}