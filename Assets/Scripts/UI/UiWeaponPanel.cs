using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class UiWeaponPanel : MonoBehaviour {

    public PlayerController player;

    public Image weaponSprite;
    public Image ammoIndicator;
    public TMPro.TMP_Text ammoText;

    // Start is called before the first frame update
    void Start() {
        //todo: set the weapon sprite
        player.OnFireWeapon += WeaponUpdateEvent;
        player.OnSwitchWeapon += WeaponUpdateEvent;
        UpdateDisplay(player.CurrentWeapon());
    }

    void WeaponUpdateEvent(System.Object src, WeaponEvent e) {
        UpdateDisplay(player.CurrentWeapon());
    }


    void UpdateDisplay(Weapon weapon) {

        weaponSprite.sprite = weapon.GetWeaponData().icon;

        if (weapon is RangedWeapon) {
            RangedWeapon gun = (RangedWeapon) weapon;
            //update sprite fill (cooldown), sprite fill (ammo), and text (ammo)
            ammoIndicator.fillAmount = (float)gun.AmmoRemaining() / gun.clipSize;
            ammoText.text = "" + gun.AmmoRemaining();
        }
        else {
            //melee weapons have no ammo or anything to display
            ammoIndicator.fillAmount = 1;
            ammoText.text = "";
        }


    }


    private void Update() {
        //show cooldown with the sprite fill
        weaponSprite.fillAmount = player.CurrentWeapon().CooldownPercent();
    }

}
