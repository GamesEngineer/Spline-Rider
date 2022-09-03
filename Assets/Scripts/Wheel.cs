using UnityEngine;

public class Wheel : MonoBehaviour
{
    public Transform visual;
    private WheelCollider wheel;

    private void Awake()
    {
        wheel = GetComponent<WheelCollider>();
    }

    void FixedUpdate()
    {
        wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
        visual.SetPositionAndRotation(position, rotation);
    }
}
