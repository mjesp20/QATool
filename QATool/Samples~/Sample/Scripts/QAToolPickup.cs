using System.Collections;
using UnityEngine;

namespace QATool.Sample
{
    public class QAToolPickup : MonoBehaviour
    {
        public static int collected = 1;
        public float cooldownDuration = 3f;

        public Material readyMaterial;
        public Material cooldownMaterial;

        private Renderer _renderer;

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                StartCoroutine(CooldownRoutine());
        }

        IEnumerator CooldownRoutine()
        {
            QAToolGlobals.Event(new System.Collections.Generic.Dictionary<string, object> { { "event", "Collected Blob" } });
            QAToolGlobals.SetFlagValue("CollectedBlobs", collected++);
            _renderer.material = cooldownMaterial;

            yield return new WaitForSeconds(cooldownDuration);

            _renderer.material = readyMaterial;
        }
    }
}