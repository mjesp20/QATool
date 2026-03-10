using UnityEngine;

namespace QATool.Sample
{
    public class QAToolLaserDamage : MonoBehaviour
    {
        public float damagePerSecond = 20f;

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                QAToolPlayerHealth health = other.GetComponent<QAToolPlayerHealth>();

                if (health != null)
                {
                    health.TakeDamage(damagePerSecond * Time.deltaTime);
                }
            }
        }
    }
}