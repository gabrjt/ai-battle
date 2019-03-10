using Game.Components;
using Game.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class CharacterCountConfirm : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField m_InputField;

        [SerializeField]
        private CharacterCountProxy m_CharacterCount;

        [SerializeField]
        private int m_MaxValue = 0xFFFF;

        private void Start()
        {
            var instantiateAICharacterSystem = World.Active.GetExistingManager<InstantiateAICharacterSystem>();
            instantiateAICharacterSystem.m_MaxCount = m_CharacterCount.Value.MaxValue;
            m_InputField.text = instantiateAICharacterSystem.m_MaxCount.ToString();
        }

        public void Confirm()
        {
            var instantiateAICharacterSystem = World.Active.GetExistingManager<InstantiateAICharacterSystem>();

            if (int.TryParse(m_InputField.text, out var inputFieldValue) && inputFieldValue >= 0)
            {
                var count = math.min(inputFieldValue, m_MaxValue);
                m_InputField.text = count.ToString();
                instantiateAICharacterSystem.m_MaxCount = count;
            }
            else
            {
                m_InputField.text = instantiateAICharacterSystem.m_MaxCount.ToString();
            }
        }
    }
}