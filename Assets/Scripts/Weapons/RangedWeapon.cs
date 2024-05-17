using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon {

    public int penetration = 0;
    public int totalAmmo = 10;
    public int clipSize = 6;
    public int clipCount = 6;

    LineRenderer lr;

    public Transform barrel;

    private void Awake() {
        //start with weapons loaded
        timer = weaponData.cooldown;
        lr = GetComponent<LineRenderer>();
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

    private void Update() {
        base.Update();
        lr.SetPosition(0, barrel.position);
        RaycastHit hitInfo;
        if (Physics.Raycast(barrel.position, barrel.forward, out hitInfo, weaponData.range, Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
            lr.SetPosition(1, hitInfo.point);
        }
        else {
            lr.SetPosition(1, barrel.position + barrel.forward*weaponData.range);
        }
    }

    public override WeaponEvent Attack() {
        //do a raycast and see if we hit anything
        //shoot a raycast from here to the target, ignoring self
        Vector3 dp = barrel.forward * weaponData.range;


        List<RaycastHit> hitInfo =
                new List<RaycastHit>((Physics.RaycastAll(barrel.position, dp, dp.magnitude + 1, Physics.AllLayers, QueryTriggerInteraction.Ignore)));

        //distance is undefined, so we need to calculate it here
        hitInfo.Sort((a, b) => (a.point - barrel.position).sqrMagnitude.CompareTo(
                                (b.point - barrel.position).sqrMagnitude));

        Zombie zed = null;
        int hits = 0;
        for (int i = 0; i < hitInfo.Count; i++) {
            if (hits > penetration) break; //break out if we reach pen. limit early

            if (hitInfo[i].collider.TryGetComponent(out zed)) {
                zed.Hit(Mathf.Max(weaponData.damage - hits, 0), this.barrel.position); // reduce damage by penetration
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

        return new WeaponEvent() { thisWeapon = this };
    }


}
