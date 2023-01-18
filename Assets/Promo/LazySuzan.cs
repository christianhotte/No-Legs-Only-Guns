using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazySuzan : MonoBehaviour
{
    //SETTINGS:
    [Header("Settings:")]
    [SerializeField, Tooltip("Degrees per second at which system will rotate")] private float rotationSpeed;
    [SerializeField, Tooltip("Use local Z axis instead of world up")]           private bool useLocalZ;

    //RUNTIME METHODS:
    void Update()
    {
        #pragma warning disable CS0618
        transform.RotateAround(transform.position, useLocalZ ? transform.forward : Vector3.up, rotationSpeed * Time.deltaTime);
        #pragma warning restore CS0618
    }
}
