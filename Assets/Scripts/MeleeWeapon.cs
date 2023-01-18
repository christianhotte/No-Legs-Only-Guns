using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeWeapon : Gun
{
    //Objects & Components:
    private Gun primaryWeapon;            //Primary gun being held in the same hand as this melee weapon
    private Transform deployedDimensions; //Position and scale of deployed blade

    //Settings:
    [Header("Melee Weapon Components:")]
    [SerializeField, Tooltip("The pointy part")]                      private Transform blade;
    [SerializeField, Tooltip("Position and scale of stowed blade")]   private Transform stowedDimensions;
    [Header("Melee Weapon Input:")]
    [SerializeField, Tooltip("Forward linear velocity at which weapon will be activate")]                                   private float activationSpeed;
    [SerializeField, Min(0.01f), Tooltip("Time which must be spent with system at activationSpeed for weapon to activate")] private float activationTime;
    [Header("Melee Weapon Functionality:")]
    [SerializeField, Min(0), Tooltip("Force applied to player rigidbody when blade is deployed")] private float deployLaunchStrength;
    [Header("Melee Weapon FX:")]
    [SerializeField, Min(0), Tooltip("Time taken for blade to fully deploy/sheath")]               private float deployTime;
    [SerializeField, Min(0), Tooltip("Curve describing linear motion during blade deployment")]    private AnimationCurve deployMotionCurve;
    [SerializeField, Min(0), Tooltip("Curve describing scale transition during blade deployment")] private AnimationCurve deployScaleCurve;
    [SerializeField, Min(0), Tooltip("Curve describing linear motion during blade stowage")]       private AnimationCurve sheathMotionCurve;
    [SerializeField, Min(0), Tooltip("Curve describing scale transition during blade stowage")]    private AnimationCurve sheathScaleCurve;
    [Space()]
    [SerializeField, Tooltip("Sound blade makes when deployed")] private AudioClip deploySound;
    [SerializeField, Tooltip("Sound blade makes while active")]  private AudioClip activeSound;
    [SerializeField, Tooltip("Sound blade makes when sheathed")] private AudioClip sheathSound;
    [Space()]
    [SerializeField, Tooltip("Intensity (X, 0-1) and duration (Y) of haptic response when blade is deployed")] private Vector2 deployHaptics;
    //[SerializeField, Tooltip("Intensity (X, 0-1) constant haptic rumble while blade is deployed")]             private float activeHaptics;
    [SerializeField, Tooltip("Intensity (X, 0-1) and duration (Y) of haptic response when blade is sheathed")] private Vector2 sheathHaptics;

    //Runtime Vars:
    private bool deployed = false;  //Whether or not blade is currently deployed
    private float timeDeployed = 0; //How long weapon has been deployed for
    private float timeAtSpeed = 0;  //Time (in seconds) hand has spent at activation velocity
    private Vector3 prevHandPos;    //Previous position of hand (relative to player origin)

    //EVENTS & COROUTINES:
    public IEnumerator DeploySequence()
    {
        //Initialization:
        if (deploySound != null) audioSource.PlayOneShot(deploySound); //Play deployment sound if possible

        //Physically Deploy blade:
        float timePassed = 0; //Initialize value to track passage of time throughout deployment
        for (; timePassed < deployTime; timePassed += Time.fixedDeltaTime) //Iterate for full deployment time
        {
            float timeValue = timePassed / deployTime;                                                                                                       //Get value representing percentage of time which has passed
            Vector3 newPos = Vector3.LerpUnclamped(stowedDimensions.localPosition, deployedDimensions.localPosition, deployMotionCurve.Evaluate(timeValue)); //Get new position by evaluating curve and lerping between positions based on time
            Vector3 newScl = Vector3.LerpUnclamped(stowedDimensions.localScale, deployedDimensions.localScale, deployScaleCurve.Evaluate(timeValue));        //Get new scale by evaluating curve and lerping between scales based on time
            blade.localPosition = newPos; blade.localScale = newScl;                                                                                         //Apply new position and scale
            yield return new WaitForFixedUpdate();                                                                                                           //Wait for next fixed update
        }
        blade.localPosition = deployedDimensions.localPosition; //Move blade to final position
        blade.localScale = deployedDimensions.localScale;       //Adjust blade to final scale

        //Begin active sound:
        if (deploySound != null && activeSound != null) //Make sure system sounds are set up before trying to do sequence
        {
            if (timePassed < deploySound.length) yield return new WaitForSeconds(deploySound.length - timePassed); //Wait until deploySound has finished
            audioSource.loop = true;                                                                               //Make sure audiosource is set to loop
            audioSource.clip = activeSound;                                                                        //Set audiosource clip to active sound
            audioSource.Play();                                                                                    //Play active sound
        }
    }
    public IEnumerator SheathSequence()
    {
        //Initialization:
        audioSource.loop = false;                                      //Make audiosource stop looping
        if (sheathSound != null) audioSource.PlayOneShot(sheathSound); //Play sheath sound if possible

        //Main sequence loop:
        for (float timePassed = 0; timePassed < deployTime; timePassed += Time.fixedDeltaTime) //Iterate for full deployment time
        {
            float timeValue = timePassed / deployTime;                                                                                              //Get value representing percentage of time which has passed
            Vector3 newPos = Vector3.Lerp(deployedDimensions.localPosition, stowedDimensions.localPosition, sheathMotionCurve.Evaluate(timeValue)); //Get new position by evaluating curve and lerping between positions based on time
            Vector3 newScl = Vector3.Lerp(deployedDimensions.localScale, stowedDimensions.localScale, sheathScaleCurve.Evaluate(timeValue));        //Get new scale by evaluating curve and lerping between scales based on time
            blade.localPosition = newPos; blade.localScale = newScl;                                                                                //Apply new position and scale
            yield return new WaitForFixedUpdate();                                                                                                  //Wait for next fixed update
        }

        //Cleanup:
        blade.localPosition = stowedDimensions.localPosition; //Move blade to final position
        blade.localScale = stowedDimensions.localScale;       //Adjust blade to final scale
        primaryWeapon.inputDisabled = false;                  //Enable input on primary weapon once blade has been fully sheathed
    }

    //RUNTIME METHODS:
    public override void Awake()
    {
        //Initialze:
        string bladeName = blade.name;           //Store name of blade in case it is destroyed
        string stowName = stowedDimensions.name; //Store name of stowDimension transform in case it is destroyed
        base.Awake();                            //Call base awake method

        //Set up blade:
        if (rightHand) //Alt model set is being used
        {
            blade = models.Find(bladeName);           //Replace missing blade transform with one from alt model
            stowedDimensions = models.Find(stowName); //Replace missing stowed dimension transform with one from alt model
        }
        deployedDimensions = new GameObject("DeployedDimensions").transform; //Generate a new transform representing dimensions of deployed blade
        deployedDimensions.parent = models;                                  //Child dimensions to model container object (same as stowed dimensions)
        deployedDimensions.localPosition = blade.localPosition;              //Record deployed position of blade
        deployedDimensions.localScale = blade.localScale;                    //Record deployed scale of blade
        blade.localPosition = stowedDimensions.localPosition;                //Move blade to stowed position
        blade.localScale = stowedDimensions.localScale;                      //Scale blade to stowed size
    }
    public override void Start()
    {
        //Initialize:
        base.Start(); //Call base start method

        //Get primary weapon:
        Gun[] playerWeapons = player.GetComponentsInChildren<Gun>(); //Get an array of all weapons currently held by player
        foreach (Gun weapon in playerWeapons) //Iterate through all weapons on player
        {
            if (weapon == this) continue;                                         //Skip self
            if (weapon.rightHand == rightHand) { primaryWeapon = weapon; break; } //Get weapon which matches this melee weapon's side
        }
        if (primaryWeapon == null) Debug.LogError(name + " could not find a corresponding primary weapon on player!"); //Report error if no primary weapon was found
    }
    public override void Update()
    {
        base.Update(); //Call base update method

        //Check for activation:
        Vector3 currentHandPos = player.XROrigin.InverseTransformPoint(attachedHand.position); //Get current position of hand controlling this weapon
        Vector3 handMotion = currentHandPos - prevHandPos;                                     //Get vector representing raw directional motion of hand
        float forwardAngle = Vector3.Angle(handMotion.normalized, transform.forward);          //Get angle between relative forward direction of hand and hand motion (linearity of punch)
        if (!deployed && forwardAngle < 90 || //Blade is not deployed and hand is moving forward, OR
            deployed && forwardAngle > 90)    //Blade is deployed and hand is moving backward
        {
            handMotion = Vector3.Project(currentHandPos - prevHandPos, transform.forward); //Filter hand motion to just represent motion along forward axis of hand
            float punchSpeed = handMotion.magnitude / Time.deltaTime;                      //Get speed of forward hand motion
            if (punchSpeed >= activationSpeed) //Punch/withdraw speed is sufficient to perform activation
            {
                timeAtSpeed += Time.deltaTime; //Add to time at speed
                if (timeAtSpeed >= activationTime) //Weapon has spent enough time at speed to activate
                {
                    if (!deployed) Deploy(); //Deploy if hand has been punching for long enough
                    else //Hand has been withdrawing for long enough, trigger special non-explosive sheath action
                    {
                        Sheath();                      //Sheath if hand has been withdrawing for long enough
                        primaryWeapon.FullyLoadAmmo(); //Reload primary weapon (as a feature of this special sheath action
                    }
                }
            }
            else timeAtSpeed = 0; //Punch speed is insufficient, reset time at speed tracker
        }
        else timeAtSpeed = 0; //Movement is not a punch, reset time at speed tracker
        prevHandPos = currentHandPos; //Store hand position for next update

        //Update deployed haptics:
        if (deployed) timeDeployed += Time.deltaTime; //Update deploy time
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Triggers blade deployment sequence.
    /// </summary>
    private void Deploy()
    {
        //Initial effects:
        player.ApplyKnockback(attachedHand.forward * deployLaunchStrength); //Launch player in the direction blade is being deployed in
        SendHapticImpulse(deployHaptics);                                   //Play haptic impulse upon deployment
        StartCoroutine(DeploySequence());                                   //Begin deployment sequence

        //Cleanup:
        timeAtSpeed = 0;                    //Reset activation time tracker
        deployed = true;                    //Indicate that blade is now deployed
        primaryWeapon.inputDisabled = true; //Disable input on primary weapon
    }
    /// <summary>
    /// Un-deploys the blade and fires once.
    /// </summary>
    private void Sheath()
    {
        //Initial effects:
        SendHapticImpulse(sheathHaptics); //Play haptic impulse when sheathing
        StartCoroutine(SheathSequence()); //Begin sheath sequence

        //Cleanup:
        timeAtSpeed = 0;                  //Reset activation time tracker
        timeDeployed = 0;                 //Reset deployment timer
        deployed = false;                 //Indicate that blade is no longer deployed
    }
    public override void OnTriggerInput(InputAction.CallbackContext context)
    {
        if (deployed) //Only accept trigger input if blade is deployed
        {
            base.OnTriggerInput(context); //Use base trigger method checks
        }
    }
    public override void Fire()
    {
        base.Fire(); //Call base fire method as normal
        Sheath();    //Sheath blade when fired
    }
    public override void OnEjectInput(InputAction.CallbackContext context) { } //This method is left empty because melee weapons do not eject their shells
    public override void CloseBreach() { } //This method is left empty because melee weapons do not have a breach
}
