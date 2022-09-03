using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpeedText : MonoBehaviour
{
    public Car car;
    public TextMeshProUGUI speedText;
    
    void Update()
    {
        speedText.text = $"{(int)car.Speed:000}<size=30> mph";
    }
}
