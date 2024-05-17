using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NoiseEvent {
    public Vector3 location;
    public float sqRange;
}

public class GameManager : MonoBehaviour {

    public static GameManager instance;
    public PlayerController player;


    public event System.EventHandler<NoiseEvent> OnNoiseEvent;

    public float TimeScale = 1.0f;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void Start() {
        player.OnFireWeapon += PlayerAttack;
        Time.timeScale = TimeScale;

    }




    public void PlayerAttack(System.Object src, WeaponEvent e) {
        WeaponData weaponData = e.thisWeapon.GetWeaponData();
        OnNoiseEvent?.Invoke(src, new NoiseEvent() { location = player.transform.position, sqRange = weaponData.soundRadius * weaponData.soundRadius });
    }





}
