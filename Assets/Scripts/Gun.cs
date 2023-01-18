using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

public class Gun : MonoBehaviour
{
    //Objects & Components:
    private protected AudioSource audioSource; //Audio source component on this weapon
    internal PlayerController player;          //The player controlling this weapon
    internal Rigidbody rb;                     //Rigidbody component attached to this weapon
    private ConfigurableJoint joint;           //Joint component attached to the top level of this weapon
    private Transform recoilTarget;            //Object indicating where weapon moves towards during recoil motion
    private Transform recoilFollower;          //A transform generated by this script and attached to the weapon's controller, used to simulate recoil
    private PlayerInput input;                 //Player input system which controls this weapon
    private protected Rigidbody followBody;    //RigidbodyFollower used by this weapon's configurable joint
    private protected Transform models;        //Container object for visible components of weapon

    public List<Ammo> loadedAmmo = new List<Ammo>(); //List of all pieces of ammo currently loaded in the weapon (first item in list will be next to fire)
    public Transform[] chambers;                     //Array of positions/orientations where pieces of ammo slot into
    public Transform[] barrels;                      //Array of positions/orientations where projectiles will be fired from (indexes numerically)
    public Transform attachedHand;                   //Hand transform this weapon is attached to
    public GameObject followerPrefab;                //Prefab used to generate rigidbody follower object
    [Space()]
    [SerializeField, Tooltip("Generic ammo prefab which may be automatically loaded into gun under some circumstances")] private GameObject defaultAmmoPrefab;

    //Settings:
    [Header("General Settings:")]
    [SerializeField, Tooltip("Base positional offset for weapon (where it is held)")]                       private Vector3 baseOffset;
    [SerializeField, Tooltip("Base maximum angular speed of weapon rigidbody (affects weapon swinginess)")] private float maxAngularSpeed = 15;
    [SerializeField, Tooltip("Maximum angular speed during recoil phase")]                                  private float recoilAngularSpeed = 100;
    [SerializeField, Tooltip("Amount of upward force applied to end of barrel when weapon is fired")]       private float recoilTorque;
    [SerializeField, Tooltip("Amount of upward force applied to end of barrel when breach is opened")]      private float ejectTorque;
    [Header("Trigger Feel:")]
    [SerializeField, Range(0, 1), Tooltip("Point in trigger pull at which weapon will fire")]                                              private float triggerFireThreshold = 1;
    [SerializeField, Range(0, 1), Tooltip("How far forward trigger must move after firing in order for weapon to be ready to fire again")] private float triggerReleaseThreshold = 0.5f;
    [Header("Haptics:")]
    [SerializeField, Tooltip("Intensity (X, 0-1) and duration (Y) of haptic response when weapon is fired")]     private Vector2 fireHaptics;
    [SerializeField, Tooltip("Intensity (X, 0-1) and duration (Y) of haptic response when trigger is actuated")] private Vector2 triggerClickHaptics;
    [SerializeField, Tooltip("Intensity (X, 0-1) and duration (Y) of haptic response when breach is opened")]    private Vector2 ejectHaptics;
    [SerializeField, Tooltip("Intensity (X, 0-1) and duration (Y) of haptic response when breach is closed")]    private Vector2 breachCloseHaptics;
    [Header("Sound Effects:")]
    [SerializeField, Tooltip("Sound which plays when trigger is mechanically actuated")] private AudioClip triggerClickSound;
    [SerializeField, Tooltip("Sound which plays when breach is opened")]                 private AudioClip ejectSound;
    [SerializeField, Tooltip("Sound which plays when breach is closed")]                 private AudioClip breachCloseSound;
    [Header("Visual Effects:")]
    [SerializeField, Tooltip("Curve describing linear movement of weapon during recoil")]                               private AnimationCurve recoilCurve;
    [SerializeField, Tooltip("Standard amount of time weapon takes to return to original position after being fired")]  private float recoilTime;
    [SerializeField, Tooltip("Standard distance weapon travels backward after being fired")]                            private float recoilDistance;
    [SerializeField, Range(0, 1), Tooltip("Intensity of screenshake when firing")]                                      private float fireShakeMagnitude;
    [SerializeField, Tooltip("Duration of screenshake when firing")]                                                    private float fireShakeTime;
    [SerializeField, Tooltip("Determines how precisely aligned with camera weapon has to be for screenshake to occur")] private float fireShakeRadius;
    [SerializeField, Tooltip("Describes intensity of shake effect depending on how aligned weapon is with camera")]     private AnimationCurve fireShakeCurve;
    [SerializeField, Tooltip("Describes scale multiplier over course of recoilScaleTime after weapon is fired")]        private AnimationCurve recoilScaleCurve;
    [SerializeField, Tooltip("Duration of recoil scale effect")]                                                        private float recoilScaleTime;
    [SerializeField, Min(1), Tooltip("Maximum scale adjustment during recoil phase")]                                   private float maxRecoilScaleMultiplier;
    [Header("Debug Settings:")]
    [SerializeField, Tooltip("When enabled, firing the weapon will not expend ammunition")]  private bool infiniteAmmo;
    [SerializeField, Tooltip("When enabled, weapon will constantly update based on offset")] private bool constantOffsetUpdate;

