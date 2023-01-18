using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakActionGun : Gun
{
    //Objects & Components:
    

    //Settings:
    [Header("Action Settings:")]
    [Tooltip("Joint which connects break assembly to rest of gun")]                      public ConfigurableJoint breakJoint;
    [SerializeField, Tooltip("Object containing mesh and collider for barrel assembly")] private Transform barrelAssembly;
    [Space()]
    [SerializeField, Range(0, 90), Tooltip("Angle at which barrels will rest when breach is open")]         private float breakAngle;
    [SerializeField, Range(0, 90), Tooltip("Angle at which, when breach is opening, ammo will be ejected")] private float ejectAngle;
    [SerializeField, Tooltip("How forcefully shells are propelled out of breach when ejected")]             private float ejectStrength;

    //Runtime Vars:
    private bool ammoEjected = false; //Whether or not ammo has been ejected (resets when breach closes)

    //RUNTIME METHODS:
    public override void Awake()
    {
        //Initialize:
        base.Awake(); //Call base method

        //Get objects & components:

    }
    public override void Start()
    {
        //Initialize:
        base.Start();         //Call base method
        SetBarrelMode(false); //Make barrel position static
    }
    public override void Update()
    {
        base.Update(); //Call base method
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Opens breach.
    /// </summary>
    private protected override void Eject()
    {
        //Ejection procedure:
        SetBarrelMode(true);                                                      //Enable dynamic barrel assembly
        breakJoint.angularXMotion = ConfigurableJointMotion.Limited;              //Unlock joint rotation
        breakJoint.targetRotation = Quaternion.Euler(Vector3.right * breakAngle); //Set target angle of break joint to break angle

        //Cleanup:
        if (loadedAmmo.Count == 0) ammoEjected = true; //Do not look for ammo ejection if no ammo is present
        base.Eject();                                  //Call base eject method
    }
    /// <summary>
    /// Secondary phase of ejection which removes shells from chambers.
    /// </summary>
    public void EjectAmmo()
    {
        //Validity checks:
        if (!breachOpen) return; //Abort if breach is not open
        if (ammoEjected) return; //Abort if ammo has already been ejected

        //Remove ammo:
        for (int i = 0; i < loadedAmmo.Count; i++) //Iterate through loaded ammo in weapon
        {
            //Initialize:
            Ammo ammo = loadedAmmo[i];                       //Get ammo from list
            Transform chamber = chambers[i];                 //Get corresponding chamber
            Transform extractor = chamber.Find("Extractor"); //Attempt to get extractor attached to chamber

            //Get ejection forces:
            Vector3 extractorPos = chamber.position;             //Default to using chamber position for extractor position
            Vector3 ejectionForce = -chamber.up * ejectStrength; //Default to using chamber orientation for ejection force
            if (extractor != null) //Chamber has an extractor
            {
                extractorPos = extractor.position;                 //Use actual extractor position
                ejectionForce = extractor.forward * ejectStrength; //Use extractor orientation for ejection force
            }
            ejectionForce += rb.velocity; //Add current weapon velocity to final ejection force

            //Perform ejection procedure:
            Physics.IgnoreCollision(ammo.coll, barrelAssembly.GetComponent<Collider>(), true); //Make sure ammo does not collide with barrel assembly
            ammo.Eject(ejectionForce, extractorPos);                                           //Eject ammo from chamber at set velocity
        }
        loadedAmmo.Clear(); //Clear list of loaded ammo

        //Cleanup:
        ammoEjected = true; //Indicate that ammo has been ejected
    }
    /// <summary>
    /// Locks barrels in closed position and enables weapon to fire.
    /// </summary>
    public override void CloseBreach()
    {
        //Validity check:
        if (!breachOpen) return; //Ignore if breach is not open

        //Close breach:
        SetBarrelMode(false);                                       //Disable dynamic barrel assembly
        breakJoint.targetRotation = Quaternion.Euler(Vector3.zero); //Change target pivot rotation to zero
        breakJoint.angularXMotion = ConfigurableJointMotion.Locked; //Lock pivot rotation

        //Cleanup:
        ammoEjected = false;       //Reset ammo ejection switch
        base.CloseBreach(); //Call base close method
    }

    //UTILITY METHODS:
    /// <summary>
    /// Sets barrels to either jointed or static mode.
    /// </summary>
    /// <param name="active"></param>
    private void SetBarrelMode(bool active)
    {
        
    }
}
