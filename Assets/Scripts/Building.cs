using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {

    public GameObject roofItems;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            roofItems.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            roofItems.SetActive(true);
        }
    }

}
