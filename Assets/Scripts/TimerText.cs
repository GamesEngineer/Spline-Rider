using System;
using UnityEngine;
using TMPro;

public class TimerText : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public RaceTrack raceTrack;

    private void Update()
    {
        if (timerText == null || raceTrack == null) return;
        var ts = TimeSpan.FromSeconds(raceTrack.ElapsedTime);
        timerText.text = $"{ts:mm\\:ss\\.ff}";
    }
}
