using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    CharacterController cc;

    Vector3 lookTarget = Vector3.zero;

    public float moveSpeed = 2.5f; // m/s
    public float rotSpeed = 30f;   // rad/s


    LineRenderer line;
    public List<Weapon> weapons;
    int currentWeapon = 0;

    public event System.EventHandler<WeaponEvent> OnFireWeapon;
    public event System.EventHandler<WeaponEvent> OnSwitchWeapon;


    private void Awake() {
        //disable our other weapons
        for (int i = 0; i < weapons.Count; i++) {
            weapons[i].gameObject.SetActive(i == currentWeapon);
        }
        cc = GetComponent<CharacterController>();
        line = GetComponent<LineRenderer>();
    }

    // Start is called before the first frame update
    void Start() {

    }

    public Weapon CurrentWeapon() {
        return weapons[currentWeapon];
    }

    // Update is called once per frame
    void Update() {

        SelectWeapon();

        Movement();
        Vector3 aim = Look();

        //todo: perhaps have this per weapon, and use to to figure out what to shoot
        line.SetPosition(0, weapons[currentWeapon].transform.position);
        line.SetPosition(1, aim);


        //attack by clicking
        if (Input.GetMouseButtonDown(0)) {

            if (weapons[currentWeapon].CanAttack()) {
                //attack!
                WeaponEvent we = weapons[currentWeapon].Attack(lookTarget);

                OnFireWeapon?.Invoke(this.gameObject, we);

            }
            else {
                //notify the player, perhaps play a click sound if it's a gun?

            }
        }



    }

    void SelectWeapon() {
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentWeapon != 0) {
            SwitchWeapon(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && currentWeapon != 1) {
            SwitchWeapon(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && currentWeapon != 2) {
        //    SwitchWeapon(2);
        }
    }

    void SwitchWeapon(int idx) {
        weapons[currentWeapon].gameObject.SetActive(false);
        weapons[currentWeapon].PutAway();
        currentWeapon = idx;
        weapons[currentWeapon].gameObject.SetActive(true);
        weapons[currentWeapon].Equip();
        //play some animations??

        OnSwitchWeapon?.Invoke(this.transform, new WeaponEvent() { weaponData = weapons[currentWeapon].GetWeaponData() });
    }


    void Movement() {
        //moving with the keyboard
        Vector3 motion = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        cc.SimpleMove(motion * moveSpeed);
    }

    Vector3 Look() {
        //aiming with the mouse
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        if (Physics.Raycast(mouseRay, out hitInfo)) {
            lookTarget = hitInfo.point;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget, Vector3.up);
            //lift the hit point up 1m to the center of mass
            Vector3 point = hitInfo.point;
            point.y = 1f;
            return point;

        } else {
            return transform.position;
        }
    }

}