    //Runtime Vars:
    internal bool rightHand;             //Indicates which hand this weapon belongs to
    private InputDeviceRole deviceRole;  //Which hand this weapon is attached to
    private InputActionMap inputMap;     //Map which this weapon's controls are currently connected to
    private bool triggerPulled = false;  //Whether or not the trigger is currently pulled
    internal bool breachOpen = false;    //Whether or not weapon is in reloading mode (unprepared to fire)
    internal int currentBarrelIndex = 0; //Index of next barrel to fire in barrel array
    [SerializeField] internal bool inputDisabled = false; //When true, weapon will not check for player inputs

    //EVENTS & COROUTINES:
    public delegate void ProjectilesSpawnedEvent(Projectile[] projectiles); //Delegate for passing array of projectiles
    public ProjectilesSpawnedEvent onProjectilesSpawned;                    //Event called during firing sequence right after projectiles are spawned
    /// <summary>
    /// Performs weapon recoil cycle.
    /// </summary>
    /// <param name="powerMultiplier">Can be used to increase or decrease overall effect of recoil.</param>
    public IEnumerator DoRecoil(float powerMultiplier)
    {
        //Initialize:
        rb.maxAngularVelocity = recoilAngularSpeed;                                                  //Make weapon very swingy for recoil
        float duration = Mathf.Max(recoilTime, recoilScaleTime);                                     //Get total duration of recoil phase (for whichever effect is longer)
        recoilTarget.localPosition = baseOffset + (recoilDistance * powerMultiplier * Vector3.back); //Set position of recoil target
        recoilTarget.localScale = Vector3.one * maxRecoilScaleMultiplier;                            //Set scale of recoil target

        //Move weapon:
        ApplyBarrelTorque(recoilTorque * powerMultiplier); //Apply recoil torque to barrel
        for (float totalTime = 0; totalTime < duration; totalTime += Time.fixedDeltaTime) //Iterate once each fixed update for duration of recoil phase
        {
            //Angular speed cap:
            float timeValue = totalTime / duration;                                             //Get value representing progression through recoil phase
            rb.maxAngularVelocity = Mathf.Lerp(recoilAngularSpeed, maxAngularSpeed, timeValue); //Adjust max angular speed back to normal throughout phase

            //Linear weapon movement:
            if (recoilTime != 0 && totalTime <= recoilTime) //Only check linear weapon movement for as long as it is relevant
            {
                timeValue = recoilCurve.Evaluate(totalTime / recoilTime);                                                      //Get interpolant value for new weapon position
                recoilFollower.localPosition = baseOffset + Vector3.Lerp(Vector3.zero, recoilTarget.localPosition, timeValue); //Move weapon model to interpolated position
            }

            //Weapon rescaling:
            if (recoilScaleTime != 0 && totalTime <= recoilScaleTime) //Only check weapon scaling for as long as it is relevant
            {
                timeValue = recoilScaleCurve.Evaluate(totalTime / recoilScaleTime);                   //Get interpolant value for new weapon scale
                transform.localScale = Vector3.Lerp(Vector3.one, recoilTarget.localScale, timeValue); //Scale weapon model to interpolated size
            }

            //Cleanup:
            yield return new WaitForFixedUpdate(); //Wait for next update
        }

        //Cleanup:
        rb.maxAngularVelocity = maxAngularSpeed;   //Set angular speed cap back to default
        recoilFollower.localPosition = baseOffset; //Move weapon model back to natural position at end of recoil phase
    }

