using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;



public class aiNAV : MonoBehaviour
{
    [Header("References")] //References for use by the script
    [SerializeField] private Transform playerTransform; //the playerCapsule is used for the playerTransform
    [SerializeField] private NavMeshAgent navAgent; //navigation Agent module for easier scripting
    [SerializeField] private Animator animator; 
    [SerializeField] private GameObject eyes;
    [SerializeField] private Transform playerEyes;
    [SerializeField] private Transform playerBody;
    [SerializeField] private MeshCollider ownMeshCollider;
    [SerializeField] private CapsuleCollider ownCapsuleCollider;
    [SerializeField] private GameObject ragdollObjectPrefab;
    [SerializeField] private GameObject ownBody;
    [SerializeField] private GameObject ownFeet;
    [SerializeField] private GameObject ownEnemyTag;
    [SerializeField] private GameObject deathCube;
    [SerializeField] private GameObject respawnPoint;
     [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer; //layerMask for the Ground to determine walkable checks
    [SerializeField] private LayerMask playerLayerMask; //layerMask for the Player to determine where the player is
    [SerializeField] private LayerMask obstacleLayerMask; 
    [SerializeField] private LayerMask hittableLayerMask;
    [SerializeField] private LayerMask trainLayerMask;
    [SerializeField] private LayerMask glassLayerMask;
    [SerializeField] private LayerMask validPlayerMask;
    [SerializeField] private LayerMask enemyTagMask;
    [SerializeField] private LayerMask respawnLayerMask;
    [Header("ShootEffects")]
    [SerializeField] private GameObject obstacleHitEffectPrefab;
    [SerializeField] private GameObject enemyHitEffectPrefab;
    [SerializeField] private GameObject groundHitEffectPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject muzzleLightEffectPrefab;
    [SerializeField] private GameObject obstacleHitEffectPrefabShotgun;
    [SerializeField] private GameObject glassHitEffectPrefab;
    [Header("ShootMuzzleSmokeLocation")]
    [SerializeField] private Transform muzzlePoint;

    [Header("Sounds")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private AudioClip hitmarkerSound;
    [SerializeField] private AudioClip deathJingle;
    [SerializeField]private AudioSource audioSource;
    [SerializeField] private AudioSource deathJingleSource;


    [Header("Patrol Settings")] //customizable setting of the patrol radius
    [SerializeField] private float patrolRadius; //the patrol radius
    private Vector3 currentPatrolPoint; //variable storing 3D coordinates of the current Patrol point
    private Vector3 strafePoint;
    private bool hasPatrolPoint;  
    private Vector3 currentPosition;
    

    [Header("Combat Settings")] //customizable settings of the bullet and attack speed using rigidbody physics
    [SerializeField] private float attackCooldown = 2f;
    private int randomStrafe;
    [SerializeField] private float npcHP = 50f;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float hitTimer = 2f;
    [SerializeField] private float patrolPointTimer = 2.5f;
    [SerializeField] private float staggeredSpeed = 1f;
    [SerializeField] private float followPlayerTimer = 1f;
    [SerializeField] private int maxRevolverAmmoCount = 50;
    private int revolverAmmoCount;
    private int ogRevolverAmmoCount;
    [SerializeField] private float currentSpread = 1f;
    [SerializeField] private float ragdollTimer = 5f;
    [SerializeField] private float weaponDrawSpeed = 1f;
    [SerializeField] private float chaseTimer = 5f;
    [SerializeField] private float revolverDamage = 50f;
    [SerializeField] private float shotgunDamage = 10f;
    [SerializeField] private float rifleDamage = 100f;
    [SerializeField] private float headshotModifier = 1f;


    [Header("Detection Ranges")] //customizable settings for the vision (follow player) and engagement (attack) range
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float engagementRange = 6f;
    [SerializeField] private float tooCloseRange = 3f;
    [SerializeField] private string currentWeapon;
    [SerializeField] private float respawnRadius = 5f;



    //Declaration of bools for behavior states
    private bool isPlayerVisible = false;
    private bool isPlayerInRange = false;
    private bool isMoving;
    private bool wasHit;
    private bool isOnAttackCooldown;
    private bool isOnStrafePoint;
    private bool fightMode;
    private bool patrolTimerFinished = true;
    private bool releaseChase = true;
    private bool keepChasing;
    private bool attackedPlayer;
    private bool isReloading;
    private bool isDead;
    private bool chaseStart = true;
    private bool doneCalculatingChase = true;
    private bool unStuck = true;
    private bool trainClose;
    private bool standing;
    private float maxHP;
    private bool dying;
    private int weaponRandom;
    private bool hitSound;
    private bool trainGone;
    private bool isPlayerValid;
    private int endPointIncrementation = 2;
    private float bestDistance = 1000f;
    private float actualDistance;
    private float enemyAngle;
    

    private float lastCheckTime;
    private Vector3 lastCheckPos;
    private float xSeconds = 3f;
    private float yMuch = 1.0f;
    //Ray for checking collisions between the NPC and the Player
    private Ray npcToPlayerEyesRay;
    private Ray npcToPlayerBodyRay;
    private Ray raySide;
    private Ray playerValidCheckRay;  
    private Ray endPointCheckRay; 
    private Ray enemyCloseRay;
    private RaycastHit hitWall;
    private RaycastHit hitGround;
    private RaycastHit hitTrain;
    
    private Vector3 endPointValid;
    private Vector3 validEndPoint;
    private Vector3 playerLastPosition;
    private Vector3 changeInPlayerPos;
    private Vector3 lastPosition;
    private Vector3 respawnPosition;
    private Vector3 randomPosition;
    private float intervalTimer;

    private Collider enemyTag;
    private Collider[] enemyTags;
    private List<GameObject> enemyList;
    private GameObject enemy;
    private GameObject foundEnemy;
    private GameObject targetEnemy;
    private GameObject enemyEyes;
    private GameObject enemyBody;
    private GameObject targetEyes;
    private GameObject targetBody;
    private GameObject targetFeet;
     private bool hasRespawnPoint;
    private Vector3 respawnPointVector;


    //INITIALIZATION
    private void Awake(){ 
            enemyList = new List<GameObject>();
            revolverAmmoCount = maxRevolverAmmoCount; 
            GameObject playerObj = GameObject.Find("PlayerLocation");
            playerTransform = playerObj.transform;
            GameObject playerObjEyes = GameObject.Find("PlayerEyes");
            playerEyes = playerObjEyes.transform;
            GameObject playerObjBody = GameObject.Find("PlayerBody");
            playerBody = playerObjBody.transform;
            ownMeshCollider = this.GetComponent<MeshCollider>();
            ownCapsuleCollider = this.GetComponent<CapsuleCollider>();
            maxHP = npcHP;
            deathCube = GameObject.Find("DeathCubeGround");
            randomPosition = this.transform.position;
            respawnPoint = GameObject.Find("RespawnPoint");
        if(navAgent == null){
            navAgent = GetComponent<NavMeshAgent>();
        }
        animator = GetComponent<Animator>(); 

        weaponRandom = Random.Range(0, 3);
        switch(weaponRandom){
            case 1: 
            currentWeapon = "revolver";
            break;

            case 2:
            currentWeapon = "shotgun";
            break;
        }

    }

     private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, engagementRange);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
    //INITIALIZATION

    private void OnInstantiate(){
        Awake();
    }

    //MAIN FUNCTIONS
    private void Update(){
        if(!isDead){
            DetectPlayer();
            UpdateBehaviourState();
            SetAnimation();
            HealthSystem();

        } else {
            while(!dying){
                if(wasHit){
                    deathJingleSource.PlayOneShot(deathJingle);
                }
                StartCoroutine(Death());
                
            }
            
        }           

    }

    private void SetAnimation(){ 
        float velocity = navAgent.velocity.magnitude;
        if(velocity > 0.2f){
            isMoving = true;
        } else {
            isMoving = false;
        }
        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isReloading", isReloading);
    }

    void BodyHitByEnemyRevolver()
    {
        wasHit = true;
        hitSound = true;
        npcHP -=  Random.Range(revolverDamage, revolverDamage * 1.4f);
    }
     void HeadHitByEnemyRevolver()
    {
        wasHit = true;
        hitSound = true;
        npcHP -= Random.Range(revolverDamage * headshotModifier * 1.8f, revolverDamage * headshotModifier * 2.2f);
    }
    void BodyHitByEnemyShotgun()
    {
        wasHit = true;
        hitSound = true;
        npcHP -= Random.Range(shotgunDamage, shotgunDamage * 1.4f);
    }
    void HeadHitByEnemyShotgun()
    {
        wasHit = true;
        hitSound = true;
        npcHP -= Random.Range(shotgunDamage * headshotModifier * 1.8f , shotgunDamage * headshotModifier * 2.2f);
    }
    void HeadHitByEnemyRifle()
    {
        wasHit = true;
        hitSound = true;
        npcHP -= Random.Range(rifleDamage * headshotModifier * 1.8f, rifleDamage * headshotModifier * 2.2f);
    }
    void BodyHitByEnemyRifle()
    {
        wasHit = true;
        hitSound = true;
        npcHP -=  Random.Range(rifleDamage, rifleDamage * 1.4f);
    }

    private IEnumerator SlowOnHit(){
        if(wasHit){
            navAgent.speed = staggeredSpeed;
            yield return new WaitForSeconds(hitTimer);
            wasHit = false;
        } else if(fightMode){
            navAgent.speed = staggeredSpeed;
        } else {
            navAgent.speed = walkSpeed;
        }
        
    }

    private void StuckCheck(){
         if ((Time.time - lastCheckTime) > xSeconds) 
            {
                if (((this.transform.position - lastCheckPos).magnitude < yMuch) && !fightMode){
                        isPlayerVisible = false;
                        isPlayerInRange = false;
                        isMoving = false;
                        wasHit = false;
                        isOnAttackCooldown = false;
                        isOnStrafePoint = false;
                        fightMode = false;
                        patrolTimerFinished = true;
                        releaseChase = true;
                        keepChasing = false;
                        attackedPlayer = false;
                        isReloading = false;
                        isDead = false;
                        chaseStart = true;
                        doneCalculatingChase = true;
                        unStuck = true;
                        trainClose = false;
                        standing = false;
                        dying = false;
                        hitSound = false; 
                    PerformPatrol();
                }
                lastCheckPos = this.transform.position;
                lastCheckTime = Time.time;
            }
    }

    private void HealthSystem()
    {
        if(0 > npcHP){
            isDead = true;
        }
        if(!isDead){
            StartCoroutine(SlowOnHit());
        }
        
        if(hitSound){
            hitSound = false;
            deathJingleSource.PlayOneShot(hitmarkerSound);
        }
    }

    private IEnumerator Death(){
        navAgent.ResetPath();
        FindRandomNavmeshLocation();
        revolverAmmoCount = maxRevolverAmmoCount;
        GameObject ragdollObject = Instantiate(ragdollObjectPrefab, this.transform.position, this.transform.rotation);
        dying = true;
        navAgent.isStopped = true;
        animator.enabled = false;
        ownMeshCollider.enabled = false;
        ownCapsuleCollider.enabled = false;
        ownBody.SetActive(false);
        ownEnemyTag.SetActive(false);
        transform.position = deathCube.transform.position;
        yield return new WaitForSeconds(ragdollTimer);
        transform.position = respawnPoint.transform.position;
        npcHP = maxHP;
        isDead = false;
        navAgent.isStopped = false;
        Destroy(ragdollObject);
        ownBody.SetActive(true);
        ownEnemyTag.SetActive(true);
        animator.enabled = true;
        ownCapsuleCollider.enabled = false;
        ownMeshCollider.enabled = true;
        dying = false;
        hasRespawnPoint = false;
    }

    private void FindRandomNavmeshLocation(){ 
        if (!hasRespawnPoint){ 
                Vector3 potentialPoint = new Vector3(0 + Random.Range(-50f, 50f), 0 + 2f, 0 + Random.Range(-50f, 50f));
                RaycastHit hit; 
                if (Physics.Raycast(potentialPoint, Vector3.down, out hit, 2f, respawnLayerMask)){
                    if(!(Physics.CheckSphere(hit.point, 5f, trainLayerMask))){
                        if(!(Physics.CheckSphere(hit.point, 5f, playerLayerMask))){
                            if(!(Physics.CheckSphere(hit.point + new Vector3(0, 1, 0), 1f, obstacleLayerMask))){
                                if(!(respawnPoint.transform.position == hit.point)){
                                    respawnPointVector = hit.point;
                                    hasRespawnPoint = true;
                                } 
                            }
                        }
                        
                    }
                }
                    
                
       

        
    }

        if (hasRespawnPoint){
            respawnPoint.transform.position = respawnPointVector;
        }   else {
            FindRandomNavmeshLocation();
        }
    }

    void TrainHit(){
        isDead = true;
        wasHit = true;
        StartCoroutine(Death());
    }
   

    private void DetectPlayer()
    {
        

        if(!fightMode){
            bestDistance = 1000f;
            enemyTags = Physics.OverlapSphere(transform.position, visionRange, enemyTagMask);
            foreach(Collider enemyTag in enemyTags){
            
            
            

            if((Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position) > 0.1f)){
                if(Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position) <= bestDistance){
                    bestDistance = Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position);
                        
                            Vector3 forward = transform.forward;
                            Vector3 toEnemy = enemyTag.transform.position - ownEnemyTag.transform.position;
                            enemyAngle = Vector3.SignedAngle(forward, toEnemy, Vector3.forward);
                            if(Mathf.Abs(enemyAngle) < 90){
                                foundEnemy = enemyTag.transform.parent.gameObject;
                                enemyEyes = foundEnemy.transform.Find("Eyes").gameObject;
                                enemyBody = foundEnemy.transform.Find("Body").gameObject;

                                npcToPlayerEyesRay = new Ray(eyes.transform.position, ((enemyEyes.transform.position - eyes.transform.position).normalized));
                                npcToPlayerBodyRay = new Ray(eyes.transform.position, ((enemyBody.transform.position - eyes.transform.position).normalized));


                                isPlayerVisible = (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitVisibleObstacle, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitVisibleGround, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) || (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitVisibleObstacleBody, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitVisibleGroundBody, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask));
                                isPlayerInRange = (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitRangeObstacle, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitRangeGround, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask)) || (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitRangeObstacleBody, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitRangeGroundBody, (Vector3.Distance(ownEnemyTag.transform.position, enemyTag.transform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask));


                                
                                Debug.Log("searching");
                                
                                if(isPlayerVisible){
                                    targetEnemy = foundEnemy;
                                    targetEyes = targetEnemy.transform.Find("Eyes").gameObject;
                                    targetBody = targetEnemy.transform.Find("Body").gameObject;
                                    targetFeet = targetEnemy.transform.Find("Feet").gameObject;
                                    isPlayerVisible = true;
                                    Debug.Log("found");
                                    
                                }
                                
                            }
                            
                      
                }
            }
            
        }
        } else {
            npcToPlayerEyesRay = new Ray(eyes.transform.position, ((enemyEyes.transform.position - eyes.transform.position).normalized));
            npcToPlayerBodyRay = new Ray(eyes.transform.position, ((enemyBody.transform.position - eyes.transform.position).normalized));

            isPlayerVisible = (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitVisibleObstacle, (Vector3.Distance(eyes.transform.position, enemyEyes.transform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitVisibleGround, (Vector3.Distance(eyes.transform.position, enemyEyes.transform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) || (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitVisibleObstacleBody, (Vector3.Distance(ownBody.transform.position, enemyEyes.transform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitVisibleGroundBody, (Vector3.Distance(ownBody.transform.position, enemyEyes.transform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask));
           
            
            if(isPlayerVisible && (Vector3.Distance(ownFeet.transform.position, targetFeet.transform.position) < engagementRange)){
                isPlayerInRange = true;
            } else {
                isPlayerInRange = false;
            }
        }
    }

    private void UpdateBehaviourState(){ //Behaviour state switcher
        TrainCheck();
        StuckCheck();
        
        if (isPlayerVisible && isPlayerInRange && releaseChase){ //Can see player, is close = Attack
            PerformAttack();

        }
        else if ((isPlayerVisible && !isPlayerInRange) || (!releaseChase) || (attackedPlayer)){  //Can see player but is not close OR Cant see player, is not close but was being chased = Chase
            PerformChase();
            Debug.Log("Activate Chase");

        }
        else if(!isPlayerVisible && !isPlayerInRange && releaseChase && !attackedPlayer){ //Cant see player, Player isnt close and Player was not being chased = Patrol
            PerformPatrol();

        }
        
    }
    //MAIN FUNCTIONS

    private void TrainCheck(){
        if(Physics.Raycast(eyes.transform.position, navAgent.destination, out RaycastHit trainCheckHit, Vector3.Distance(eyes.transform.position, navAgent.destination), trainLayerMask)){
            Debug.Log("train in way");
        } else {
            Debug.Log("train not in way");
        }
    }

  

    //PATROL
    private void PerformPatrol(){ //When Patrolling state
        fightMode = false;
        if (!hasPatrolPoint){ //If no patrol point has been decided YET, run the function to find it.
            FindPatrolPoint();       
        }

        if (hasPatrolPoint){
            navAgent.SetDestination(currentPatrolPoint);
            if(patrolTimerFinished){
                StartCoroutine(PatrolPointTimer());
            }
    
        } 
    }

    private void FindPatrolPoint(){ //Finding of the patrol point
        
        Vector3 potentialPoint = new Vector3(transform.position.x + Random.Range(-patrolRadius, patrolRadius), transform.position.y + 10f, transform.position.z + Random.Range(-patrolRadius, patrolRadius)); //Calculate a desired point to send a raycast from (using the patrol radius values, with a Y value above the npc)
        RaycastHit hit; 
        if (Physics.Raycast(potentialPoint, Vector3.down, out hit, 20f, terrainLayer)){ //Send a raycast from above down to check for walkable layer.
            currentPatrolPoint = hit.point; //found valid point, go to it
            hasPatrolPoint = true;
        }
    }

    private IEnumerator PatrolPointTimer(){
        patrolTimerFinished = false;
        yield return new WaitForSeconds(patrolPointTimer);
        hasPatrolPoint = false;
        patrolTimerFinished = true;
    }

    //PATROL

    //CHASE
    private void PerformChase(){
        fightMode = false;
        if(chaseStart){
            releaseChase = false;
            chaseStart = false;
        }
        
        if(isPlayerVisible && !isPlayerInRange && !attackedPlayer){
            playerLastPosition = targetFeet.transform.position;
        }
    
        if(isPlayerVisible && isPlayerInRange){
            Debug.Log("vidim te");
            releaseChase = true;
            chaseStart = true;
        } 
        
        if(Vector3.Distance(ownFeet.transform.position, playerLastPosition) < 0.1f){
            Debug.Log("jsem u tebe");
            releaseChase = true;
            chaseStart = true;
            attackedPlayer = false;
           
        }

        playerValidCheckRay = new Ray(targetFeet.transform.position, Vector3.down);
        Debug.DrawLine(endPointCheckRay.origin, validEndPoint, Color.red);

        Physics.Raycast(playerValidCheckRay, out RaycastHit playerPosValid, 0.7f);
        Vector3 validPoint = playerPosValid.point;
        NavMeshHit validHit;
       if(NavMesh.SamplePosition(validPoint, out validHit, 0.2f, NavMesh.AllAreas)){
            isPlayerValid = true;
        } else {
            isPlayerValid = false;
        }

        if (intervalTimer == 0f)
            {
        
                    if(isPlayerValid){
                        navAgent.SetDestination(playerLastPosition);
                    } else {
                        SetBetterEndPoint();   
                    }
                    
                
            }

        intervalTimer += Time.deltaTime;
        if (intervalTimer >= 1)
            {
                intervalTimer = 0f;
            }
        
    }

     private void SetBetterEndPoint() {
        float endPointDistance = (Vector3.Distance(transform.position, targetFeet.transform.position))/endPointIncrementation;
        Vector3 endPoint = (ownFeet.transform.position + ((targetFeet.transform.position - ownFeet.transform.position).normalized) * endPointDistance);
        endPointCheckRay = new Ray(endPoint, Vector3.down);
        Physics.Raycast(endPointCheckRay, out RaycastHit endPointValid, 5f);
        validEndPoint = endPointValid.point;
        NavMeshHit endValidHit;

        Debug.DrawLine(endPointCheckRay.origin, validEndPoint, Color.red);

        if(NavMesh.SamplePosition(validEndPoint, out endValidHit, 0.2f, NavMesh.AllAreas)){
            navAgent.SetDestination(validEndPoint);
            endPointIncrementation = 2;
        } else {
            if(endPointIncrementation > 4){

            } else {
            endPointIncrementation++;
            releaseChase = true;
            chaseStart = true;
            attackedPlayer = false;
            }
            
        }
    }
    
    //CHASE
    
    //ATTACK - WILL BE UPDATED!!
    private void PerformAttack(){
        fightMode = true;
        attackedPlayer = true;
        playerLastPosition = targetFeet.transform.position;
        AttackMovement();
        if(!isOnAttackCooldown){ //attack function along with attack cooldown function
            StartCoroutine(AttackCooldownRoutine());
            StartCoroutine(FireWeapon());
        }
    }

    private IEnumerator FireWeapon(){
        yield return new WaitForSeconds(weaponDrawSpeed);
        if(!isDead){
            
            attackedPlayer = false;
            if (revolverAmmoCount > 0 && !isReloading) //Shoot action, split into two - physical part, effect part
            {
                ShootRoutine();
                StartCoroutine(ShootEffects());
            }

            if ((revolverAmmoCount == 0) && !isReloading){ //Reload function
                StartCoroutine(ReloadWeapon());
            }
        }
        
    }

    private IEnumerator ShootEffects(){
        GameObject muzzleLight = Instantiate(muzzleLightEffectPrefab, muzzlePoint); //Create instantiation of prefabs of effects on the pre-defined transform point "muzzlePoint" placed on the tip of the barrel
        GameObject muzzleSmoke = Instantiate(muzzleFlashPrefab, muzzlePoint);
        yield return new WaitForSeconds(0.05f);
        Destroy(muzzleLight);
        yield return new WaitForSeconds(5);
        Destroy(muzzleSmoke);
    }

    private void ShootRoutine() //Shoot function with animator
    {
        
        revolverAmmoCount--;
        audioSource.PlayOneShot(shootSound);
        Vector3 direction = GetShootDirectionWithSpread(); //Call bullet-spread function
        Ray ray = new Ray(muzzlePoint.transform.position, direction); //raycast of bullet from camera center with direction with applied spread
        RaycastHit hit; //Return of raycast
        
        if (Physics.Raycast(ray, out hit, 10f, hittableLayerMask))  //which effect prefab to use depending on hit object tag
                {
                    switch(hit.collider.tag){
                        case "Untagged":
                        break;

                        

                        case "Obstacle": 
                            switch(currentWeapon){
                                case "revolver":
                                Instantiate(obstacleHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                                break;

                                case "shotgun":
                                Instantiate(obstacleHitEffectPrefabShotgun, hit.point, Quaternion.LookRotation(hit.normal));
                                break;
                            }
                        break;

                        case "Ground" or "Door" or "train" or "movingTrain":
                        Instantiate(groundHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        break;

                        case "Character" or "Player":
                        Collider hitCollider = hit.collider;
                        PhysicsMaterial hitMaterial = hitCollider.material;
                        string hitLocation = hitMaterial.name;
                            switch(hitLocation){
                                case "headMaterial (Instance)":
                                    switch(currentWeapon){
                                        case "revolver":
                                        hit.transform.SendMessage("HeadHitByEnemyRevolver", ownEnemyTag);
                                        break;
                                    
                                        case "shotgun":
                                        hit.transform.SendMessage("HeadHitByEnemyShotgun", ownEnemyTag);
                                        break;
                                    }
                                break;
                                

                                case "bodyMaterial (Instance)":
                                    switch(currentWeapon){
                                        case "revolver":
                                        hit.transform.SendMessage("BodyHitByEnemyRevolver", ownEnemyTag);
                                        break;

                                        case "shotgun":
                                        hit.transform.SendMessage("BodyHitByEnemyShotgun", ownEnemyTag);
                                        break;
                                    }
                                break;

                                default:
                                break;
                            }
                        Instantiate(enemyHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        break;  
                    }
                    
                }
            if (Physics.Raycast(ray, out hit, 200f, glassLayerMask)){
                switch(hit.collider.tag){
                    case "Glass":
                        hit.transform.SendMessage("BreakGlass");
                    break;
                }
                
            }
        
    }


    private Vector3 GetShootDirectionWithSpread()
    {
        Vector3 forward = (targetBody.transform.position - muzzlePoint.transform.position) / Vector3.Distance(muzzlePoint.transform.position, targetBody.transform.position);
        float spreadX = Random.Range(-currentSpread, currentSpread) * 0.1f; // spread on the X and Y axis, normalized by 0.1f to be able to use bigger numbers in the variable declaration
        float spreadY = Random.Range(-currentSpread, currentSpread) * 0.1f; 
        Vector3 direction = forward + muzzlePoint.transform.right * spreadX + muzzlePoint.transform.up * spreadY; //direction is set to the camera direction with applied rules of direction
        return direction;
    }

    private IEnumerator ReloadWeapon()
    {
        isReloading = true;
        audioSource.PlayOneShot(breakSound);
        yield return new WaitForSeconds(0.2f);
        yield return new WaitForSeconds(0.2f);
        ogRevolverAmmoCount = revolverAmmoCount; //"Temporary" variable used only for separate values to set how many times the reload sound should play, while the HUD displays the correct current value
        for(int i = 0; i < (maxRevolverAmmoCount-ogRevolverAmmoCount); i++){ //loops for how many bullets are missing in the cylinder
            audioSource.PlayOneShot(reloadSound);
            yield return new WaitForSeconds(0.6f);
            revolverAmmoCount++;
        }
        
        yield return new WaitForSeconds(0.4f);
        audioSource.PlayOneShot(breakSound);
        isReloading = false; //reloading end
    }

    private void AttackMovement(){
        
        
                randomStrafe = Random.Range(0, 2);
                if(randomStrafe == 0){
                    raySide = new Ray(transform.position - transform.right * 3, Vector3.down);
                } else {
                    raySide = new Ray(transform.position + transform.right * 3, Vector3.down);
                }
                transform.LookAt(new Vector3(targetBody.transform.position.x, transform.position.y, targetBody.transform.position.z));
                if(!isOnStrafePoint){
                    StartCoroutine(FindStrafePoint());
                }
        
        
    }
    private IEnumerator FindStrafePoint(){
            if((Physics.Raycast(raySide, out hitGround, 15f, terrainLayer)) && !isOnStrafePoint){
                    isOnStrafePoint = true;
                    navAgent.SetDestination(hitGround.point);
                    transform.LookAt(new Vector3(targetBody.transform.position.x, transform.position.y, targetBody.transform.position.z));
                    yield return new WaitForSeconds(1f);
                    isOnStrafePoint = false;
            } else {
                FindStrafePoint();
            }        
        }
    
            
    private IEnumerator AttackCooldownRoutine(){ //simple attack speed cooldown
        isOnAttackCooldown = true; 
        yield return new WaitForSeconds(Random.Range(attackCooldown, attackCooldown * 1.5f));
        isOnAttackCooldown = false;
    }

    //ATTACK - WILL BE UPDATED!!
}