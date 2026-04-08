using UnityEngine;

namespace QATool
{
    public class QAToolQuestionPromptZone : MonoBehaviour
    {
        [SerializeField] private string prompt = "Enter your feedback:";
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);

        private bool hasTriggered = false;

        void Start()
        {
            if (GetComponent<Collider>() == null)
            {
                BoxCollider col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }
            else
            {
                GetComponent<Collider>().isTrigger = true;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;
            if (!other.CompareTag("Player")) return;

            hasTriggered = true;
            QAToolPlayerTracker.Instance.CreateFeedbackNotesWindow(prompt, gizmoColor);
        }

        void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
}   