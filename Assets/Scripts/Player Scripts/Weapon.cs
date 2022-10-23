using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponEvent : System.EventArgs {
    public float radius;
    public AudioClip sound;
}


public class Weapon : MonoBehaviour {

    float soundRadius = 50;


    public bool CanFire() {
        //check if we need to reload or something

        return true;
    }

    public WeaponEvent Fire(Vector3 target) {

        //do a raycast and see if we hit anything

        //if so, damage it


        return new WeaponEvent() { radius = soundRadius };
    }


	
}
