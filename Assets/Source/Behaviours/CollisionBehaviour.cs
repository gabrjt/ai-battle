using UnityEngine;

namespace Game.Behaviours
{
    public class CollisionBehaviour : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            // TODO: create damage entity

            Destroy(gameObject);
        }
    }
}