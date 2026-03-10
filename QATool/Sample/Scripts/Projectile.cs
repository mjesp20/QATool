using UnityEngine;

namespace QATool
{
    public class Projectile : MonoBehaviour
    {
        private float speed;
        private float lifetime;
        public float damage = 20f;

        public void Initialize(float projectileSpeed, float projectileLifetime, float projectileDamage = 20f)
        {
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            damage = projectileDamage;

            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {

            if (other.CompareTag("Player"))
            {
                QAToolPlayerHealth health = other.GetComponent<QAToolPlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                }
            }
            
            Destroy(gameObject);
        }
    }
}