using UnityEngine;

namespace Game.MonoBehaviours
{
    public class Quit : MonoBehaviour
    {
        public void Execute()
        {
            Application.Quit();
        }
    }
}