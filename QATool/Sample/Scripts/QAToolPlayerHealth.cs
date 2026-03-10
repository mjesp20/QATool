using UnityEngine;

namespace QATool
{
    public class QAToolPlayerHealth : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float currentHealth;

        void Start()
        {
            currentHealth = maxHealth;
        }

        public void Heal(float amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            Debug.Log("Healed: " + amount + " | Health: " + currentHealth);
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            Debug.Log("Damage: " + amount + " | Health: " + currentHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            Debug.Log("Player died");
        }
    }
}