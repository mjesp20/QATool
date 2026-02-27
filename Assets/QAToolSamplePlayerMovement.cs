using UnityEngine;

public class QAToolSamplePlayerMovement : MonoBehaviour
{

    private void Start()
    {
        QAToolGlobals.FlagFilters.Add("jumpCount", new QAToolGlobals.FlagFilter() { enabled = true, op = QAToolGlobals.FilterOperator.GreaterThan, value = 10 });
    }
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float turnSpeed = 120f;

    int jumpCount = 0;
    int jumpyjumps = 100;
    void Update()
    {
        float moveInput = 0f;
        float turnInput = 0f;

        // Forward / Backward
        if (Input.GetKey(KeyCode.W)) moveInput = 1f;
        if (Input.GetKey(KeyCode.S)) moveInput = -1f;

        // Turning
        if (Input.GetKey(KeyCode.A)) turnInput = -1f;
        if (Input.GetKey(KeyCode.D)) turnInput = 1f;

        if (Input.GetKeyDown(KeyCode.Space)) { QAToolGlobals.SetFlagValue("jumps", jumpCount++); QAToolGlobals.SetFlagValue("jumpyjumps", jumpyjumps--); }


        // Move forward/backward
        transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);

        // Turn (only while moving, feels more like a car)
        if (moveInput != 0f)
        {
            float direction = moveInput > 0 ? 1f : -1f;
            transform.Rotate(Vector3.up * turnInput * turnSpeed * direction * Time.deltaTime);
        }
    }
}