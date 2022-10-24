using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon {

    public int penetration = 0;
    public int totalAmmo = 10;
    public int clipSize = 6;
    public int clipCount = 6;

    private void Awake() {
        //start with weapons loaded
        timer = weaponData.cooldown;
    }

    public override bool CanAttack() {
        return timer >= weaponData.cooldown && clipCount > 0;
    }

    public int AmmoRemaining() {
        return clipCount;
    }

    public override void PutAway() {

    }

    public override void Equip() {
        if (timer <= weaponData.cooldown) {
            timer = 0;
        }
    }


    public override WeaponEvent Attack(Vector3 target) {
        //do a raycast and see if we hit anything
        //shoot a raycast from here to the target, ignoring self
        Vector3 dp = target - transform.position;

        List<RaycastHit> hitInfo =
                new List<RaycastHit>((Physics.RaycastAll(transform.position, dp, dp.magnitude + 1)));

        //distance is undefined, so we need to calculate it here
        hitInfo.Sort((a, b) => (a.point - transform.position).sqrMagnitude.CompareTo(
                                (b.point - transform.position).sqrMagnitude));

        Zombie zed = null;
        int hits = 0;
        for (int i = 0; i < hitInfo.Count; i++) {
            if (hits > penetration) break; //break out if we reach pen. limit early

            if (hitInfo[i].collider.TryGetComponent(out zed)) {
                zed.Hit(Mathf.Max(weaponData.damage - hits, 0), this.transform.position); // reduce damage by penetration
                hits++;
            }

        }

        //reset the timer and use some ammo
        timer = 0;
        clipCount--;
        totalAmmo--;

        if (clipCount == 0) {
            clipCount = clipSize;
        }

        return new WeaponEvent() { weaponData = this.weaponData };
    }


}
