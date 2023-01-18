using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBody : MonoBehaviour
{
    //DESCRIPTION: Takes transform movement of target object and turns it into identical velocity-based rigidbody movement on this object

    //Objects & Components:
    internal Transform target;            //Transform position for this rigidbody object to follow
    internal Rigidbody velocityReference; //Optional reference to another rigidbody which allows final velocity/position to be corrected for
    private Rigidbody rb;                 //Local rigidbody component

    //Settings:
    [SerializeField, Tooltip("Reduces apparent lag when moving by countering velocity (of given reference)")] private float velocityCompensation = 0.01f;
    [Tooltip("Distance from actual target by which effective target is offset")]                              public Vector3 offset;

    private void Awake()
    {
        //Get objects & components:
        if (!TryGetComponent(out rb)) { Debug.LogError("Object with FollowBody script needs a rigidbody component"); Destroy(this); } //Get rigidbody component and throw error if it is missing

        //Set up velocity reference:
        if (velocityReference != null) //Velocity reference object has been provided
        {
            if (!velocityReference.TryGetComponent(out FollowBodyUpdater updater)) { updater = velocityReference.gameObject.AddComponent<FollowBodyUpdater>(); } //Instantiate updater on velocity reference if not present already
            updater.fb.Add(this); //Add this followBody to update list
        }
    }

    //RUNTIME METHODS:
    private void FixedUpdate()
    {
        PerformUpdate(); //Update position during physics step
    }
    private void OnPreRender()
    {
        PerformUpdate(); //Update position right before render to ensure movement is smooth
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Updates position of rigidbody follower to match position of target.
    /// </summary>
    public void PerformUpdate()
    {
        //Initialize:
        Vector3 targetPos = target.position; //Store value for actual target position

        //Velocity compensation:
        if (velocityReference != null && velocityCompensation > 0) //Velocity reference has been given and compensation is non-zero
        {
            targetPos += velocityReference.velocity * velocityCompensation; //Adjust target position based on velocity compensation to account for lag
        }

        //Apply to rigidbody:
        rb.MovePosition(targetPos);       //Apply position
        rb.MoveRotation(target.rotation); //Apply rotation
    }
}
