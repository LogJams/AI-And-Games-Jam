using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zombie : MonoBehaviour {

    public int index;

    public int health = 5;

    // Start is called before the first frame update
    void Start() {
        GameManager.instance.OnNoiseEvent += OnNoiseTrigger;
    }

    


    public void OnNoiseTrigger(System.Object src, NoiseEvent e) {
        if ( (transform.position - e.location).sqrMagnitude <= e.sqRange ) {
            HordeManager.instance.ZombieTrigger(index, e.location);
        }
    }


    private void OnCollisionEnter(Collision collision) {
        Zombie zed = null;
        //if it's another zombie, move with it
        if (collision.gameObject.TryGetComponent(out zed)) {
            HordeManager.instance.OnZombieCollision(index, zed.index);
        }
        //otherwise just stop
        else {
            HordeManager.instance.OnZombieCollision(index);
        }
    }

    public void Hit(int dmg) {
        health -= dmg;
        if (health <= 0) {
            HordeManager.instance.ZombieDeath(index);
        }
    }
}
