using UnityEngine;

public class SkidMarks : MonoBehaviour
{
    public float maxSideSlip = 0.3f;
    public float maxRotationalSlip = 0.3f;

    private WheelCollider wheel;
    private TrailRenderer trail;

    private void Awake()
    {
        wheel = GetComponentInParent<WheelCollider>();
        trail = GetComponent<TrailRenderer>();
    }

    void FixedUpdate()
    {
        if (!wheel.GetGroundHit(out WheelHit hit))
        {
            trail.emitting = false;
            return;
        }

        transform.position = hit.point + hit.normal * 0.08f;
        trail.emitting = Mathf.Abs(hit.forwardSlip) > maxRotationalSlip || Mathf.Abs(hit.sidewaysSlip) > maxSideSlip;
    }
}
