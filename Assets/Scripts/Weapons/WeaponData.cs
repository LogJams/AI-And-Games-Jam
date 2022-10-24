using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/WeaponData", order = 1)]
public class WeaponData : ScriptableObject {

    public float soundRadius = 50;
    public int damage = 5;
    public float cooldown = 10f;

    public Sprite icon;
    public AudioClip attackSound;
}
