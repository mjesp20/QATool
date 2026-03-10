using UnityEngine;

namespace QATool.Sample
{
    public class TurretShooter : MonoBehaviour
    {
        public GameObject projectilePrefab;
        Transform firePoint;

        public float minShootInterval = 1f;
        public float maxShootInterval = 3f;
        public float projectileSpeed = 10f;
        public float projectileLifetime = 5f;
        public float projectileDamage = 20f;

        private float timer;
        private float currentShootInterval;

        void Start()
        {
            firePoint = this.transform;
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
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            QAToolProjectile projScript = proj.GetComponent<QAToolProjectile>();
            if (projScript != null)
            {
                projScript.speed = projectileSpeed;
                projScript.lifetime = projectileLifetime;
                projScript.damage = projectileDamage;
            }
        }
    }
}