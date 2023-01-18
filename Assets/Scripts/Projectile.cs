using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    //Objects & Components:
    private Renderer[] renderers; //Array of all components rendering this projectile

    //Settings:
    [Header("Settings:")]
    [Tooltip("Radius of projectile collision area (uses linecast if 0)")]                                                       public float radius = 0;
    [SerializeField, Tooltip("Adds separation distance between barrel and projectile spawnpoint, useful for long projectiles")] private float barrelGap;
    [SerializeField, Tooltip("Layers which projectile will not collide with")]                                                  private LayerMask ignoreLayers;
    [Header("Visuals:")]
    [SerializeField, Tooltip("Object spawned when projectile hits something")] private GameObject hitEffect;

    //Internal Settings:
    internal Vector3 velocity;      //Direction and speed of projectile motion
    internal float lifetime = 0;    //Maximum number of seconds this projectile can exist before despawning (0 or below is infinite)
    internal float maxDistance = 0; //Maximum distance this projectile can travel before despawning (0 or below is infinite)
    internal float hitDamage = 0;   //Amount of damage this projectile deals when it hits a damageable entity
    internal float hitKick = 0;     //Amount of force this projectile applies when it hits an applicable entity

    //Runtime Vars:
    internal float timeAlive;     //Time (in seconds) since this projectile was spawned
    internal float totalTravel;   //Distance (in units) this projectile has traveled
    internal float currentSpeed;  //Speed (in units per second) projectile is currently traveling at
    private LayerMask sphereMask; //Layer mask used when SphereCasting (ignores level)

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialize runtime vars:
        sphereMask = ignoreLayers; sphereMask |= (1 << LayerMask.NameToLayer("Level")); //Set up sphere mask to ignore level (allowing projectiles to slide around corners)
    }
    private void Start()
    {
        //Render setup:
        List<Renderer> renderComponents = new List<Renderer>();         //Create list to store found renderer components
        renderComponents.AddRange(GetComponents<Renderer>());           //Populate list with renderers on projectile object
        renderComponents.AddRange(GetComponentsInChildren<Renderer>()); //Populate list with renderers on objects childed to projectile
        renderers = renderComponents.ToArray();                         //Store list as array

        //Barrel gap setup:
        if (barrelGap > 0) //Projectile is being initially placed further from barrel
        {
            //Perform a mini position update:
            Vector3 targetPos = transform.position + (velocity.normalized * barrelGap);                                                 //Get target starting position
            if (Physics.Linecast(transform.position, targetPos, out RaycastHit hitInfo, ~ignoreLayers)) { HitObject(hitInfo); return; } //Check for collisions (just in case)
            transform.position = targetPos;                                                                                             //Move projectile to starting position
            totalTravel += barrelGap;                                                                                                   //Include distance in total distanced traveled (to keep it accurate)
        }
    }
    private void FixedUpdate()
    {
        //Initialize variables:
        Vector3 targetPos = transform.position + (velocity * Time.fixedDeltaTime); //Get target projectile position
        float travelDistance = Vector3.Distance(transform.position, targetPos);    //Get distance this projectile is moving this update
        currentSpeed = travelDistance / Time.fixedDeltaTime;                       //Get speed at which this projectile is currently traveling (in case modifiers need to know this)
        RaycastHit hitInfo = new RaycastHit();                                     //Initialize container to store hit info

        //Check range:
        totalTravel += travelDistance; //Add motion to total distance traveled (NOTE: may briefly end up being greater than actual distance traveled)
        if (maxDistance > 0 && totalTravel >= maxDistance) //New distance traveled exceeds range
        {
            float backtrackAmt = totalTravel - maxDistance;                               //Determine length by which to backtrack projectile
            travelDistance -= backtrackAmt;                                               //Adjust travel distance to account for limited range
            targetPos = Vector3.MoveTowards(targetPos, transform.position, backtrackAmt); //Backtrack target position by given amount (ensuring projectile travels exact range)
        }

        //Check for hit:
        if (Physics.Linecast(transform.position, targetPos, out hitInfo, ~ignoreLayers)) //Do a simple linecast first, only ignoring designated layers
            { HitObject(hitInfo); return; } //Trigger hit procedure if projectile strikes object, then skip remainder of update
        
        //NOTE: Potentially add something here which only performs this cast when the previous linecast hits a large "general area" collider
        if (radius > 0 && Physics.SphereCast(transform.position, radius, velocity, out hitInfo, travelDistance - radius, ~sphereMask)) //If projectile has diameter, do a spherecast for diameter (ignoring level)
            { HitObject(hitInfo); return; } //Trigger hit procedure if projectile strikes object, then skip remainder of update

        //Perform move:
        transform.position = targetPos;                               //Move projectile to target position
        transform.rotation = Quaternion.LookRotation(velocity);       //Rotate projectile to align with current velocity
        if (maxDistance > 0 && totalTravel >= maxDistance) BurnOut(); //Delayed projectile destruction for end of range (ensures projectile dies after being moved)

        //Check lifetime:
        timeAlive += Time.fixedDeltaTime;                    //Increment time alive
        if (lifetime > 0 && timeAlive > lifetime) BurnOut(); //Destroy projectile if it has a limited lifetime which has just been exceeded
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Called when projectile collides with an object.
    /// </summary>
    private void HitObject(RaycastHit hitInfo)
    {
        //Check for reaction:
        if (hitInfo.collider.TryGetComponent(out IShootable shotScript)) shotScript.IsHit(this); //Trigger reaction if shot object is compatible

        //Spawn hit effect:
        GameObject effect = Instantiate(hitEffect);    //Instantiate hit effect object
        effect.transform.position = hitInfo.point;     //Position hit effect at target point
        effect.transform.eulerAngles = hitInfo.normal; //Rotate hit effect to align with hit surface

        //Cleanup:
        Destroy(gameObject); //Destroy projectile object
    }
    /// <summary>
    /// Called when projectile either runs out of range or time without hitting anything.
    /// </summary>
    private void BurnOut()
    {
        Destroy(gameObject); //TEMP
    }
    /// <summary>
    /// Enables or disables projectile renderers.
    /// </summary>
    public void ToggleVisibility(bool enabled) { foreach (Renderer renderer in renderers) renderer.enabled = enabled; }
}
