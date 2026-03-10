using UnityEngine;

namespace QATool
{
    public class QAToolHealthPickup : MonoBehaviour
    {
        public float healAmount = 50f;

        private void OnTriggerEnter(Collider other)
        {
            // Check if the object hitting the pickup is the player
            if (other.CompareTag("Player"))
            {
                QAToolPlayerHealth health = other.GetComponent<QAToolPlayerHealth>();
                if (health != null)
                {
                    health.Heal(healAmount);
                }

                // Destroy the pickup after being collected
                Destroy(gameObject);
            }
        }
    }
}