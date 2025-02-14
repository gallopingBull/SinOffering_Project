﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// command for dash attack that's invoked by InputHandler.cs. implements melee attack behavior.
/// </summary>

public class DashCommand : Command
{
    #region variables
    private PlayerController pc; 
    [HideInInspector]
    public enum DashState { init, inDashAttack, completed, disabled };
    //[HideInInspector]
    public DashState dashState;

    public RaycastHit2D[] m_Hits;
    public List<GameObject> targets;

    public float DashSpeed = 100f;  
    public float ShakeDur = .3f;
    public float ShakeAmmount = 07f;
    public GameObject dashLoc;
    public int MaxDashCount = 3;


    public float DashManaCost = 25f;
    public float dashButtonHeldTime;
    [SerializeField]
    private float DashButtonHeldTimeMAX = .033f; //.02

    //[HideInInspector]
    public int dashCount;
    public Image ManaUI;

    [HideInInspector]
    public bool EnableDashCommand;
    [HideInInspector]
    public bool isDashing = false;
    [HideInInspector]
    public bool dashAttack = false;

    private bool InCooldown;
    public bool isValid;
    public bool dashObstructed = false;

    public float CooldownDelay = 5; // short delay before cooldown begins
    public float CooldownScale = .001f; // time scale/rate that mana charges at

    //private PlayerController pc; //reference to main Player Controller
    private Transform tmpPos; //store last position before initializing dash

    [HideInInspector]
    public GameObject RadialMenu;
    [HideInInspector]
    public Image RadialCounterBar;

    private float radialCounterValue = 0;
    private float radialCounterMulti = .0005f; //.05f;
    //[HideInInspector]
    public LineRenderer lr_DashTrajectory;
    public LineRenderer lr_DashAttack;
    public GameObject dashDestinationImage;

    private bool m_HitDetect;

    [SerializeField]
    private Collider[] m_Colliders;

    [SerializeField]
    private Collider2D[] m_Colliders2D;

    private Vector3 direction;
    private Vector3 origin;

    [SerializeField]
    private float circleCastRadius = .5f;
    [SerializeField]
    private float circleCastDashRadius = .5f;
    [SerializeField]
    private Vector3 BoxCastSize;
    private float curHitDistance;
    [Tooltip("original set to 4, testing with 7")]
    public float MaxHitDistance = 10; //original set to 4, testing with 7
    [Tooltip("this is for how close the player's dash dintination is to a wall/floor")]
    public float MaxDashDistanceDiff = 1.5f; 
    [SerializeField]
    public GameObject enemyTargetMarkers;

    public LayerMask layerMask;
    private PostProcessManager ppm;

    private bool cr_active = false; 
    #endregion

    #region functions
    public override void Execute() {
        
        if (pc.Mana >= DashManaCost)
            EnableDashCommand = true;
        else
            EnableDashCommand = false;
    }

    public override void Redo() { }

    private void Awake()
    {
        targets = new List<GameObject>();
        pc = GetComponent<PlayerController>();
        //******\\
        // this is for dash. move back dashcomman afterwards
        RadialMenu = GameObject.Find("Radial Dash");
        RadialCounterBar = GameObject.Find("Radialbar").GetComponent<Image>();
        //******\\
        dashDestinationImage.SetActive(false);
        ManaUI = GameObject.Find("ManaBar_Fill").GetComponent<Image>();
    }

    protected override void Start()
    {
        base.Start();
        ppm = PostProcessManager.instance;
        dashState = DashState.completed;
        RadialMenu.SetActive(false);
    }

