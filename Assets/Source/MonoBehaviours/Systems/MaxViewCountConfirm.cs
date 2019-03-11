using Game.Components;
using Game.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class MaxViewCountConfirm : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField m_InputField;

        [SerializeField]
        private MaxViewCountProxy m_MaxViewCount;

        private void Start()
        {
            var instantiateViewSystem = World.Active.GetExistingManager<InstantiateViewSystem>();
            instantiateViewSystem.m_MaxViewCount = m_MaxViewCount.Value.Value;
            m_InputField.text = instantiateViewSystem.m_MaxViewCount.ToString();
        }

        public void Confirm()
        {
            var instantiateAICharacterSystem = World.Active.GetExistingManager<InstantiateAICharacterSystem>();
            var instantiateViewSystem = World.Active.GetExistingManager<InstantiateViewSystem>();

            if (int.TryParse(m_InputField.text, out var inputFieldValue) && inputFieldValue >= 0)
            {
                var count = math.min(inputFieldValue, instantiateAICharacterSystem.m_MaxCount);
                m_InputField.text = count.ToString();
                instantiateViewSystem.m_MaxViewCount = count;
            }
            else
            {
                m_InputField.text = instantiateViewSystem.m_MaxViewCount.ToString();
            }
        }
    }
}