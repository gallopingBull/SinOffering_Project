﻿using UnityEngine;

public class Weapon_DesetEagle : Weapon {

    protected override void SpawnProjectile(int dir)
    {
        GameObject tmpProjectile;
        tmpProjectile =
                    Instantiate(ProjectilePrefab,
                        spawnLoc.transform.position,
                        spawnLoc.transform.rotation);
        tmpProjectile.GetComponent<Projectile>().FireProjectile(dir);
    }
}
