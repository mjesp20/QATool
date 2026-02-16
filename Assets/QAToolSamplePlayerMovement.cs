using UnityEngine;

public class QAToolSamplePlayerMovement : MonoBehaviour
{
    [SerializeField]
    float moveSpeed = 1;
    Rigidbody _rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
    void Update()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;

        Vector3 direction = new Vector3(x, 0f, z).normalized;

        _rb.AddForce(direction * moveSpeed, ForceMode.Force);
    }
}