    //RUNTIME METHODS:
    public virtual void Awake()
    {
        //Get objects & components:
        if (!TryGetComponent(out audioSource)) Debug.LogWarning(name + " is missing audio source");                                                   //Get audio source component and post warning if it is missing
        if (!TryGetComponent(out rb)) Debug.LogWarning(name + " is missing rigidbody component");                                                     //Get rigidbody component and post warning if it is missing
        if (!TryGetComponent(out joint)) Debug.LogWarning(name + " is missing configurable joint component");                                         //Get configurable joint component and post warning if it is missing
        player = GetComponentInParent<PlayerController>(); if (player == null) Debug.LogWarning(name + " could not find playerController in parent"); //Get playerController and post warning if it is missing
        if (!player.TryGetComponent(out input)) Debug.LogWarning(name + " could not find PlayerInput component in player object");                    //Get player input component and post warning if it is missing

        //Detect hand side:
        Transform altModels = transform.Find("AltModels"); //Try to get alternate model container
        if (attachedHand.name.Contains("Left") || attachedHand.name.Contains("left")) //Attached hand is left
        {
            //Update status:
            rightHand = false;                                    //Indicate that this is the left hand
            deviceRole = InputDeviceRole.LeftHanded;              //Set corresponding device role (for haptics)
            if (altModels != null) Destroy(altModels.gameObject); //Destroy alternate models if possible
            models = transform.Find("Models");                    //Store model container transform
        }
        else //Attached hand is right
        {
            //Update status:
            rightHand = true;                         //Indicate that this is the right hand
            deviceRole = InputDeviceRole.RightHanded; //Set corresponding device role (for haptics)

            //Flip:
            baseOffset.x *= -1; //Flip positional offset along X axis
            if (altModels != null) //A flipped model is available
            {
                for (int x = 0; x < barrels.Length; x++) barrels[x] = altModels.Find(barrels[x].name);    //Replace barrels with their alternate model versions
                for (int x = 0; x < chambers.Length; x++) chambers[x] = altModels.Find(chambers[x].name); //Replace chambers with their alternate model versions
                Destroy(transform.Find("Models").gameObject);                                             //Destroy original model set
            }
            models = altModels; //Store model container transform
        }
        inputMap = input.actions.FindActionMap("XRI " + (rightHand ? "RightHand" : "LeftHand") + " Interaction"); //Get interaction map from input (use ternary operation to get correct map depending on hand side)

        //Set up misc variables:
        rb.maxAngularVelocity = maxAngularSpeed; //Set base max rotation value

        //Event subscriptions:
        inputMap.actionTriggered += InputActionTriggered; //Hook up input action event to player input system
        onProjectilesSpawned += EmptyMethod;              //Subscribe to empty method to prevent weirdness when weapon has no modifiers
    }
    public virtual void Start()
    {
        //Set up recoil system:
        followBody = Instantiate(followerPrefab, player.transform).GetComponent<Rigidbody>();                                          //Generate followbody object and get reference to its rigidbody
        followBody.GetComponent<FollowBody>().velocityReference = player.GetComponentInChildren<XROrigin>().GetComponent<Rigidbody>(); //Get rigidbody from player XR origin and use that as followbody velocity reference
        joint.connectedBody = followBody;                                                                                              //Link configurable joint to designated rigidbody

        recoilTarget = new GameObject().transform; //Instantiate recoil target object
        recoilTarget.parent = transform;           //Child recoil target to weapon object
        recoilTarget.name = "RecoilTarget";        //Set name so hierarchy is less confusing

        recoilFollower = new GameObject().transform;                   //Instantiate follower object
        recoilFollower.parent = attachedHand;                          //Child follower object to tracked hand controller
        recoilFollower.localPosition = baseOffset;                     //Ensure that follower is aligned with parent
        recoilFollower.name = "RecoilFollower";                        //Set name so hierarchy is less confusing
        followBody.GetComponent<FollowBody>().target = recoilFollower; //Replace followbody target with recoilFollower (inserting it in follow chain)

        //Detect starting items:
        foreach (Ammo ammo in GetComponentsInChildren<Ammo>()) { if (!LoadAmmo(ammo)) Destroy(ammo); } //Load or destroy each piece of ammo childed to weapon at start of game
    }
    public virtual void Update()
    {
        //Debug stuff:
        if (constantOffsetUpdate) recoilFollower.localPosition = baseOffset; //Update recoilFollower position constantly if setting is active
    }
    public virtual void OnDestroy()
    {
        if (inputMap != null) inputMap.actionTriggered -= InputActionTriggered; //Unsubscribe from input event if inputs have been set up on this weapon
    }

