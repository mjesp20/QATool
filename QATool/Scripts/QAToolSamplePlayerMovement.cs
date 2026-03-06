using UnityEngine;
namespace QATool {
public class QAToolSamplePlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float turnSpeed = 120f;

    int jumpCount = 0;

    private Rigidbody _rb;
    private float moveInput = 0f;
    private float turnInput = 0f;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        moveInput = 0f;
        turnInput = 0f;

        // Forward / Backward
        if (Input.GetKey(KeyCode.W)) moveInput = 1f;
        if (Input.GetKey(KeyCode.S)) moveInput = -1f;

        // Turning
        if (Input.GetKey(KeyCode.A)) turnInput = -1f;
        if (Input.GetKey(KeyCode.D)) turnInput = 1f;

        if (Input.GetKeyDown(KeyCode.Space)) { QAToolGlobals.SetFlagValue("jumps", jumpCount++); }
    }

    void FixedUpdate()
    {
        // Move forward/backward
        Vector3 move = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + move);

        // Turn (only while moving, feels more like a car)
        if (moveInput != 0f)
        {
            float direction = moveInput > 0 ? 1f : -1f;
            float turn = turnInput * turnSpeed * direction * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            _rb.MoveRotation(_rb.rotation * turnRotation);
        }
    }
}
}