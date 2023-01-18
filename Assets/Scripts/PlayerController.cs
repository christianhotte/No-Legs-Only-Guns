using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerController : MonoBehaviour
{
    //Objects & Components:
    public static PlayerController main;   //Singleton instance of this playercontroller in scene
    public Transform leftHand;             //Transform for left hand controller
    public Transform rightHand;            //Transform for right hand controller
    internal Transform XROrigin;           //Top-level object in VR controller hierarchy
    internal Transform head;               //Transform for player head
    internal Rigidbody rb;                 //Rigidbody for player physics
    internal Gun[] weapons;                //Array of all weapons controlled by this player
    private ScreenShakeVR[] screenShakers; //Scripts used to shake screen

    //Runtime Variables:
    internal float leftWingValue;  //Amount by which player is currently squeezing left wing input axis
    internal float rightWingValue; //Amount by which player is currently squeezing right wing input axis

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialize:
        if (main == null) { main = this; } else { Destroy(gameObject); } //Singleton-ize this player object

        //Get objects & components:
        XROrigin = GetComponentInChildren<XROrigin>().transform; if (XROrigin == null) Debug.LogError(name + " needs an XR Origin (is XR set up on this player?)"); //Get XR origin and post warning if it is missing
        if (!XROrigin.TryGetComponent(out rb)) Debug.LogError(name + " is missing rigidBody component (should be on child named PlayerBody)");                      //Get rigidbody component and post warning if it cannot be found
        head = GetComponentInChildren<Camera>().transform;                                                                                                          //Get transform for player head
        weapons = GetComponentsInChildren<Gun>();                                                                                                                   //Get player weapons
        screenShakers = GetComponentsInChildren<ScreenShakeVR>();                                                                                                   //Get screen shaker scripts
    }

    //INPUT METHODS:
    public void OnLeftWingInput(InputAction.CallbackContext context)
    {
        leftWingValue = context.ReadValue<float>(); //Record input value
    }
    public void OnRightWingInput(InputAction.CallbackContext context)
    {
        rightWingValue = context.ReadValue<float>(); //Record input value
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Applies force to player rigidbody, main mode of locomotion.
    /// </summary>
    /// <param name="force">Amount and direction of force to apply to player.</param>
    public void ApplyKnockback(Vector3 force)
    {
        //Modify velocity:
        rb.velocity = force / rb.mass; //Instantly change velocity to impulse of given force

        //Cleanup:
        foreach (FollowBody follower in GetComponentsInChildren<FollowBody>()) follower.PerformUpdate(); //Immediately update followBody objects to alleviate jank
    }
    /// <summary>
    /// Shakes screen with given amount of intensity for given length of time (in seconds).
    /// </summary>
    public void ShakeScreen(float intensity, float time)
    {
        foreach (ScreenShakeVR shaker in screenShakers) shaker.Shake(intensity, time); //Trigger screenshake script on camera
    }
}
