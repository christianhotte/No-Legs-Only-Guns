using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EjectionTrigger : MonoBehaviour
{
    //DESCRIPTION: Triggers shell ejection in break action weapons.

    //Objects & Components:
    private BreakActionGun weapon; //The weaponController in this script's parent chain

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        weapon = GetComponentInParent<BreakActionGun>();                                                            //Try to get BreakActionGun script in parent
        if (weapon == null) { Debug.LogError("BarrelLocker could not find parent weapon script."); Destroy(this); } //Destroy script if it cannot find weapon controller
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Chamber")) //Chamber has left trigger area
        {
            //Initialize:
            BreakActionGun chamberWeapon = other.GetComponentInParent<BreakActionGun>(); //Try to get BreakActionGun script from chamber
            if (chamberWeapon == null) return;   //Ignore if chamber is not from break-action weapon
            if (chamberWeapon != weapon) return; //Ignore if chamber is from different weapon

            //Perform function:
            weapon.EjectAmmo(); //Eject ammo from weapon
        }
    }
}
