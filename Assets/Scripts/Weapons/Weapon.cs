using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponEvent : System.EventArgs {
    public WeaponData weaponData;
}


public abstract class Weapon : MonoBehaviour {

    [SerializeField]
    protected WeaponData weaponData;

    protected float timer = 0;

    public WeaponData GetWeaponData() {
        return weaponData;
    }

    public float CooldownPercent() {
        return Mathf.Min(timer / weaponData.cooldown, 1);
    }

    //check to make sure we're able to attack
    public abstract bool CanAttack();

    //actually do the attack
    public abstract WeaponEvent Attack();

    public abstract void PutAway();

    public abstract void Equip();

    public void Update() {
        timer += Time.deltaTime;
    }


}
