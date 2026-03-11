using UnityEngine;

namespace QATool.Sample
{
    public class QAToolFireball : MonoBehaviour
    {
        public float fallSpeed = 5f;
        public float destroyY = -10f;
        public float damage = 20f;

        private Collider col;

        void Start()
        {
            // Make collider a trigger so it can detect player
            col = GetComponent<Collider>();
            if (col == null)
                col = gameObject.AddComponent<SphereCollider>();

            col.isTrigger = true;
        }

        void Update()
        {
            // Move down
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            // Destroy if below threshold
            if (transform.position.y < destroyY)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Try to get the player's health component
                QAToolPlayerHealth health = other.GetComponent<QAToolPlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                }

                // Destroy fireball after hitting player
                Destroy(gameObject);
            }
        }
    }
}