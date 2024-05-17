using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    CharacterController cc;

    Vector3 lookTarget = Vector3.zero;
    Vector3 aim = Vector3.zero;

    public float moveSpeed = 2.5f; // m/s
    public float rotSpeed = 30f;   // rad/s

    public Animator anim;

    //LineRenderer line;

    public List<Weapon> weapons;
    public List<GameObject> holsteredWeapons;
    
    int currentWeapon = 0;

    public event System.EventHandler<WeaponEvent> OnFireWeapon;
    public event System.EventHandler<WeaponEvent> OnSwitchWeapon;


    bool weaponBusy = false;

    private void Awake() {
        //disable our other weapons
        for (int i = 0; i < weapons.Count; i++) {
            weapons[i].gameObject.SetActive(i == currentWeapon);
            holsteredWeapons[i].SetActive(i != currentWeapon);
        }
        cc = GetComponent<CharacterController>();
       // line = GetComponent<LineRenderer>();
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
        aim = Look();

        //todo: perhaps have this per weapon, and use to to figure out what to shoot
        //line.SetPosition(0, weapons[currentWeapon].transform.position);
        //line.SetPosition(1, aim);


        //attack by clicking
        if (Input.GetMouseButtonDown(0) && !weaponBusy) {

            if (weapons[currentWeapon].CanAttack()) {
                //attack!
                WeaponEvent we = weapons[currentWeapon].Attack();

                OnFireWeapon?.Invoke(this.gameObject, we);
                GetComponent<AudioSource>().PlayOneShot(weapons[currentWeapon].GetWeaponData().attackSound);

            }
            else {
                //notify the player, perhaps play a click sound if it's a gun?

            }
        }
    }

    private void OnAnimatorIK(int layerIndex) {
        //anim.SetLookAtWeight(1,0.25f,0.9f,1.0f, 1.0f);
        //anim.SetLookAtPosition(aim);
    }

    void SelectWeapon() {

        if (weaponBusy) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) && currentWeapon != 0) {
            StartCoroutine(SwitchWeapon(0));
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && currentWeapon != 1) {
            StartCoroutine(SwitchWeapon(1));
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && currentWeapon != 2) {
        //    SwitchWeapon(2);
        }
    }

    IEnumerator SwitchWeapon(int idx) {
        weaponBusy = true;

        OnSwitchWeapon?.Invoke(this.transform, new WeaponEvent() { thisWeapon = weapons[idx] });

        //if we switched from the rifle
        if (currentWeapon == 0) {
            anim.SetTrigger("draw_pistol");

        }
        //we switched from the pistol
        else {
            anim.SetTrigger("draw_rifle");
        }

        //both weapons take the same time to draw and put away
        float dt = 0.5f;
        yield return new WaitForSeconds(dt);

        weapons[currentWeapon].gameObject.SetActive(false);
        holsteredWeapons[currentWeapon].SetActive(true);

        currentWeapon = idx;

        yield return new WaitForSeconds(dt);

        weapons[currentWeapon].gameObject.SetActive(true);
        weapons[currentWeapon].Equip();
        holsteredWeapons[currentWeapon].SetActive(false);
        

        weaponBusy = false;

        yield return null;
    }


    void Movement() {
        //moving with the keyboard
        Vector3 motion = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        Vector3 facing = Quaternion.Inverse(transform.rotation) * motion;

        float factor = 1.0f;
        if (facing.z < - 0.4f) {
            factor = factor / 2f;
        }

        cc.SimpleMove(motion * moveSpeed * factor);


        //rotate motion to match forward direction for animator
        anim.SetFloat("vx", facing.x);
        anim.SetFloat("vz", facing.z);


    }

    Vector3 Look() {
        //aiming with the mouse
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) {
            lookTarget = hitInfo.point;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget, Vector3.up);
            //lift the hit point up 1m to the center of mass
            Vector3 point = hitInfo.point;
            point.y = 1f;
            return point;

        } else {
            return transform.position + transform.forward;
        }
    }

}
