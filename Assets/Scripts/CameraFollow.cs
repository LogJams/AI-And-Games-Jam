using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public Transform target;

    [SerializeField] float trackingBias = 5.0f;

    private Vector3 boom;

    // Start is called before the first frame update
    void Start() {
        boom = this.transform.position - target.transform.position;
    }

    // Update is called once per frame
    void Update() {
        transform.position = Vector3.Lerp(transform.position, target.position + boom, trackingBias * Time.deltaTime);
    }
	
}