    //INPUT METHODS:
    private void InputActionTriggered(InputAction.CallbackContext context)
    {
        if (inputDisabled) return; //Ignore all input if inputs are disabled
        switch (context.action.name) //Determine behavior depending on action name
        {
            case "Trigger": OnTriggerInput(context); break; //Trigger input actions
            case "Eject": OnEjectInput(context); break;     //Eject input actions
            default: break;                                 //Ignore unrecognized actions
        }
    }
    public virtual void OnTriggerInput(InputAction.CallbackContext context)
    {
        //Initialization:
        float triggerPosition = context.ReadValue<float>(); //Get trigger position

        //Trigger events:
        if (!triggerPulled) //Trigger is not currently being pulled
        {
            if (triggerPosition >= triggerFireThreshold) //Trigger has been pulled
            {
                Fire();                                 //Attempt to fire weapon
                triggerPulled = true;                   //Indicate that trigger has been pulled
            }
        }
        else //Trigger is already being pulled
        {
            if (triggerPosition <= triggerReleaseThreshold) //Trigger is being released
            {
                SendHapticImpulse(triggerClickHaptics); //Send haptic pulse which indicates the trigger has passed its actuation point
                triggerPulled = false;                  //Indicate that trigger is no longer being pulled
            }
        }
    }
    public virtual void OnEjectInput(InputAction.CallbackContext context)
    {
        if (context.started && !breachOpen) Eject(); //Trigger ejection procedure if input has been pressed and breach is not already open
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Fires weapon if able.
    /// </summary>
    public virtual void Fire()
    {
        //Check for dry-fire:
        if (loadedAmmo.Count == 0) { DryFire(); return; } //Dry-fire if gun has no ammo
        if (breachOpen) { DryFire(); return; }            //Dry-fire if breach is open

        //Check ammo:
        Ammo hotAmmo = loadedAmmo[0];                                                  //Get ammunition piece to fire
        if (loadedAmmo.Count > 1) { loadedAmmo.RemoveAt(0); loadedAmmo.Add(hotAmmo); } //If there is currently more than one piece of ammunition in the weapon, move this piece to the back of the order
        if (hotAmmo.spent) { DryFire(); return; }                                      //Dry-fire if active ammo is spent

        //Firing procedure:
        Projectile[] projectiles = hotAmmo.Shoot(this, barrels[currentBarrelIndex]); //Shoot projectile(s) from active barrel
        onProjectilesSpawned(projectiles);                                           //Trigger events which occur when projectiles are spawned
        if (infiniteAmmo) hotAmmo.spent = false;                                     //Un-spend ammo if infinite ammo is turned on
        if (barrels.Length > 1)                                                      //Weapon alternates between multiple barrels
        {
            currentBarrelIndex++;                                             //Index to next barrel
            if (currentBarrelIndex >= barrels.Length) currentBarrelIndex = 0; //Roll over to first barrel if last barrel was just fired
        }

        //Effects:
        float shotAngle = Vector3.Angle(GetHeadDirection(), player.head.forward); //Get angle between weapon facing direction and player look direction
        if (shotAngle <= fireShakeRadius) //Angle is within designated radius
        {
            shotAngle = fireShakeCurve.Evaluate(1 - (shotAngle / fireShakeRadius));        //Get value for how close weapon is to being perfectly aligned with player head
            player.ShakeScreen(fireShakeMagnitude * shotAngle, fireShakeTime * shotAngle); //Activate screen shake effect (scaled proportionately depending on alignment with head)
        }
        SendHapticImpulse(fireHaptics); //Activate haptic effect
    }
    /// <summary>
    /// Opens breach and ejects all ammo from weapon (should be overridden by child classes).
    /// </summary>
    private protected virtual void Eject()
    {
        //Validity checks:
        if (breachOpen) return; //Ignore if breach is already open

        //Effects:
        ApplyBarrelTorque(-ejectTorque);     //Apply ejection torque to barrel
        SendHapticImpulse(ejectHaptics);     //Play haptic impulse
        audioSource.PlayOneShot(ejectSound); //Play sound effect

        //Cleanup:
        breachOpen = true; //Indicate that the breach is now open
    }
    /// <summary>
    /// Closes breach and enables weapon firing.
    /// </summary>
    public virtual void CloseBreach()
    {
        //Validity checks:
        if (!breachOpen) return; //Ignore if breach is already closed

        //Effects:
        SendHapticImpulse(breachCloseHaptics);     //Play haptic impulse
        audioSource.PlayOneShot(breachCloseSound); //Play sound effect

        //Cleanup:
        breachOpen = false; //Indicate that breach is now closed
    }
    /// <summary>
    /// Loads given item of ammo into weapon.
    /// </summary>
    /// <returns>Whether or not the ammo was successfully loaded.</returns>
    public bool LoadAmmo(Ammo ammo)
    {
        //Check loading conditions:
        if (loadedAmmo.Count >= chambers.Length) return false; //Do not load ammo if there is no room in weapon

        //Prep ammo:
        ammo.rb.isKinematic = true; //Ensure ammo is not affected by physics while loaded
        ammo.coll.enabled = false;  //Disable ammo collider while loaded

        //Loading procedure:
        loadedAmmo.Add(ammo);                               //Add ammo to list of loaded ammo pieces
        Transform chamber = chambers[loadedAmmo.Count - 1]; //Get chamber ammo is being loaded into
        ammo.transform.parent = chamber;                    //Child ammo to chamber
        ammo.transform.localPosition = Vector3.zero;        //Move ammo to chamber position
        ammo.transform.localEulerAngles = Vector3.zero;     //Orient ammo to chamber

        //Cleanup:
        return true; //Indicate that ammo was successfully loaded
    }
    /// <summary>
    /// Loads all weapon chambers with copies of given ammo item (does not actually move given ammo object into weapon).
    /// </summary>
    public void FullyLoadAmmo(Ammo ammo)
    {
        //Loading procedure:
        while (loadedAmmo.Count < chambers.Length) LoadAmmo(Instantiate(ammo.gameObject).GetComponent<Ammo>()); //Load copies of ammo object into weapon until weapon is full

        //Effects:
        SendHapticImpulse(ejectHaptics);         //Play haptic effect
        audioSource.PlayOneShot(ammo.loadSound); //Play ammo loading sound
    }
    /// <summary>
    /// Loads all weapon chambers with copies of weapon's designated default ammo item (overrides/destroys shells currently in weapon).
    /// </summary>
    public void FullyLoadAmmo()
    {
        if (defaultAmmoPrefab == null) { Debug.LogWarning("Weapon " + gameObject.name + " has no defaultAmmoPrefab and cannot use FullyLoadAmmo()"); return; } //Log warning and skip if no prefab is provided
        foreach (Ammo shellCasing in loadedAmmo) Destroy(shellCasing.gameObject);                                                                              //Destroy any ammo left in weapon
        loadedAmmo = new List<Ammo>();                                                                                                                         //Clear loaded ammo list
        FullyLoadAmmo(defaultAmmoPrefab.GetComponent<Ammo>());                                                                                                 //Call base loading method
    }

    //UTILITY METHODS:
    /// <summary>
    /// Returns direction weapon is currently being held relative to player head.
    /// </summary>
    public Vector3 GetHeadDirection()
    {
        return Vector3.Normalize(transform.position - player.head.position); //Return normalized direction between weapon and head
    }
    /// <summary>
    /// Applies given amount of force as torque to end of active barrel.
    /// </summary>
    public void ApplyBarrelTorque(float force)
    {
        //Validity checks:
        if (barrels.Length == 0) return;                  //Ignore if weapon has no barrels (for some reason)
        if (currentBarrelIndex >= barrels.Length) return; //Ignore if current barrel index is out of range

        //Apply force:
        Vector3 torqueForce = force * barrels[currentBarrelIndex].up;                                //Get vector for force applied to barrel
        rb.AddForceAtPosition(torqueForce, barrels[currentBarrelIndex].position, ForceMode.Impulse); //Apply instantaneous upward torque to end of weapon barrel
    }
    /// <summary>
    /// Procedure for when weapon tries to fire but cannot for some reason.
    /// </summary>
    private void DryFire()
    {
        SendHapticImpulse(triggerClickHaptics);     //Play trigger click haptics
        audioSource.PlayOneShot(triggerClickSound); //Play trigger click sound
    }
    /// <summary>
    /// Sends a haptic impulse to this weapon's controller.
    /// </summary>
    /// <param name="amplitude">Strength of vibration (between 0 and 1).</param>
    /// <param name="duration">Duration of vibration (in seconds).</param>
    public void SendHapticImpulse(float amplitude, float duration)
    {
        List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>(); //Initialize list to store input devices
        #pragma warning disable CS0618                                                     //Disable obsolescence warning
        UnityEngine.XR.InputDevices.GetDevicesWithRole(deviceRole, devices);               //Find all input devices counted as right hand
        #pragma warning restore CS0618                                                     //Re-enable obsolescence warning
        foreach (var device in devices) //Iterate through list of devices identified as right hand
        {
            if (device.TryGetHapticCapabilities(out UnityEngine.XR.HapticCapabilities capabilities)) //Device has haptic capabilities
            {
                if (capabilities.supportsImpulse) device.SendHapticImpulse(0, amplitude, duration); //Send impulse if supported by device
            }
        }
    }
    /// <summary>
    /// Sends a haptic impulse to this weapon's controller.
    /// </summary>
    /// <param name="haptics">X value is strength (between 0 and 1), Y value is duration (in seconds).</param>
    public void SendHapticImpulse(Vector2 haptics) { SendHapticImpulse(haptics.x, haptics.y); }

    //UTILITY METHODS:
    /// <summary>
    /// Generic empty method for making sure certain events don't break in certain circumstances.
    /// </summary>
    private void EmptyMethod(Projectile[] projectiles) { }
}
