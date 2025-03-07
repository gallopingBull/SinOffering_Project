﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// command for melee attack that's invoked by InputHandler.cs. implements melee attack behavior.
/// </summary>

public class MeleeCommand : Command 
{
    [SerializeField] float _attackRange = 1;
    
    public Transform[] AttackPoints;
    public LayerMask enemyLayer;
    public List<Collider> hitEnemies = new List<Collider>();

    public override void Execute() => MeleeAttack();

    public override void Redo()
    {
    }

    private void LateUpdate()
    {
        if (_pc.state == Entity.State.meleeing)
        {
            if (_pc.dir != 1)
            {
                Vector3 tmpPos = _pc.MeleeSprite.gameObject.transform.localPosition;
                Vector3 tmpRot = _pc.MeleeSprite.gameObject.transform.eulerAngles;
                tmpPos.x *= -1;
                tmpRot.z *= -1;
                _pc.MeleeSprite.gameObject.transform.localPosition = tmpPos;
                _pc.MeleeSprite.gameObject.transform.eulerAngles = tmpRot;
            }
        }
    }

    public void MeleeAttack()
    {
        // return if reached max attack count
        if (_pc.IsGrounded) //check if not in state jump state
        {
            _pc.StateManager.EnterState(Entity.State.meleeing);
    
            for (int i = 0; i < AttackPoints.Length; i++)
                hitEnemies = Physics.OverlapSphere(AttackPoints[i].position, _attackRange, enemyLayer).ToList<Collider>();

            if (hitEnemies != null)
            {
                for (int i = 0; i < hitEnemies.Count; i++)
                {
                    Debug.Log("hitemeies["+i+"] - " + "enemy name: " + hitEnemies[i].gameObject.name);
                    if (!hitEnemies[i].gameObject.GetComponent<EnemyController>().dying)
                    {
                        //hitEnemies[i].gameObject.GetComponent<EnemyController>().Damage(.5f);
                        hitEnemies[i].gameObject.GetComponent<EnemyController>().Damaged(10f);
                    }       
                }
            }
            hitEnemies.Clear();
            return;
        }

        // melee from falling state
        if (_pc.state == Entity.State.falling || _pc.state == Entity.State.Jumping)
        {
        
        }
    }

    private void OnDrawGizmos()
    {
        if (AttackPoints[0] == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackPoints[0].position, _attackRange);
        Gizmos.DrawWireSphere(AttackPoints[1].position, _attackRange);
    }
}
