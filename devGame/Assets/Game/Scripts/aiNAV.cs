using UnityEngine;
using System.Collections;
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
    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer; //layerMask for the Ground to determine walkable checks
    [SerializeField] private LayerMask playerLayerMask; //layerMask for the Player to determine where the player is
    [SerializeField] private LayerMask obstacleLayerMask; 
    [SerializeField] private LayerMask hittableLayerMask;
    [Header("ShootEffects")]
    [SerializeField] private GameObject obstacleHitEffectPrefab;
    [SerializeField] private GameObject enemyHitEffectPrefab;
    [SerializeField] private GameObject groundHitEffectPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject muzzleLightEffectPrefab;
    [SerializeField] private GameObject obstacleHitEffectPrefabShotgun;
    [Header("ShootMuzzleSmokeLocation")]
    [SerializeField] private Transform muzzlePoint;

    [Header("Sounds")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip breakSound;
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

    [Header("Detection Ranges")] //customizable settings for the vision (follow player) and engagement (attack) range
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float engagementRange = 6f;
    [SerializeField] private float tooCloseRange = 3f;
    [SerializeField] private string currentWeapon;
    [SerializeField] private float respawnRadius = 5f;



    //Declaration of bools for behavior states
    private bool isPlayerVisible;
    private bool isPlayerInRange;
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
    private float maxHP;
    private bool dying;
    private int weaponRandom;
    //Ray for checking collisions between the NPC and the Player
    Ray npcToPlayerEyesRay;
    Ray npcToPlayerBodyRay;
    Ray raySide;
    RaycastHit hitWall;
    RaycastHit hitGround;
    private Vector3 playerLastPosition;
    private Vector3 changeInPlayerPos;
    private Vector3 lastPosition;
    private float intervalTimer;

    //TO ADD NEXT!!! - isPlayerVisible and isPlayerInRange checks need to be sent from an empty gameobject of the npc head to the player head (obstacles make player invisible)
    //atleast optimize a bit
    //comment a lot!!!!!

    //INITIALIZATION
    private void Awake(){ //Because the EnemyNPC is a prefab and the playertransform cannot be assigned into the reference, a script hard-reference is needed
            revolverAmmoCount = maxRevolverAmmoCount; 
            GameObject playerObj = GameObject.Find("PlayerLocation");
            playerTransform = playerObj.transform;
            GameObject playerObjEyes = GameObject.Find("PlayerEyes");
            playerEyes = playerObjEyes.transform;
            GameObject playerObjBody = GameObject.Find("PlayerBody");
            playerBody = playerObjBody.transform;
            ownMeshCollider = this.GetComponent<MeshCollider>();
            maxHP = npcHP;

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

     private void OnDrawGizmosSelected() //Helper for debugging and play-testing with gizmos
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, engagementRange);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseRange);
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
                audioSource.PlayOneShot(deathJingle);
                StartCoroutine(Death());
                
            }
            
        }           
        
    }

    private void SetAnimation(){ //Animation setter using bool parameters in the animator
        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isReloading", isReloading);
    }

     void HeadHitByEnemyRevolver()
    {
        wasHit = true;
        npcHP -= Random.Range(70f, 110f);
    }

    void HeadHitByEnemyShotgun()
    {
        wasHit = true;
        npcHP = npcHP - Random.Range(25f, 35f);
    }

    void BodyHitByEnemyRevolver()
    {
        wasHit = true;
        npcHP = npcHP - Random.Range(50f, 70f);
    }

    void BodyHitByEnemyShotgun()
    {
        wasHit = true;
        npcHP = npcHP - Random.Range(15f, 25f);
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

    private IEnumerator StuckCheck(){
        unStuck = false;
        lastPosition = this.transform.position;
        yield return new WaitForSeconds(3f);
        if(transform.position == lastPosition){
            releaseChase = true;
            chaseStart = true;
            doneCalculatingChase = true;
            patrolTimerFinished = true;
        }
        unStuck = true;
    }
    

    private void HealthSystem()
    {
        if(0 > npcHP){
            isDead = true;
        }
        if(!isDead){
            StartCoroutine(SlowOnHit());
        }
        
    }

    private IEnumerator Death(){
        dying = true;
        navAgent.isStopped = true;
        audioSource.enabled = false;
        animator.enabled = false;
        ownMeshCollider.enabled = false;
        yield return new WaitForSeconds(ragdollTimer);
        
        transform.position = RandomNavmeshLocation();
        npcHP = maxHP;
        isDead = false;
        navAgent.isStopped = false;
        audioSource.enabled = true;
        animator.enabled = true;
        ownMeshCollider.enabled = true;
        dying = false;
        
        
    }

    Vector3 RandomNavmeshLocation() {
            Vector3 randomDirection = Random.insideUnitSphere * respawnRadius;
            randomDirection += transform.position;
            UnityEngine.AI.NavMeshHit hit;
            Vector3 finalPosition = Vector3.zero;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, respawnRadius, 1)) {
                finalPosition = hit.position;            
            }
            Debug.Log(finalPosition);
            return finalPosition;
            
    }

    private void DetectPlayer()
    {
        npcToPlayerEyesRay = new Ray(eyes.transform.position, ((playerEyes.transform.position - eyes.transform.position).normalized));
        npcToPlayerBodyRay = new Ray(eyes.transform.position, ((playerBody.transform.position - eyes.transform.position).normalized)); //assigning properties to the Raycast, being the instance´s position and the direction of a normalized 3D-vector (from the object to the player)

        isPlayerVisible =  (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitVisibleObstacle, (Vector3.Distance(transform.position, playerTransform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitVisibleGround, (Vector3.Distance(transform.position, playerTransform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) || (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitVisibleObstacleBody, (Vector3.Distance(transform.position, playerTransform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitVisibleGroundBody, (Vector3.Distance(transform.position, playerTransform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask));
        isPlayerInRange = (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitRangeObstacle, (Vector3.Distance(transform.position, playerTransform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerEyesRay, out RaycastHit hitRangeGround, (Vector3.Distance(transform.position, playerTransform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask)) || (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitRangeObstacleBody, (Vector3.Distance(transform.position, playerTransform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask)) && (!(Physics.Raycast(npcToPlayerBodyRay, out RaycastHit hitRangeGroundBody, (Vector3.Distance(transform.position, playerTransform.position)), terrainLayer)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask));
        //variables for determining behaviour state - Raycast for checking if there are any obstacles between the npc and the player, CheckSphere for checking if the npc is in range of the player.
        //the RaycastHit variables are declared inside the function, to avoid unnecessary variable declaration in the initialization.
    }

    private void UpdateBehaviourState(){ //Behaviour state switcher
        
        if (isPlayerVisible && isPlayerInRange && releaseChase){ //Can see player, is close = Attack
            PerformAttack();

        }
        else if ((isPlayerVisible && !isPlayerInRange) || (!releaseChase) || (attackedPlayer)){  //Can see player but is not close OR Cant see player, is not close but was being chased = Chase
            PerformChase();

        }
        else if(!isPlayerVisible && !isPlayerInRange && releaseChase && !attackedPlayer){ //Cant see player, Player isnt close and Player was not being chased = Patrol
            PerformPatrol();

        }
        
    }
    //MAIN FUNCTIONS


    //PATROL
    private void PerformPatrol(){ //When Patrolling state
        fightMode = false;
        if (!hasPatrolPoint){ //If no patrol point has been decided YET, run the function to find it.
            FindPatrolPoint();       
        }

        if (hasPatrolPoint){
            isMoving = true;
            navAgent.SetDestination(currentPatrolPoint);
            if(patrolTimerFinished){
                StartCoroutine(PatrolPointTimer());
            }
            
            if (Vector3.Distance(transform.position, currentPatrolPoint) < 0.1f)
            {
            isMoving = false;
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
        isMoving = true;
        if(chaseStart){
            releaseChase = false;
            chaseStart = false;
        }
        
        if(isPlayerVisible && !isPlayerInRange && !attackedPlayer){
            playerLastPosition = playerTransform.position;
        }
    
        if(isPlayerVisible && isPlayerInRange){
            releaseChase = true;
            chaseStart = true;
        }
        
        if(Vector3.Distance(transform.position, playerLastPosition) < 0.1f){
            releaseChase = true;
            chaseStart = true;
            attackedPlayer = false;
        }
        if (intervalTimer == 0f)
            {
                navAgent.SetDestination(playerLastPosition);
            }

        intervalTimer += Time.deltaTime;
        if (intervalTimer >= 1)
            {
                intervalTimer = 0f;
            }
        while(unStuck){
            StartCoroutine(StuckCheck());
        }
        
    }

    
    //CHASE
    
    //ATTACK - WILL BE UPDATED!!
    private void PerformAttack(){
        isMoving = true;
        fightMode = true;
        attackedPlayer = true;
        playerLastPosition = playerTransform.position;
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
        
        if (Physics.Raycast(ray, out hit, hittableLayerMask))  //which effect prefab to use depending on hit object tag
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

                        case "Ground":
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
                                        hit.transform.SendMessage("HeadHitByEnemyRevolver");
                                        break;
                                    
                                        case "shotgun":
                                        hit.transform.SendMessage("HeadHitByEnemyShotgun");
                                        break;
                                    }
                                break;
                                

                                case "bodyMaterial (Instance)":
                                    switch(currentWeapon){
                                        case "revolver":
                                        hit.transform.SendMessage("BodyHitByEnemyRevolver");
                                        break;

                                        case "shotgun":
                                        hit.transform.SendMessage("BodyHitByEnemyShotgun");
                                        break;
                                    }
                                break;
                            }
                        Instantiate(enemyHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        break;  
                    }
                    
                }
    }


    private Vector3 GetShootDirectionWithSpread()
    {
        Vector3 forward = (playerEyes.position - muzzlePoint.transform.position) / Vector3.Distance(muzzlePoint.transform.position, playerEyes.position);
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
        if(Physics.CheckSphere(transform.position, tooCloseRange, playerLayerMask)){
            transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
            navAgent.SetDestination(transform.position - transform.forward); 
        } else {
                randomStrafe = Random.Range(0, 2);
                if(randomStrafe == 0){
                    raySide = new Ray(transform.position - transform.right * 3, Vector3.down);
                } else {
                    raySide = new Ray(transform.position + transform.right * 3, Vector3.down);
                }
                transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
                if(!isOnStrafePoint){
                    StartCoroutine(FindStrafePoint());
                }
        }
        
    }
    private IEnumerator FindStrafePoint(){
            if((Physics.Raycast(raySide, out hitGround, 15f, terrainLayer)) && !isOnStrafePoint){
                    isOnStrafePoint = true;
                    navAgent.SetDestination(hitGround.point);
                    transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
                    yield return new WaitForSeconds(1f);
                    isOnStrafePoint = false;
            } else {
                FindStrafePoint();
            }        
        }
    
            
    private IEnumerator AttackCooldownRoutine(){ //simple attack speed cooldown
        isOnAttackCooldown = true; 
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    //ATTACK - WILL BE UPDATED!!
}
