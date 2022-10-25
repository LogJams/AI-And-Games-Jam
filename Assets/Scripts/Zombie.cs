using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zombie : MonoBehaviour {

    public int index;

    public int health = 5;

    public Animator anim;

    // Start is called before the first frame update
    void Start() {
        GameManager.instance.OnNoiseEvent += OnNoiseTrigger;
        anim = GetComponentInChildren<Animator>();

        anim.enabled = false;

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

        if (collision.gameObject.CompareTag("Player")) {
            anim.SetTrigger("attack");
        }
    }

    public void Hit(int dmg, Vector3 source) {
        health -= dmg;
        if (health <= 0) {
            HordeManager.instance.ZombieDeath(index);
            anim.enabled = false;
        }
        else {
            source.y = 0;
            HordeManager.instance.ZombieTrigger(index, source);
        }
    }
}
