﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvadeCommand : Command {
    
    
    [SerializeField]
    [Tooltip("Keep at 300 to move four units.")]
    private float evadeSpeed = 100;

    [SerializeField]
    private int EvadeCountMAX = 2;
    //[HideInInspector]
    public int EvadeCount = 0;
    [HideInInspector]
    public int AirEvadeCount = 0;

    [HideInInspector]
    public bool FullyUpgraded = true;
    [HideInInspector]
    public bool AirEvadeUpgraded = true;

    //colliser specific for evade action
    //only interacts with walls/floors/platforms and
    //goes through enemies
    private GameObject evadeCollider; 
    

    /*
    private bool evadeObstructed = false; 
    private Vector3 origin;
    private Vector3 direction;
    public float MaxHitDistance = .25f;
    private float curHitDistance;
    public LayerMask layerMask;
    private RaycastHit2D[] hits;
    */
    
    public override void Execute() { Evade(); }
    public override void Redo()
    {

    }
    private void Awake()
    {
        _pc = GetComponent<PlayerController>();
        evadeCollider = GameObject.Find("TmpCollider");
    }

    public void Evade()
    {
        if (EvadeCount == EvadeCountMAX)
            return;
        
        if (!_pc.IsGrounded && AirEvadeUpgraded)
        {
            if (AirEvadeCount == 1)
                return;
            EvadeCount = AirEvadeCount++;
        }

        //pc.rb.velocity = Vector3.zero; 
        
        _pc.StateManager.EnterState(Entity.State.evading);
        EvadeCount++;
        evadeCollider.transform.position = _pc.transform.position;
        evadeCollider.transform.parent = _pc.transform;
            
        _pc.rb.velocity = new Vector3(-_pc.dir * evadeSpeed, 0, 0);
        

        StartCoroutine("EvadeComplete");


        #region old, might delete
        /*
        evadeObstructed = false;

        origin = pc.transform.position; 
        direction = Vector3.right * -pc.dir;
        curHitDistance = MaxHitDistance;

        hits = Physics2D.RaycastAll(origin,
        direction,
        MaxHitDistance,
        layerMask);

        Vector3 Pos = new Vector3();

        foreach (RaycastHit2D hit in hits)
        {
            GameObject tmp = hit.transform.gameObject;
            if (tmp.tag == "Wall" ||
               tmp.tag == "Floor")
            {
                evadeObstructed = true;
                print("hitting walls/floor");
                print("distance: " + hit.distance);
                curHitDistance = hit.distance;
                Pos = hit.point;
                break;
            }
         
        }

        Vector3 tmpDir = (Pos - pc.transform.position.normalized * evadeSpeed);

        pc.sm.EnterState(Entity.State.evading);

        EvadeCount++;
        evadeCollider.transform.position = pc.transform.position;
        evadeCollider.transform.parent = pc.transform;

        //pc.rb.velocity = new Vector3(-pc.dir * evadeSpeed, 0, 0);
        pc.rb.velocity = new Vector3(tmpDir.x, 0, 0);
        StartCoroutine("EvadeComplete");
        */
        #endregion


    }

    private IEnumerator EvadeComplete()
    {
        yield return new WaitForSeconds(.3f);
       
        _pc.StateManager.ExitState(Entity.State.evading);

        evadeCollider.transform.parent = null;
        evadeCollider.transform.position = new Vector3(100, 100, 100); // moving collider away to distant location when not in use

        if (!_pc.IsGrounded)
            _pc.StateManager.EnterState(Entity.State.falling);

        StopCoroutine("EvadeComplete");
    }

  
    private void OnDrawGizmos()
    {
        //for dash trajectory

        /*
        if (evadeObstructed)
        {
            Gizmos.color = Color.red;

            Debug.DrawRay(origin, direction.normalized * curHitDistance, Color.red);
            Gizmos.DrawSphere(origin + (direction.normalized * curHitDistance), .5f);
            //Gizmos.DrawWireSphere(origin + (direction.normalized * curHitDistance), circleCastDashRadius);
        }
        else
        {
            Gizmos.color = Color.green;

            Debug.DrawRay(origin, direction.normalized * curHitDistance, Color.green);
            //Gizmos.DrawSphere(origin + (direction.normalized * curHitDistance), 1f);
            Gizmos.DrawSphere(origin + (direction.normalized * curHitDistance), .5f);
        }
        */
    }
}
