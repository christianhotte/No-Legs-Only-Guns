using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ammo : MonoBehaviour
{
    //Objects & Components:
    private AudioSource audioSource; //The local audiosource component on this object
    internal Rigidbody rb;           //This object's rigidbody component
    internal Collider coll;          //This object's collider component
    [SerializeField, Tooltip("Projectile prefab spawned when this ammunition is fired")] private GameObject projectile;

    //Settings:
    [Header("Projectile Travel:")]
    [SerializeField, Tooltip("Number of projectiles fired")]                                                              private int projectileQuantity = 1;
    [SerializeField, Tooltip("Maximum angle of projectile spread")]                                                       private float maxSpreadAngle;
    [SerializeField, Tooltip("Initial speed of projectiles when fired (in units per second)")]                            private float projectileSpeed;
    [SerializeField, Tooltip("Maximum distance (in units) projectiles will travel before despawning (zero if infinite)")] private float range = 0;
    [SerializeField, Tooltip("Maximum time (in seconds) projectiles will last before despawning (zero if infinite)")]     private float lifetime = 0;
    [Header("Projectile Hit:")]
    [SerializeField, Tooltip("Total amount of damage dealt between all projectiles in a single shot")]          private float totalHitDamage;
    [SerializeField, Tooltip("Total amount of enemy knockback dealt between all projectiles in a single shot")] private float totalHitKick;
    [Header("Shell Physics:")]
    [SerializeField, Tooltip("LOCOMOTION: How much force this ammo applies to player rigidbody when fired")]        private float kickStrength;
    [SerializeField, Tooltip("VISUALS: Can be used to increase or decrease intensity of recoil effect when fired")] private float recoilMultiplier = 1;
    [SerializeField, Tooltip("How many Gs are exerted on this shell after ejection (relative to normal gravity")]   private float shellGravity = 1;
    [SerializeField, Tooltip("How long (after ejection) before shell despawns")]                                    private float ejectedLifetime = 5;
    [SerializeField, Tooltip("Max angular velocity of spent casing")]                                               private float maxAngVel = 7;
    [Header("Sound Effects:")]
    [Tooltip("Sound made when this ammo is loaded into a weapon")]                                                       public AudioClip loadSound;
    [SerializeField, Tooltip("Sound made when this ammo is fired (chosen randomly from list each time weapon is fired")] private AudioClip[] fireSounds;

    //Runtime Vars:
    internal bool spent = false;         //Indicates that ammo has already been fired
    private float timeSinceEjected = -1; //How much time (in seconds) has passed since shell was ejected (negative if shell is not ejected)

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        if (!TryGetComponent(out rb)) Debug.LogWarning(name + " is missing rigidbody component");   //Get rigidbody component and post warning if it is missing
        if (!TryGetComponent(out coll)) Debug.LogWarning(name + " is missing collider component");  //Get collider component and post warning if it is missing
        if (!TryGetComponent(out audioSource)) Debug.LogWarning(name + " is missing audio source"); //Get audio source component and post warning if it is missing
    }
    private void FixedUpdate()
    {
        if (timeSinceEjected >= 0) //Updates for shell after ejection
        {
            rb.AddForce(Physics.gravity * shellGravity, ForceMode.Acceleration); //Apply artificial gravity
            timeSinceEjected += Time.fixedDeltaTime;                             //Increment time tracker
            if (timeSinceEjected >= ejectedLifetime) Destroy(gameObject);        //Destroy shell if lifetime has been exceeded
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Fires projectiles based on ammunition settings.
    /// </summary>
    /// <param name="weapon">The gun that is firing this piece of ammo.</param>
    /// <param name="barrelEnd">The position this ammo is firing from.</param>
    /// <returns>Array of projectiles spawned.</returns>
    public Projectile[] Shoot(Gun weapon, Transform barrelEnd)
    {
        //Fire projectiles:
        Vector3 origBarrelEndEulers = barrelEnd.localEulerAngles; //Save local rotation of current barrel end
        List<Projectile> projectiles = new List<Projectile>();    //Initialize list to store spawned projectiles
        for (int i = 0; i < projectileQuantity; i++) //Iterate for number of projectiles
        {
            //Initialize:
            Projectile newProjectile = Instantiate(projectile).GetComponent<Projectile>(); //Instantiate new projectile
            projectiles.Add(newProjectile);                                                //Add new projectile to running list

            //Get projectile values:
            Vector3 exitAngles = Random.insideUnitCircle * maxSpreadAngle;                 //Get random value for shot spread (VERY TEMPORARY)
            barrelEnd.localEulerAngles = new Vector3(origBarrelEndEulers.x + exitAngles.x, //Apply temporary rotation to barrel end x axis
                                                     origBarrelEndEulers.y + exitAngles.y, //Apply temporary rotation to barrel end y axis
                                                     origBarrelEndEulers.z);               //Do nothing with z axis
            Vector3 projVel = barrelEnd.forward * projectileSpeed;                         //Get exit velocity based on barrel direction and set speed

            //Position projectile:
            newProjectile.transform.position = barrelEnd.position;               //Move projectile to end of barrel being fired
            newProjectile.transform.rotation = Quaternion.LookRotation(projVel); //Rotate projectile to align with starting velocity

            //Set up projectile:
            newProjectile.velocity = projVel;                              //Set projectile initial velocity
            newProjectile.lifetime = lifetime;                             //Set projectile's maximum time alive
            newProjectile.maxDistance = range;                             //Set projectile's maximum travel distance
            newProjectile.hitDamage = totalHitDamage / projectileQuantity; //Set projectile damage
            newProjectile.hitKick = totalHitKick / projectileQuantity;     //Set projectile kickback
            newProjectile.currentSpeed = projectileSpeed;                  //Fill in projectile speed (saves a magnitude check)
        }
        barrelEnd.localEulerAngles = origBarrelEndEulers; //Return current barrel end to its base rotation

        //Knockback:
        if (weapon.rightHand && weapon.player.rightWingValue < 0.5f || //Player is firing right weapon and does not currently have right wing extended
            !weapon.rightHand && weapon.player.leftWingValue < 0.5f)   //Player is firing left weapon and does not currently have left wing extended
        {
            weapon.player.ApplyKnockback(barrelEnd.forward * -kickStrength); //Move player using knockback force
        }
        weapon.StartCoroutine(weapon.DoRecoil(recoilMultiplier)); //Start recoil coroutine

        //Effects:
        if (fireSounds.Length > 0) audioSource.PlayOneShot(fireSounds[Random.Range(0, fireSounds.Length)]); //Play random firing sound (if possible)

        //Cleanup:
        spent = true;                 //Indicate that ammo has been fired
        return projectiles.ToArray(); //Return list of spawned projectiles as array
    }
    /// <summary>
    /// Enables physics and applies given force in given direction to ammo object.
    /// </summary>
    /// <param name="force">Direction and intensity of ejection force.</param>
    /// <param name="extractorPos">Position of extractor, used to put torque on ejected shell.</param>
    public void Eject(Vector3 force, Vector3 extractorPos)
    {
        //Set shell physics:
        rb.maxAngularVelocity = maxAngVel;                             //Internally set max angular velocity
        coll.enabled = true;                                           //Enable collider
        rb.isKinematic = false;                                        //Enable physics
        rb.interpolation = RigidbodyInterpolation.Interpolate;         //Do interpolation (for smoother ejection at speed)
        rb.AddForceAtPosition(force, extractorPos, ForceMode.Impulse); //Apply ejection force to shell at extractor position

        //Cleanup:
        transform.parent = null; //Unparent from chamber
        timeSinceEjected = 0;    //Indicate that shell has been ejected
    }
}
