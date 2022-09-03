using UnityEngine;

public class RaceTrack : MonoBehaviour
{
    private float timeAtStart;
    private float timeAtFinish;

    public void ResetTimer()
    {
        timeAtStart = 0f;
        timeAtFinish = 0f;
    }

    public void StartTimer()
    {
        if (timeAtStart > timeAtFinish) return; // already started
        timeAtStart = Time.time;
    }

    public void StopTimer()
    {
        if (timeAtFinish > timeAtStart) return; // already finished
        timeAtFinish = Time.time;
    }

    public float ElapsedTime =>
        (timeAtFinish < timeAtStart) ?  (Time.time - timeAtStart)  :  (timeAtFinish - timeAtStart);

    private void Awake()
    {
        ResetTimer();
    }
}
