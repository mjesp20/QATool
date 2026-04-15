using UnityEngine;

namespace QATool.Sample
{
    public class QAToolPlayerMovement : MonoBehaviour
    {
        public float moveSpeed = 6f;

        private Rigidbody rb;
        private Vector3 movement;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            movement = new Vector3(-moveX, 0f, -moveZ).normalized;
        }

        void FixedUpdate()
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }
}