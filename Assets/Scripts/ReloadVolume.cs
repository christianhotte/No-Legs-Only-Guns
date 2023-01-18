using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReloadVolume : MonoBehaviour
{
    //Settings:
    [SerializeField, Tooltip("Ammo object to load into weapons")] private GameObject ammunition;

    //RUNTIME METHODS:
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Chamber")) //Intersected volume is the chamber of a weapon
        {
            Gun weapon = other.GetComponentInParent<Gun>(); //Get weapon component from chamber

            if (weapon.breachOpen &&                              //Weapon breach is open
                weapon.loadedAmmo.Count < weapon.chambers.Length) //Weapon has room for more ammo
            {
                weapon.FullyLoadAmmo(ammunition.GetComponent<Ammo>()); //Fully load weapon
            }
        }
    }
}
