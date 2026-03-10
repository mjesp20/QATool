using UnityEngine;

namespace QATool
{
    public class QAToolBackAndForth : MonoBehaviour
    {
        public Transform pointA; 
        public Transform pointB; 
        public float speed = 3f;

        private Transform target;

        void Start()
        {
            if (pointA == null || pointB == null)
            {
                Debug.LogError("QAToolBackAndForth: Please assign both pointA and pointB!");
                enabled = false;
                return;
            }

            target = pointB; 
        }

        void Update()
        {
            if (pointA == null || pointB == null) return;

            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.01f)
            {
                target = target == pointB ? pointA : pointB;
            }
        }


        void OnDrawGizmos()
        {
            if (pointA != null && pointB != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pointA.position, pointB.position);
                Gizmos.DrawSphere(pointA.position, 0.2f);
                Gizmos.DrawSphere(pointB.position, 0.2f);
            }
        }
    }
}