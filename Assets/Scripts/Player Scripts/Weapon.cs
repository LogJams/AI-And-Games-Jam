using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponEvent : System.EventArgs {
    public float radius;
    public AudioClip sound;
}


public class Weapon : MonoBehaviour {

    public float soundRadius = 50;
    public int damage = 5;
    public int penetration = 0; //how many targets does it go through?

    public bool CanFire() {
        //check if we need to reload or something

        return true;
    }

    public WeaponEvent Fire(Vector3 target) {

        //do a raycast and see if we hit anything
        //shoot a raycast from here to the target, ignoring self
        Vector3 dp = target - transform.position;

        List<RaycastHit> hitInfo =
                new List<RaycastHit>((Physics.RaycastAll(transform.position, dp, dp.magnitude + 1)));

        //distance is undefined, so we need to calculate it here
        hitInfo.Sort((a,b) =>  (a.point - transform.position).sqrMagnitude.CompareTo(
                                (b.point - transform.position).sqrMagnitude));

        Zombie zed = null;
        int hits = 0;
        for (int i = 0; i < hitInfo.Count; i++) {
            if (hits > penetration) break; //break out if we reach pen. limit early

            if (hitInfo[i].collider.TryGetComponent(out zed)) {
                zed.Hit( Mathf.Max(damage - hits, 0)); // reduce damage by penetration
                hits++;
            }

        }

        return new WeaponEvent() { radius = soundRadius };
    }


	
}
