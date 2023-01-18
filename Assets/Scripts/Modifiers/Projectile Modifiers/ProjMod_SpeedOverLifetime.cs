using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modifies velocity of projectile based on given curve.
/// </summary>
public class ProjMod_SpeedOverLifetime : ProjectileMod
{
    //Settings:
    [Tooltip("Determines velocity multiplier (Value, based on initial velocity) over projectile lifetime (Time)")] public AnimationCurve speedCurve;
    [Tooltip("Re-checks lifetime every update to more accurately model curve (more performance expensive)")]       public bool useAdaptiveLifetime;

    //Runtime Vars:
    private float initialSpeed; //Starting speed of projectile
    private float lifetime = 0; //Projectile lifetime (not used to kill projectile)

    //RUNTIME METHODS:
    private void Start()
    {
        //Setup:
        initialSpeed = projectile.currentSpeed;                      //Get base projectile velocity
        if (!useAdaptiveLifetime) lifetime = GetPredictedLifetime(); //Get projectile lifetime (only necessary if adaptive lifetime is off)
    }
    private void FixedUpdate()
    {
        //Adjust speed:
        float timeInterpolant;                                                //Initialize time interpolant variable
        if (useAdaptiveLifetime) timeInterpolant = TimeInterpolant();         //If using adaptive lifetime system, get up-to-date lifetime interpolant
        else timeInterpolant = projectile.timeAlive / lifetime;               //Otherwise, use lifetime interpolant based on initial reading
        float newSpeed = speedCurve.Evaluate(timeInterpolant) * initialSpeed; //Evaluate speedCurve using lifetime interpolant and apply multiplier to initial projectile speed
        projectile.velocity = projectile.velocity.normalized * newSpeed;      //Set new projectile speed
    }
}
