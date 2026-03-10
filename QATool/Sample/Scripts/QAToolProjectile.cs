using UnityEngine;

namespace QATool
{
    public class QAToolProjectile : MonoBehaviour
    {
        public float speed = 10f;
        public float lifetime = 5f;
        public float damage = 20f;

        private float timer;

        void Update()
        {
            // Move forward in local Z+
            transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);

            // Self-destruct after lifetime
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Damage player
            if (other.CompareTag("Player"))
            {
                QAToolPlayerHealth health = other.GetComponent<QAToolPlayerHealth>();
                if (health != null)
                    health.TakeDamage(damage);
            }

            // Destroy on collision with anything
            Destroy(gameObject);
        }
    }
}