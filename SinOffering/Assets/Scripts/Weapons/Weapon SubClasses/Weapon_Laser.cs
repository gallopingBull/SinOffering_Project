﻿using UnityEngine;

public class Weapon_Laser : Weapon {

    public float JumpSpeedModifier = 1;
    public float RunSpeedModifier = 1;

    public bool LaserEnabled;

    public LineRenderer lr;
    private int direction; 

    public override void InitWeapon()
    {
        FlipWeaponSprite(pc.dir);
        ModifyEntitySpeed(JumpSpeedModifier, RunSpeedModifier);
    }
    public override void FireWeapon()
    {
        //if (spawnLoc != GetMuzzleDirection())
            //spawnLoc = GetMuzzleDirection();

        canFire = true;
        if (!LaserEnabled)
            LaserEnabled = true;

        SoundManager.PlaySound(fireClip); // move this into weapon subclasses for more specific behavior
        SpawnProjectile(pc.dir);
        Recoil.WeaponRecoil();
        CameraShake.instance.Shake(DurationCamShake,
                     AmmountCamShake, SmoothTransition);
    }

    protected override void MoveWeaponToSocket()
    {
        //print("moce to socket called in weapoin_laser");
        //lr.transform.position = GetMuzzleDirection().transform.position;
    }

    protected override void FixedUpdate()
    {
        FireRateCheck();
        if (Input.GetButtonUp("Fire1"))
            ReleaseTrigger();
    }

    public override void ReleaseTrigger()
    {
        LaserEnabled = false;
        canFire = false;
        lr.gameObject.SetActive(false);
    }

    protected override void SpawnProjectile(int dir)
    {
        lr.gameObject.SetActive(true);
        LaserEnabled = true;
    }
}