    private void Update()
    {
        // move this to HUDManager
        if (InCooldown)
        {
            if (ManaUI.fillAmount >= 0)
            {
                ManaUI.fillAmount = Mathf.Lerp(ManaUI.fillAmount, 200, Time.deltaTime * CooldownScale);
                if (pc.Mana < 200)
                    pc.Mana = ManaUI.fillAmount * 200;
            }
            if (ManaUI.fillAmount == 1 && pc.Mana == 200)
            {
                InCooldown = false;
                cr_active = false; 
                StopAllCoroutines();
            }      
        }

        
        if (Input.GetAxis("LeftTrigger") == 0 &&
              !GameManager.Instance.GameCompleted)
        {
            //print("dash completed and left trigger released");
            if (dashState == DashState.completed && ppm.ppEnabled == true)
                ppm.ExitInitDash(.75f, true);

            if (dashState != DashState.completed &&
                dashButtonHeldTime > DashButtonHeldTimeMAX)
            {
                //cheeck if this condition is needed
                if (dashButtonHeldTime < DashButtonHeldTimeMAX)
                {
                    print("calling exit InitDash");
                    ppm.ExitInitDash(.75f, true);
                    DisableDashAttack();
                    return;
                }

                if (Time.timeScale != 1)
                {
                    Time.timeScale = 1; 
                    TimeScale.DisableSlomo();
                }

                dashButtonHeldTime = 0;

                // reset radialCounterValue after releasing dash button
                if (radialCounterValue != 0 && RadialMenu.activeSelf)
                {
                    radialCounterValue = 0;
                    RadialCounterBar.fillAmount = radialCounterValue;
                }

                pc.StateManager.ExitState(Entity.State.dashing);
                dashState = DashState.completed;

                if (dashState != DashState.inDashAttack && !EnableDashCommand)
                {
                    ppm.ExitInitDash(.75f, true);
                    DisableDashAttack();
                }
            }
        }

        if (EnableDashCommand)
        {
            //print("DashCommand.cs -> Update() at: " + Time.realtimeSinceStartup);
            if (Input.GetAxis("LeftTrigger") == 1)
            {
                //print("holding left trigger");
                if (dashState == DashState.inDashAttack)
                    return;
                if (pc.Mana < DashManaCost)
                {
                    print("max cound reached - LEftTrigger being held");
                    DisableDashAttack();
                    return;
                }

                if (dashState != DashState.inDashAttack && pc.state != Entity.State.dashing)
                {
                    dashButtonHeldTime += .01f;
                    if (dashButtonHeldTime > DashButtonHeldTimeMAX)
                    {
                        if (pc.Mana >= DashManaCost)
                            InitDash();

                        if (radialCounterValue < 1 && RadialMenu.activeSelf)
                        {
                            CalculateDashTrajectory();
                            radialCounterValue += radialCounterMulti;
                            RadialCounterBar.fillAmount = radialCounterValue;
                        }

                        // if dash attack button is held too long
                        // activate dash attack
                        if (radialCounterValue >= 1)
                        {
                            if (dashState != DashState.inDashAttack)
                            {
                                radialCounterValue = 0f;
                                RadialCounterBar.fillAmount = radialCounterValue;

                                if (pc.Mana > DashManaCost && RadialMenu.activeInHierarchy)
                                {
                                    if (Time.timeScale != 1)
                                    {
                                        Time.timeScale = 1;
                                        TimeScale.DisableSlomo();
                                    }

                                    dashDestinationImage.SetActive(false);

                                    pc.rb.velocity = Vector3.zero;
                                    if (targets != null)
                                        dashAttack = true;

                                    Dash();
                                    RadialMenu.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }

            if (Input.GetButtonDown("Melee") && isValid
                && !GameManager.Instance.GameCompleted && 
                (direction.x != 0 || direction.y != 0) && 
                pc.inputHandler.dashDelayComplete)
            {
                //print(dashState);
                if (dashState == DashState.init)
                {
                    //print("In DashCommand.cs -> Update() -> Right Triggr Pressed");
                    //print("origin: " + origin + "; origin + (direction.normalized * curHitDistance): " + (origin + (direction.normalized * curHitDistance)));
                    if (Time.timeScale != 1)
                    {
                        Time.timeScale = 1;
                        TimeScale.DisableSlomo();
                    }

                    lr_DashTrajectory.gameObject.SetActive(false);
                    lr_DashAttack.gameObject.SetActive(false);
                    dashDestinationImage.SetActive(false);

                    if (targets.Count > 0)
                        dashAttack = true;
                    
                    Dash();
                    dashButtonHeldTime = 0;

                    // reset radialCounterValue after releasing dash button
                    if (radialCounterValue != 0 && RadialMenu.activeSelf)
                    {
                        radialCounterValue = 0;
                        RadialCounterBar.fillAmount = radialCounterValue;
                    }
                }
            }
        }
        else
        {
            if (pc.Mana < DashManaCost && Time.timeScale != 1)
            {
                DisableDashAttack();
                ppm.ExitInitDash(.75f, true);
                print("max count reached");
            }
        }
    }

    // called when dash trigger is held down
    public void InitDash()
    {
        if (!pc.dying && dashState != DashState.init)
        {
            // slow down time
            if (Time.timeScale != .1)
            {
                Time.timeScale = .1f;
                TimeScale.EnableSlomo();
            }
            ppm.InitDash(); // darken screen or some other sfx to emphasize player
            dashState = DashState.init;

            GetComponent<Entity>().rb.useGravity = false;
            GetComponent<Entity>().rb.velocity = Vector3.zero;

            if (pc.transform != tmpPos)
                tmpPos = pc.transform;
            else
                pc.transform.position = tmpPos.position;

            // bring up dash wheel and line trajectory
            if (RadialMenu != null)
                RadialMenu.SetActive(true);
            lr_DashTrajectory.gameObject.SetActive(true);
            lr_DashAttack.gameObject.SetActive(true);
        }
    }

    private void Dash()
    {
        RadialMenu.SetActive(false);

        if (InCooldown == true)
            InCooldown = false;

        if (lr_DashTrajectory.gameObject.activeSelf)
        {
            lr_DashTrajectory.gameObject.SetActive(false);
            lr_DashAttack.gameObject.SetActive(false);
        }

        pc.inputHandler.dashDelay = pc.inputHandler.MAXDashDelay;

        if (dashState != DashState.inDashAttack && pc.Mana >= DashManaCost)
        {
            dashCount++;
            DashUI(false);
            if(!cr_active)
                StartCoroutine(AutoCoolDown());
            StartCoroutine(Dashing());
        }
    }

    private void DashToLocation()
    {
        if (GetDashDirection().x != 0 || GetDashDirection().y != 0)
            pc.transform.position = origin + (direction.normalized * curHitDistance);
        else
        {
            Vector3 tmpVel;
            if (pc.facingLeft)
                tmpVel = new Vector3(-1 * DashSpeed, transform.position.y, transform.position.z);
            else
                tmpVel = new Vector3(1, transform.position.y, transform.position.z);

            transform.position = origin + (direction.normalized * curHitDistance);
        }
    }

    private IEnumerator Dashing()
    {
        dashState = DashState.inDashAttack;
        pc.StateManager.EnterState(Entity.State.dashing);

        if (targets.Count > 0)
        {
            FreezeEnemies(targets); //freeze enemies that will be dash killed
            yield return new WaitForSeconds(.5f);
        }

        ppm.OnDash(.75f, 5f, true);
        DashToLocation();
        
        CameraShake.instance.Shake(ShakeDur, ShakeAmmount, true);

        yield return new WaitForSeconds(.25f);

        if (Time.timeScale != .1)
        {
            Time.timeScale = .1f;
            TimeScale.EnableSlomo();
        }

        if (targets.Count > 0 && dashAttack)    
        {
            var enemyTotal = targets.Count;
    
            Vector3 tmpDir = (direction.normalized * curHitDistance);
            Vector3 tmpEnd = origin + (direction.normalized * curHitDistance);

            GetComponent<SpriteCutter>().Slice(origin, tmpEnd, layerMask, targets, tmpDir);
            
            DestroyEnemies();
            yield return new WaitForSeconds(enemyTotal * .025f); // adds a small delay before ending slomo cinematic dash attack
                                                                    // delay is scaled by how many enemies are dash killed (.015-.0025 )
        }

        if (!InCooldown && pc.Mana < DashManaCost)
        {
            if (!cr_active)
            {
                InCooldown = true;
                StartCoroutine(CoolDown());
            }
        }

        yield return new WaitForSeconds(.01f); //delay before dash attack finishes
        DisableDashAttack();

        pc.StateManager.ExitState(Entity.State.dashing);
        dashState = DashState.completed;

        StopCoroutine(Dashing());
    }
    
    public void DisableCollisions()
    {
        foreach (Collider collider in m_Colliders)
            collider.enabled = false;
        foreach (Collider2D collider in m_Colliders2D)
            collider.enabled = false;
    }

    public void EnableCollisions()
    {
        foreach (Collider collider in m_Colliders)
            collider.enabled = true;
        foreach (Collider2D collider in m_Colliders2D)
            collider.enabled = true;
    }
    
    private Vector3 GetDashDirection()
    {
        float xRaw = pc.inputHandler.L_xRaw;
        float yRaw = pc.inputHandler.L_yRaw;
        return new Vector3(xRaw, yRaw, 0);
    }

    public void CalculateDashAttackTargets(Vector2 _origin, Vector2 _dir, float _hitDistance)
    {   
        List<GameObject> gameObjectsToCut = new List<GameObject>();
        // Remove any 'X' on enemies that have been destroyed
        if (targets != null)
            RemoveDashTags();

        RaycastHit2D[] hits = Physics2D.RaycastAll(_origin,  _dir,_hitDistance, layerMask);
        foreach (RaycastHit2D hit in hits)
        {
            GameObject tmp = hit.transform.gameObject;
            if (tmp.tag == "Enemy")
            {
                if (targets == null && !tmp.GetComponent<EnemyController>().dying)
                {
                    targets.Add(tmp);
                    break;
                }
                if (tmp != null && !tmp.transform.parent.GetComponent<EnemyController>().dying)
                {
                    if (targets.Contains(tmp))
                        continue;

                    else
                    {
                        Instantiate(enemyTargetMarkers,
                            tmp.transform.parent.transform.position,
                            tmp.transform.parent.transform.rotation,
                            tmp.transform.parent.transform);
                        targets.Add(tmp.transform.parent.gameObject);
                    }
                }

                // maybe delete this
                if (hit.distance > MaxDashDistanceDiff)
                    dashObstructed = false;
            }    
        }
    }

    public void CalculateDashTrajectory()
    {
        isValid = false;
        origin = new Vector3(pc.transform.position.x,
            pc.transform.position.y + .1f,
            pc.transform.position.z);
        direction = GetDashDirection();
        curHitDistance = MaxHitDistance;

        m_Hits = Physics2D.BoxCastAll(origin, BoxCastSize, 0,direction, MaxHitDistance, layerMask);
        
        foreach (RaycastHit2D hit in m_Hits)
        {
            GameObject tmp = hit.transform.gameObject;
          
            if ((hit.transform.gameObject.tag == "Floor" || hit.transform.gameObject.tag == "Wall"))
            {
                if (hit.distance ==0)
                    dashObstructed = false;
                if (hit.distance < MaxDashDistanceDiff)
                    dashObstructed = true; //print("TOO CLOSE TO WALL - DASH FAILED");
                else
                    dashObstructed = false; //print("far enough from  wall/ground to dash");
                curHitDistance = hit.distance;
                if (curHitDistance == 0)
                    print("shit");
                else
                    break;
            }
        }
   
        if (m_Hits.Length > 0)
        {
            if (!isValid && !dashObstructed)
                isValid = true;
        }

        if (m_Hits.Length == 0)
        {
            dashObstructed = false;
            if (!isValid && !dashObstructed)
                isValid = true;
        }

        lr_DashTrajectory.SetPosition(0, origin);
        lr_DashAttack.SetPosition(0, origin);

        lr_DashTrajectory.SetPosition(1, origin + ((direction.normalized * curHitDistance)*.95f)); //.9-.95 tp scale the size of the "outer rim" of the dash trajectory 
                                                                                                  //so its slightly shorter than the lr_dashAttack

        lr_DashAttack.SetPosition(1, origin + (direction.normalized * curHitDistance)*1.11f);
        DashTrajectoryMarker(origin + (direction.normalized * curHitDistance) * 1.11f);

        CalculateDashAttackTargets(origin, direction, curHitDistance);
    }

    private void DashTrajectoryMarker(Vector3 hitLoc)
    {
        dashDestinationImage.transform.position = hitLoc;

        if (!dashDestinationImage.activeSelf &&
            pc.Mana >= DashManaCost && 
            dashState == DashState.init)
            dashDestinationImage.SetActive(true);

        if (direction.x < -0.01f)
            dashDestinationImage.GetComponent<SpriteRenderer>().flipX = true;
        else
            dashDestinationImage.GetComponent<SpriteRenderer>().flipX = false;
    }

    private void OnDrawGizmos()
    {
        // for dash trajectory
        if (dashObstructed)
        {
            Gizmos.color = Color.red;

            Debug.DrawRay(origin, direction.normalized * curHitDistance, Color.red);
            Gizmos.DrawWireCube(origin + (direction.normalized * curHitDistance), BoxCastSize);
        }
        else
        {
            Gizmos.color = Color.green;

            Debug.DrawRay(origin, direction.normalized * curHitDistance, Color.green);
            Gizmos.DrawWireCube(origin + (direction.normalized * curHitDistance), BoxCastSize);
        }

        // for dash attack
        Gizmos.color = Color.yellow;
        Debug.DrawRay(origin, direction.normalized * curHitDistance, Color.yellow);
        Gizmos.DrawWireSphere(origin + (direction.normalized * curHitDistance), circleCastRadius);
    }

    //** look into why this is using PC.yRaw and not yraw value from inputhandler **\\
    public void ChangeRigidbodyValues()
    {
        if (pc.yRaw < 0)
        {   
            //print("dashing down while falling");
            //print("1st cond. || player drag: 1000 || player state: " + GetComponent<PlayerController>().state);
            if (pc.state == Entity.State.falling || pc.state == Entity.State.Jumping)
            { 
                pc.rb.mass = .01f;
                pc.rb.angularDrag = 100f;
                pc.rb.drag = 1000f;
            }
        }
        //print("2nd cond. || player drag: 15 || player state: " + GetComponent<PlayerController>().state);
        if (pc.yRaw < .001f && pc.state != Entity.State.running)
            pc.rb.drag = 30;
        //print("3rd cond. || player drag: 30 || player state: " + GetComponent<PlayerController>().state);
        else
            pc.rb.drag = 30;
    }

    // displays and changes dash counter in UI
    private void DashUI(bool coolingDown)
    {
        if (!coolingDown)
        {
            if (ManaUI.fillAmount > 0)
            {
                pc.Mana -= 25f;
                //GameEvents.OnManaUpdateEvent();
                ManaUI.fillAmount -= DashManaCost/200;
            }
        }
    }

    private IEnumerator AutoCoolDown()
    {
        //print("auto cooldown Called");
        cr_active = true;
        
        yield return new WaitForSeconds(CooldownDelay * 2f);
        
        if (!InCooldown && pc.Mana < 200 && pc.state != Entity.State.dashing)
        {
            print("auto cooldown - InCoolDown = true");
            InCooldown = true;
            StartCoroutine(CoolDown());
        }

        //print("StopCoroutine(AutoCoolDown())");
        cr_active = false;
        StopCoroutine(AutoCoolDown());
    }

    private IEnumerator CoolDown()
    {
        print("cooldown Called");
        cr_active = true; 
        StopCoroutine(AutoCoolDown());
        yield return new WaitForSeconds(CooldownDelay * 2);

        dashCount = 0;
        DashUI(true);
        InCooldown = false;
        cr_active = false;

        StopCoroutine(CoolDown());
    }

    private void FreezeEnemies(List<GameObject> dashTargets)
    {
        // stop movement for all dash attack targets
        foreach (GameObject target in dashTargets)
        {
            if (target != null)
            {
                target.GetComponent<EnemyController>().rb.velocity = Vector3.zero;
                target.GetComponent<EnemyController>().rb.isKinematic = true;
                target.GetComponent<EnemyController>().rb.useGravity = false;
                target.GetComponentInChildren<Rigidbody2D>().velocity = Vector2.zero;
                target.GetComponentInChildren<Rigidbody2D>().isKinematic = true;
                target.GetComponentInChildren<Rigidbody2D>().gravityScale = 0;
                target.GetComponent<EnemyController>().CanMove = false;
            }
        }
    }
    
    // remove tags on enemies that were marked for a dash attack
    private void RemoveDashTags()
    {
        foreach (GameObject target in targets)
        {
            if (target != null)
                Destroy(target.transform.Find("X(Clone)").gameObject);
        }
        targets.Clear();
    }

    private void DestroyEnemies()
    {
        List<GameObject> tmpTargetList = targets;

        if (tmpTargetList.Count > 0)
        {
            #region testing / want slice here
            // adds a small delay before ending slomo cinematic dash attack
            // delay is scaled by how many enemies are dash killed (.015-.0025 )

            //GetComponent<ISlice>().Slice(origin,
            //origin + (direction.normalized * curHitDistance), layerMask);
            //yield return new WaitForSeconds(tmpTargetList.Count * .025f);
            #endregion

            for (int i = 0; i < tmpTargetList.Count; i++)
            {
                if (tmpTargetList[i] != null)
                { 
                    //RemoveEnemyFromLists(target); //testing this location
                    //LoopUpdate(tmpTargetList, i, false); //prints elements in list for debuggin purposes
                    //print("i: " + i);
                    if (tmpTargetList.Count == 0)
                        break;
                    else
                    {
                        //print("Calling DashKilled() on this enemy target: " + targets[i].name);
                        if (tmpTargetList[i] == null)
                            break;
                        tmpTargetList[i].GetComponent<EnemyController>().DashKilled();
                    }
                }
            }
        }
        dashAttack = false;
        targets.Clear();
    }
    
    public void DisableDashAttack()
    {
        if (!EnableDashCommand)
        {
            RadialMenu.SetActive(false);
            dashDestinationImage.SetActive(false);
            lr_DashTrajectory.gameObject.SetActive(false);
            lr_DashAttack.gameObject.SetActive(false);
            RemoveDashTags();
            isValid = false;
            dashButtonHeldTime = 0;
            Time.timeScale = 1;
            TimeScale.DisableSlomo();
        }
    }
    #endregion
}
