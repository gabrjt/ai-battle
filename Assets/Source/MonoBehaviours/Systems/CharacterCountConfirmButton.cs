using Game.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class CharacterCountConfirmButton : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField m_CharacterCountInputField;

        [SerializeField]
        private int m_MaxTotalCount = 0xFFFF;

        public void Confirm()
        {
            if (int.TryParse(m_CharacterCountInputField.text, out var inputFieldCount) && inputFieldCount > 0)
            {
                var count = math.min(inputFieldCount, m_MaxTotalCount);

                m_CharacterCountInputField.text = count.ToString();

                var instantiateAICharacterSystem = World.Active.GetExistingManager<InstantiateAICharacterSystem>();

                instantiateAICharacterSystem.m_LastTotalCount = instantiateAICharacterSystem.m_TotalCount;
                instantiateAICharacterSystem.m_TotalCount = count;
            }
        }
    }
}