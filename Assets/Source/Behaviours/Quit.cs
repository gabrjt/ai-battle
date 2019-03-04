using UnityEngine;

namespace Game.Behaviours
{
    public class Quit : MonoBehaviour
    {
        public void Execute()
        {
            Application.Quit();
        }
    }
}