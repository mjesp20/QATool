using UnityEngine;
using QATool;

public class QAToolTurretShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;

    public float minShootInterval = 1f;
    public float maxShootInterval = 3f;

    public float projectileSpeed = 10f;
    public float projectileLifetime = 5f;

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
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Projectile projectileScript = proj.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(projectileSpeed, projectileLifetime);
        }
    }
}