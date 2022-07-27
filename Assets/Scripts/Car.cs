using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Car : MonoBehaviour
{
    public float maxMotorTorque = 250f;
    public float maxBreakTorque = 1000f;
    public float maxSteerAngle = 30f;
    public WheelCollider wheelLeftFront;
    public WheelCollider wheelRightFront;
    public WheelCollider wheelLeftBack;
    public WheelCollider wheelRightBack;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float steerAngle = Input.GetAxis("Horizontal") * maxSteerAngle;
        float motorTorque = Input.GetAxis("Vertical") * maxMotorTorque;
        float brakeTorque = Input.GetButton("Jump") ? maxBreakTorque : 0f;
        
        float speed = Mathf.Abs(Vector3.Dot(body.velocity, transform.forward));
        steerAngle /= (1f + 0.25f * speed); // reducing steering angle as you move faster
        wheelLeftFront.steerAngle = steerAngle;
        wheelRightFront.steerAngle = steerAngle;

        if (motorTorque < 0f) motorTorque *= 0.2f; // make reverse gear slower
        wheelLeftFront.motorTorque = motorTorque;
        wheelRightFront.motorTorque = motorTorque;

        wheelLeftFront.brakeTorque = brakeTorque * 0.25f;
        wheelRightFront.brakeTorque = brakeTorque * 0.25f;
        wheelLeftBack.brakeTorque = brakeTorque;
        wheelRightBack.brakeTorque = brakeTorque;

        ApplyLocalPositionToVisuals(wheelLeftFront);
        ApplyLocalPositionToVisuals(wheelRightFront);
        ApplyLocalPositionToVisuals(wheelLeftBack);
        ApplyLocalPositionToVisuals(wheelRightBack);
    }

    private void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = collider.transform.GetChild(0);
        collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        visualWheel.transform.SetPositionAndRotation(position, rotation);
    }
}
