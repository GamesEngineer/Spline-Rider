using System;
using UnityEngine;
using UnityEngine.Events;


public class ColliderTrigger : MonoBehaviour
{
    [Serializable]
    public class ColliderEvent : UnityEvent<Collider> { }

    public ColliderEvent OnEnter;
    public ColliderEvent OnLeave;

    private void OnTriggerEnter(Collider other) => OnEnter?.Invoke(other);
    private void OnTriggerExit(Collider other) => OnLeave?.Invoke(other);
}
