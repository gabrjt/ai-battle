using UnityEngine;

namespace Game.Behaviours
{
    public class Collision : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            Destroy(gameObject);
        }
    }
}