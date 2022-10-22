using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zombie : MonoBehaviour {

    public int index;

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    private void OnCollisionEnter(Collision collision) {
        Zombie zed = null;
        if (collision.gameObject.TryGetComponent(out zed)) {
            HordeManager.instance.OnZombieCollision(index, zed.index);
        }
    }
}
