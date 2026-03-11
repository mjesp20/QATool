using UnityEngine;

namespace QATool.Sample
{
    public class TurretShooter : MonoBehaviour
    {

        public float minShootInterval = 1f;
        public float maxShootInterval = 3f;
        public float projectileSpeed = 10f;
        public float projectileLifetime = 5f;
        public float projectileDamage = 20f;

        private float timer;
        private float currentShootInterval;

        void Start()
        {
            currentShootInterval = Random.Range(minShootInterval, maxShootInterval);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= currentShootInterval)
            {
                Shoot();
                timer = 0f;
                currentShootInterval = Random.Range(minShootInterval, maxShootInterval);
            }
        }

        void Shoot()
        {
            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.transform.position = this.transform.position;
            QAToolProjectile projScript = proj.AddComponent<QAToolProjectile>();
            projScript.speed = projectileSpeed;
            projScript.lifetime = projectileLifetime;
            projScript.damage = projectileDamage;
            proj.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}