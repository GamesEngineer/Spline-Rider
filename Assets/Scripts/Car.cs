using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Car : MonoBehaviour
{
    public float maxMotorTorque = 250f;
    public float maxBreakTorque = 1000f;
    public float maxSteerAngle = 30f;
    public float steeringAttenuation = 0.25f;
    public WheelCollider wheelLeftFront;
    public WheelCollider wheelRightFront;
    public WheelCollider wheelLeftBack;
    public WheelCollider wheelRightBack;
    public Material breakLightMaterial;

    private Rigidbody body;

    public float Speed => body ? body.velocity.magnitude * 2.23694f : 0f; // mph

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    void FixedUpdate()
    {
        float steerAngle = Input.GetAxis("Horizontal") * maxSteerAngle;
        float motorTorque = Input.GetAxis("Vertical") * maxMotorTorque;
        float brakeTorque = Input.GetButton("Jump") ? maxBreakTorque : 0f;
        
        float speed = Vector3.Dot(body.velocity, transform.forward);

        if (motorTorque < 0f)
        {
            motorTorque *= 0.2f; // make reverse gear slower
            if (speed > 0.01f || brakeTorque > 0f) breakLightMaterial.EnableKeyword("_EMISSION");
            else if (speed <= 0f) breakLightMaterial.DisableKeyword("_EMISSION");
        }
        else
        {
            if (speed < -0.01f || brakeTorque > 0f) breakLightMaterial.EnableKeyword("_EMISSION");
            else if (speed >= 0f) breakLightMaterial.DisableKeyword("_EMISSION");
        }

        float leftSlip = 0f;
        float rightSlip = 0f;

        WheelHit hit;
        if (wheelLeftFront.GetGroundHit(out hit))
        {
            leftSlip = hit.forwardSlip + hit.sidewaysSlip;
        }

        if (wheelRightFront.GetGroundHit(out hit))
        {
            rightSlip = hit.forwardSlip + hit.sidewaysSlip;
        }

        float traction = 1f - Mathf.Clamp01(Mathf.Max(leftSlip, rightSlip) * 2f);
        steerAngle /= (1f + steeringAttenuation * Mathf.Abs(speed)); // reducing steering angle as you move faster

        wheelLeftFront.steerAngle = steerAngle;
        wheelLeftFront.motorTorque = motorTorque * traction;
        wheelLeftFront.brakeTorque = brakeTorque * 0.25f;

        wheelRightFront.steerAngle = steerAngle;
        wheelRightFront.motorTorque = motorTorque * traction;
        wheelRightFront.brakeTorque = brakeTorque * 0.25f;

        wheelLeftBack.brakeTorque = brakeTorque;

        wheelRightBack.brakeTorque = brakeTorque;
    }
}
