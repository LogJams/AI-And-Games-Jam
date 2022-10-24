using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiPauseMenu : MonoBehaviour {

    public GameObject childPanel;

    private void Start() {
        Toggle();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Toggle();
        }
    }



    public void Toggle() {
        childPanel.SetActive(!childPanel.activeSelf);
    }

    public void MainMenu() {

    }

    public void Exit() {
        Application.Quit(0);
    }


}
