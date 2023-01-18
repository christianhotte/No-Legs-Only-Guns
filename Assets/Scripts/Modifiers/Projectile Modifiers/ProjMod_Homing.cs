using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for various target-following properties which can be added to projectile.  Contains generic functionality, but does not function without other scripts.
/// </summary>
public class ProjMod_Homing : ProjectileMod
{
    //Objects & Components:
    internal Transform currentTarget; //Position for projectile to seek

    //Settings:
    

    //RUNTIME METHODS:
    private void FixedUpdate()
    {
        //Adjust velocity:
        //Vector3 targetVelocity = (currentTarget.position - transform.position).
    }
}
