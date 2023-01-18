using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
/// <summary>
/// PLAYER PASSIVE ABILITY: Consecutive shots in the same direction will add increased acceleration.
/// </summary>
public class PlayerPassive_ConsecShotBoost : MonoBehaviour
{
    //Objects & Components:
    private PlayerController player; //Playercontroller script

    //Settings:
    [SerializeField, Tooltip("Max amount of force added by boost (compounds for each consecutive shot)")]          private float boostPower;
    [SerializeField, MinMaxSlider(0, 1), Tooltip("Determines how time between shots affects boost power")]         private Vector2 timeRange;
    [SerializeField, MinMaxSlider(0, 90), Tooltip("Determines how angle discrepancy affects boost power")]         private Vector2 angleRange;
    [SerializeField, Range(0, 1), Tooltip("Percent of boost power to carry over from previous consecutive shots")] private float carryoverInfluence;

    //Runtime vars:
    private float timeSinceLastShot; //Amount of time which has passed since player last fired a weapon
    private Vector3 lastShotForce;   //Force applied by last shot
    private float carryoverForce;    //Force carried over from previous boosts for consecutive shots

    //RUNTIME METHODS:
    private void Start()
    {
        //Get objects & components:
        if (!TryGetComponent(out player)) { print("Unable to locate player script"); Destroy(this); } //Get playerCOntroller component and post error if it cannot be found

        //Event subscriptions:
        player.onKnockbackApplied += OnKnockbackApplied; //Subscribe to OnKnockbackApplied event
    }
    private void OnDestroy()
    {
        //Event unsubscriptions:
        player.onKnockbackApplied -= OnKnockbackApplied; //Unsubscribe from OnKnockbackApplied event
    }
    private void Update()
    {
        //Update time tracker:
        timeSinceLastShot += Time.deltaTime; //Add to time since last shot
    }
    private void OnKnockbackApplied(Vector3 force)
    {
        //Initialize:
        Vector3 newShotForce = force;                                 //Store current force vector in new container
        float angleDiff = Vector3.Angle(lastShotForce, newShotForce); //Get angle difference between last shot and current shot

        //Get multipliers:
        float timeMultiplier = Mathf.Clamp(Mathf.InverseLerp(timeRange.y, timeRange.x, timeSinceLastShot), 0, 1); //Get clamped multiplier value based on how quickly this shot followed the last one
        float angleMultiplier = Mathf.Clamp(Mathf.InverseLerp(angleRange.y, angleRange.x, angleDiff), 0, 1);      //Get clamped multiplier value based on difference in angle from last shot
        float totalMultiplier = timeMultiplier * angleMultiplier;                                                 //Get value for total multiplier being applied to boost force
        //print("Time = " + timeMultiplier + " | Angle = " + angleMultiplier);

        //Get new knockback force:
        if (totalMultiplier > 0) //Shot has a non-zero multiplier in both areas and qualifies for boost
        {
            float forceAdd = (boostPower + carryoverForce) * totalMultiplier;    //Calculate boost force using multiplier, base boost value, and added carryover force from previous consecutive shots
            carryoverForce += boostPower * totalMultiplier * carryoverInfluence; //Store residual carryover force for next shot
            newShotForce += forceAdd * newShotForce.normalized;                  //Add force to existing knockback vector
            player.rb.velocity = newShotForce / player.rb.mass;                  //Apply adjusted player velocity
        }
        else //Shot does not qualify for boost
        {
            carryoverForce = 0; //Reset carryover force on non-boost shot
        }

        //Cleanup:
        lastShotForce = newShotForce; //Store shot force for next shot
        timeSinceLastShot = 0;        //Reset time tracker
    }
}
*/