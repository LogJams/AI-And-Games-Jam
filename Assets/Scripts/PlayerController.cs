using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    CharacterController cc;

    Vector3 lookTarget = Vector3.zero;

    public float moveSpeed = 2.5f; // m/s
    public float rotSpeed = 30f;   // rad/s


    LineRenderer line;
    public Weapon weapon;

    public event System.EventHandler<WeaponEvent> OnFireWeapon;


    // Start is called before the first frame update
    void Start() {
        cc = GetComponent<CharacterController>();
        line = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update() {

        //movement
        Movement();
        Vector3 aim = Look();

        //todo: perhaps have this per weapon, and use to to figure out what to shoot
        line.SetPosition(0, weapon.transform.position);
        line.SetPosition(1, aim);


        //attack by clicking
        if (Input.GetMouseButtonDown(0)) {

            if (weapon.CanFire()) {
                //attack!
                WeaponEvent we = weapon.Fire(lookTarget);

                OnFireWeapon?.Invoke(this.gameObject, we);

            }
            else {


            }

        }



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
