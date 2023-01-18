using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for scripts which are attached to projectiles to modify their in-flight characteristics.
/// </summary>
public class ProjectileMod : MonoBehaviour
{
    //Objects & Components:
    private protected Projectile projectile; //The projectile this mod is attached to

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        if (!TryGetComponent(out projectile)) { Debug.LogError("Tried to attach ProjectileMod to non-projectile"); Destroy(this); } //Get reference to projectile script
    }

    //UTILITY METHODS:
    /// <summary>
    /// Generates predicted projectile lifetime based on current velocity and range.  If projectile has pre-set lifetime, returns that instead.
    /// </summary>
    private protected float GetPredictedLifetime()
    {
        //Initial checks:
        if (projectile.lifetime > 0) return projectile.lifetime; //Return actual projectile lifetime if specified
        if (projectile.maxDistance <= 0) return Mathf.Infinity;  //Return infinite value if projectile has no given max range (lifetime cannot be accurately predicted)

        //Calculate lifetime:
        float predictedLifetime = projectile.timeAlive;                            //Initialize predicted lifetime at known amount of time projectile has been alive
        float remainingDistance = projectile.maxDistance - projectile.totalTravel; //Get distance of travel projectile has yet to cover
        predictedLifetime += remainingDistance / projectile.currentSpeed;          //Calculate time projectile should take to travel remaining distance and add to known lifetime
        return predictedLifetime;                                                  //Return calculated value
    }
    private protected float TimeInterpolant() { return projectile.timeAlive / GetPredictedLifetime(); }
    /// <summary>
    /// Generates predicted projectile range based on current velocity and lifetime.  If projectile has pre-set range, returns that instead.
    /// </summary>
    private protected float GetPredictedRange()
    {
        //Initial checks:
        if (projectile.maxDistance > 0) return projectile.maxDistance; //Return actual projectile range if specified
        if (projectile.lifetime <= 0) return Mathf.Infinity;           //Return infinite value if projectile has no given lifetime (range cannot be accurately predicted)

        //Calculate range:
        float predictedRange = projectile.totalTravel;                    //Initialize predicted range at known amount of distance projectile has already covered
        float remainingTime = projectile.lifetime - projectile.timeAlive; //Get amount of time projectile has left to live
        predictedRange += remainingTime * projectile.currentSpeed;        //Calculate distance projectile should cover within remaining lifetime and add to known range
        return predictedRange;                                            //Return calculated value
    }
    private protected float RangeInterpolant() { return projectile.totalTravel / GetPredictedRange(); }
}
