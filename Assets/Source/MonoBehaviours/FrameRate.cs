using TMPro;
using UnityEngine;

namespace Game.MonoBehaviours
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FrameRate : MonoBehaviour
    {
        private TextMeshProUGUI m_FrameRateText;

        private float m_FrameRate;

        private void Awake()
        {
            m_FrameRateText = GetComponent<TextMeshProUGUI>();
        }

        private void LateUpdate()
        {
            var frameRate = 1 / Time.deltaTime;
            m_FrameRate -= (m_FrameRate - frameRate) * Time.deltaTime;
            m_FrameRateText.text = $"{m_FrameRate:#0} FPS";
        }
    }
}