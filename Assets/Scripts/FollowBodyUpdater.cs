using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBodyUpdater : MonoBehaviour
{
    //DESCRIPTION: Goes on followBody's velocity reference, checks for collisions to reduce jank when running into walls.

    internal List<FollowBody> fb = new List<FollowBody>(); //FollowBody components this script will update
    private void OnCollisionEnter(Collision collision)
    {
        foreach (FollowBody body in fb) body.PerformUpdate(); //Update each followbody position immediately upon collision
    }
}
