using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables the player to swing their weapon to curve/boost shots slightly.
/// </summary>
public class WepMod_SwingShot : WeaponMod
{
    //Settings:
    [Header("Curve Settings:")]
    [SerializeField, Tooltip("Determines how swing velocity is converted into constant force turning projectiles")]                private float forceMultiplier;
    [SerializeField, Tooltip("Curve describing how spin force acts on projectiles over projectile lifetime")]                      private AnimationCurve forceCurve;
    [SerializeField, Tooltip("Minimum speed at which weapon must be swung to add force to projectile (should filter out recoil)")] private float minTriggerVelocity;
    [SerializeField, Tooltip("Maximum amount of force which the player is able to exert on spun projectiles")]                     private float maxSpinForce;
    [Header("Projectile Effects:")]
    [SerializeField, Tooltip("Range added to projectiles depending on swing strength")]  private float maxRangeBoost;
    [SerializeField, Tooltip("Damage added to projectiles depending on swing strength")] private float maxDamageBoost;

    //RUNTIME METHODS:
    private void Start()
    {
        //Event subscriptions:
        weapon.onProjectilesSpawned += OnProjectilesSpawned; //Subscribe to OnProjectilesSpawned event
    }
    private void OnDestroy()
    {
        //Event unsubscriptions:
        weapon.onProjectilesSpawned -= OnProjectilesSpawned; //Unsubscribe from OnProjectilesSpawned event
    }
    private void OnProjectilesSpawned(Projectile[] projectiles)
    {
        //Initialize:
        float swingPower = Mathf.Clamp(Mathf.InverseLerp(minTriggerVelocity, maxSpinForce / forceMultiplier, GetCurrentVelocity().magnitude), 0, 1); //Get linear parameter representing force of swing
        if (swingPower <= 0) return;                                                                                                                 //Do nothing if swing velocity is not high enough
        Vector3 spinForce = Vector3.ClampMagnitude(GetCurrentVelocity() * forceMultiplier, maxSpinForce);                                            //Get force at which projectiles will be spun

        //Set up projectiles:
        float addRange = maxRangeBoost * swingPower;                          //Get value for additional range to give each projectile
        float addDamage = (maxDamageBoost * swingPower) / projectiles.Length; //Get value for additional damage to give each projectile
        foreach (Projectile projectile in projectiles) //Iterate through each fired projectile
        {
            //Add spin:
            ProjMod_ForceOverLifetime mod = projectile.gameObject.AddComponent<ProjMod_ForceOverLifetime>(); //Add ForceOverLifetime mod to projectile
            mod.force = spinForce;                                                                           //Set force value
            mod.forceCurve = forceCurve;                                                                     //Transfer forceCurve to projectile mod script

            //Add other attributes:
            projectile.maxDistance += addRange; //Add range to projectile
            projectile.hitDamage += addDamage;  //Add damage to projectile
        }
        //print("Velocity = " + GetCurrentVelocity().magnitude + " | Force = " + GetCurrentVelocity().magnitude * forceMultiplier);
    }
}
