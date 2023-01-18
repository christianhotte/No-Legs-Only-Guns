using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for scripts which are attached to weapons to modify their firing characteristics.
/// </summary>
public class WeaponMod : MonoBehaviour
{
    //Objects & Components:
    /// <summary>
    /// The weapon this script is attached to and modifying.
    /// </summary>
    private protected Gun weapon;

    //Settings:
    [Header("General Modifier Settings:")]
    [SerializeField, Tooltip("Use this to smooth out small fluctuations in velocity calculations if weapon mod involves velocity.  If weapon mod does not involve velocity, leave this as 0")] private int weaponVelocityMem = 0;

    //Runtime vars:
    private List<Vector3> prevPositions = new List<Vector3>(); //List containing memory of position of current barrel end on previous frames (from newest to oldest)

    //RUNTIME METHODS:
    public virtual void Awake()
    {
        //Get objects & components:
        if (!TryGetComponent(out weapon)) { Debug.LogError("Tried to attach WeaponMod to non-weapon"); Destroy(this); } //Get reference to weapon script
    }
    private void FixedUpdate()
    {
        //Update velocity memory:
        if (weaponVelocityMem > 0) //Mod is using velocity memory
        {
            prevPositions.Insert(0, GetCurrentPosition());                                                //Add current position to memory list
            if (prevPositions.Count > weaponVelocityMem) prevPositions.RemoveAt(prevPositions.Count - 1); //Trim memory list once it exceeds given memory length
        }
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns current linear velocity of this weapon, specifically the end of its barrel.
    /// </summary>
    /// <returns></returns>
    private protected Vector3 GetCurrentVelocity()
    {
        //Validity checks:
        if (weaponVelocityMem == 0) return Vector3.zero;   //Return zero if weapon is not checking for velocity
        if (prevPositions.Count == 0) return Vector3.zero; //Return zero if weapon does not currently have any previous positions stored

        //Cleanup:
        Vector3 netVelocity = GetCurrentPosition() - prevPositions[prevPositions.Count - 1]; //Return total velocity change between current position and
        return netVelocity / (Time.fixedDeltaTime * prevPositions.Count);                    //Apply fixed timescale to get real average velocity over time
    }
    /// <summary>
    /// Returns current position of this weapon's barrelEnd, relative to position of player body.
    /// </summary>
    /// <returns></returns>
    private protected Vector3 GetCurrentPosition()
    {
        return weapon.player.XROrigin.InverseTransformPoint(weapon.barrels[weapon.currentBarrelIndex].transform.position); //Use InverseTransformPoint to place position of active weapon barrel in player's relative space
    }
}
