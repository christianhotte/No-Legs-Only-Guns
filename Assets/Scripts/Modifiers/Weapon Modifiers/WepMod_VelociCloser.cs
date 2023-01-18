using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables player to close this weapon's breach by swinging it vertically (only works on BreakActionGun).
/// </summary>
public class WepMod_VelociCloser : WeaponMod
{
    //Objects & Components:
    private BreakActionGun breakWeapon; //The inherited weapon script for the weapon this mod attaches to (only works with BreakActionGun)
    private Rigidbody pivotBody;        //Rigidbody which does physics for pivoting barrel assembly

    //Settings:
    [Header("Settings:")]
    [SerializeField, Min(0), Tooltip("Determines strength of velocity closing effect.")] private float forceMultiplier;
    [SerializeField, Min(0), Tooltip("Display debug information in console.")]           private bool doDebugLogs;

    //RUNTIME METHODS:
    public override void Awake()
    {
        //Initialization:
        base.Awake(); //Call base method

        //Get objects & components:
        if (!weapon.TryGetComponent(out breakWeapon)) { Debug.LogError("VelociCloser mod can only go on a Break Action Weapon."); Destroy(this); } //Make sure mod is attached to a break action weapon
    }
    private void Start()
    {
        //Get objects & components (late):
        pivotBody = breakWeapon.breakJoint.GetComponent<Rigidbody>(); //Get rigidbody from weapon pivot
    }
    private void Update()
    {
        if (weapon.breachOpen) //Only apply force while weapon breach is open
        {
            //Get applied force:
            Vector3 currentBarrelVel = GetCurrentVelocity();                                                                //Get current velocity of weapon barrel
            currentBarrelVel = Vector3.Project(currentBarrelVel, pivotBody.transform.up);                                   //Get barrel velocity exclusively along vertical axis of barrel
            Vector3 barrelForce = pivotBody.transform.up * currentBarrelVel.magnitude * forceMultiplier;                    //Use barrel velocity and force multiplier to get force to apply to barrel
            pivotBody.AddForceAtPosition(barrelForce, weapon.barrels[weapon.currentBarrelIndex].position, ForceMode.Force); //Apply force to rigidbody at end of barrel

            if (doDebugLogs) { Debug.Log("VelociCloser force: " + currentBarrelVel.magnitude * forceMultiplier); } //Optionally log applied force
        }
    }
}
