using UnityEngine;

namespace QATool.Sample
{
    public class QAToolRotateObject : MonoBehaviour
    {
        public Vector3 rotationAxis = Vector3.up;
        public float rotationSpeed = 90f; // degrees per second

        void Update()
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
    }
}