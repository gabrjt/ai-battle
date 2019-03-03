using Game.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Behaviours
{
    public class CharacterCountConfirmButton : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField m_CharacterCountInputField;

        [SerializeField]
        private int m_MaxTotalCount = 10000;

        public void Confirm()
        {
            if (int.TryParse(m_CharacterCountInputField.text, out var inputFieldCount) && inputFieldCount > 0)
            {
                World.Active.GetExistingManager<SpawnAICharacterSystem>().m_TotalCount = math.min(inputFieldCount, m_MaxTotalCount);
            }
        }
    }
}