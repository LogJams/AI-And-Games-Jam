using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorInteraction : MonoBehaviour {

    Transform door;
    bool closed = true;
    bool animating = false;

    Quaternion qClosed = Quaternion.AngleAxis(-90, Vector3.up);
    Quaternion qOpen = Quaternion.identity;

    float swingTime = 0.5f;
    float timer = 0;

    // Start is called before the first frame update
    void Start() {
        door = transform.GetChild(0);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && closed) {
            timer = 0;
            closed = false;
            animating = true;
        }
    }

    // Update is called once per frame
    void Update() {
        //todo: put this in an animation coroutine or something
        if (!animating) return;
        timer += Time.deltaTime;

        door.localRotation = Quaternion.Lerp(qClosed, qOpen, timer / swingTime);

        if (timer >= swingTime) {
            animating = false;
            door.localRotation = qOpen;
        }

    }
	
}
