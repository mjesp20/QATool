using UnityEngine;

namespace QATool.Sample
{
    public class QAToolPlayerHealth : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float currentHealth;

        // New field to track accumulated damage
        private float accumulatedDamage = 0f;
        public float debugThreshold = 10f; // only log every 10 damage

        void Start()
        {
            currentHealth = maxHealth;
        }

        public void Heal(float amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            QAToolGlobals.SetFlagValue("Health", currentHealth);

            Debug.Log("Healed: " + amount + " | Health: " + currentHealth);
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            QAToolGlobals.SetFlagValue("Health", currentHealth);
            //Debug.Log("Damage: " + amount + " | Health: " + currentHealth);

            // --- Accumulate damage for meaningful logging ---
            accumulatedDamage += amount;

            if (accumulatedDamage >= debugThreshold)
            {
                Debug.Log($"Player lost {accumulatedDamage} health | Current: {currentHealth}");
                QAToolGlobals.SetFlagValue("Meaningful Damage Taken", accumulatedDamage);
                accumulatedDamage = 0f; // reset counter
            }

            // Optional: if you want to also handle leftover damage less than threshold on death
            if (currentHealth <= 0f && accumulatedDamage > 0f)
            {
                Debug.Log($"Player lost remaining {accumulatedDamage} health before death");
                accumulatedDamage = 0f;
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            Debug.Log("Player died");
            QAToolGlobals.Event(new System.Collections.Generic.Dictionary<string, object> { { "event", "Player died" } });
            Camera.main.gameObject.transform.parent = null;
            Destroy(gameObject);
        }
    }
}