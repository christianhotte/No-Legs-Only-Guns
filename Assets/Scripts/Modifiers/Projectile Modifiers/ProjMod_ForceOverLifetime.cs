using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies a constant force to projectile, with strength optionally determined by lifetime.
/// </summary>
public class ProjMod_ForceOverLifetime : ProjectileMod
{
    //Settings:
    [Tooltip("Direction and strength of base acceleration")]                                                 public Vector3 force;
    [Tooltip("Determines gravityForce multiplier (Value) over projectile lifetime (Time)")]                  public AnimationCurve forceCurve;
    [Tooltip("Re-checks lifetime every update to more accurately model curve (more performance expensive)")] public bool useAdaptiveLifetime;

    //Runtime Vars:
    private float lifetime = 0; //Projectile lifetime (not used to kill projectile)

    private void Start()
    {
        //Setup:
        if (!useAdaptiveLifetime) lifetime = GetPredictedLifetime(); //Get projectile lifetime (only necessary if adaptive lifetime is off)
    }
    private void FixedUpdate()
    {
        //Adjust velocity:
        float timeInterpolant;                                             //Initialize time interpolant variable
        if (useAdaptiveLifetime) timeInterpolant = TimeInterpolant();      //If using adaptive lifetime system, get up-to-date lifetime interpolant
        else timeInterpolant = projectile.timeAlive / lifetime;            //Otherwise, use lifetime interpolant based on initial reading
        Vector3 forceAccel = forceCurve.Evaluate(timeInterpolant) * force; //Evaluate strength of force based on lifetime
        projectile.velocity += forceAccel * Time.fixedDeltaTime;           //Apply acceleration to projectile
    }
}
