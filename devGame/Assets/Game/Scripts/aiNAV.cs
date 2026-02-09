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
    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer; //layerMask for the Ground to determine walkable checks
    [SerializeField] private LayerMask playerLayerMask; //layerMask for the Player to determine where the player is
    [SerializeField] private LayerMask obstacleLayerMask; 
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Transform muzzlePoint;

    [Header("Sounds")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField]private AudioSource audioSource;

    [Header("Patrol Settings")] //customizable setting of the patrol radius
    [SerializeField] private float patrolRadius; //the patrol radius
    private Vector3 currentPatrolPoint; //variable storing 3D coordinates of the current Patrol point
    private Vector3 strafePoint;
    private bool hasPatrolPoint;  
    

    [Header("Combat Settings")] //customizable settings of the bullet and attack speed using rigidbody physics
    [SerializeField] private float attackCooldown = 2f;
    private int randomStrafe;
    [SerializeField] private float npcHP = 100f;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float hitTimer = 2f;
    [SerializeField] private float patrolPointTimer = 2.5f;
    [SerializeField] private float staggeredSpeed = 1f;
    [SerializeField] private float followPlayerTimer = 1f;

    [Header("Detection Ranges")] //customizable settings for the vision (follow player) and engagement (attack) range
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float engagementRange = 6f;
    [SerializeField] private float tooCloseRange = 3f;

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
    private string currentState;
    private bool attackedPlayer = false;
    //Ray for checking collisions between the NPC and the Player
    Ray npcToPlayerEyesRay;
    Ray npcToPlayerBodyRay;
    Ray raySide;
    RaycastHit hitWall;
    RaycastHit hitGround;
    private Vector3 playerLastPosition;

    //TO ADD NEXT!!! - isPlayerVisible and isPlayerInRange checks need to be sent from an empty gameobject of the npc head to the player head (obstacles make player invisible)
    //atleast optimize a bit
    //comment a lot!!!!!

    //INITIALIZATION
    private void Awake(){ //Because the EnemyNPC is a prefab and the playertransform cannot be assigned into the reference, a script hard-reference is needed
            GameObject playerObj = GameObject.Find("PlayerCapsule");
            playerTransform = playerObj.transform;
            GameObject playerObjEyes = GameObject.Find("PlayerCameraRoot");
            playerEyes = playerObjEyes.transform;
            GameObject playerObjBody = GameObject.Find("PlayerBody");
            playerBody = playerObjBody.transform;
        if(navAgent == null){
            navAgent = GetComponent<NavMeshAgent>();
        }
        animator = GetComponent<Animator>(); 
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


    //MAIN FUNCTIONS
    private void Update(){ //Constant activation of functions
    Debug.Log(currentState);
        DetectPlayer();
        UpdateBehaviourState();
        SetAnimation();
        HealthSystem();
    }

    private void SetAnimation(){ //Animation setter using bool parameters in the animator
        animator.SetBool("isMoving", isMoving);
    }

     void HitByPlayerRevolver()
        {
            wasHit = true;
            npcHP = npcHP - Random.Range(30f, 60f);
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
    

    private void HealthSystem()
    {
        if(npcHP <= 0){
            Destroy(gameObject);
        }
        StartCoroutine(SlowOnHit());
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
            currentState = "Attack";
        }
        else if ((isPlayerVisible && !isPlayerInRange) || (!releaseChase) || (attackedPlayer)){  //Can see player but is not close OR Cant see player, is not close but was being chased = Chase
            PerformChase();
            currentState = "Chase";
            Debug.Log("is it release chase" + !releaseChase);
            Debug.Log("is it attackedPlayer" + attackedPlayer);
        }
        else if(!isPlayerVisible && !isPlayerInRange && !attackedPlayer){ //Cant see player, Player isnt close and Player was not being chased = Patrol
            PerformPatrol();
            currentState = "Patrol";
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
        if(attackedPlayer){
            releaseChase = false;
            playerLastPosition = playerTransform.position;
            attackedPlayer = false;
            keepChasing = true;
            StartCoroutine(KeepChasingTimer());
        }
        if(isPlayerVisible && !isPlayerInRange){
            releaseChase = false;
            playerLastPosition = playerTransform.position;
            keepChasing = true;
            StartCoroutine(KeepChasingTimer());
            //once this stops being true start coroutine after that finishes start the code below
        } 
        if(keepChasing){
            playerLastPosition = playerTransform.position;
        }
        if(!isPlayerVisible && (Vector3.Distance(transform.position, playerLastPosition) < 0.1f) && !keepChasing || isPlayerInRange ){
            releaseChase = true;
        }
        navAgent.SetDestination(playerLastPosition);
         //simple follow the player
    }

    private IEnumerator KeepChasingTimer(){
        yield return new WaitForSeconds(followPlayerTimer);
        keepChasing = false;
    }
    
    //CHASE
    
    //ATTACK - WILL BE UPDATED!!
    private void PerformAttack(){
        isMoving = true;
        fightMode = true;
        attackedPlayer = true;
        AttackMovement();
        if(!isOnAttackCooldown){ //attack function along with attack cooldown function
            StartCoroutine(AttackCooldownRoutine());
            FireWeapon();
        }
    }

    private void FireWeapon(){
        audioSource.PlayOneShot(shootSound);
        StartCoroutine(ShootEffects());
    }

    private IEnumerator ShootEffects(){
        GameObject muzzleSmoke = Instantiate(muzzleFlashPrefab, muzzlePoint);
        yield return new WaitForSeconds(5);
        Destroy(muzzleSmoke);
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
            if((Physics.Raycast(raySide, out hitGround, 3f, terrainLayer)) && !isOnStrafePoint){
                    isOnStrafePoint = true;
                    navAgent.SetDestination(hitGround.point);
                    transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
                    yield return new WaitForSeconds(1f);
                    isOnStrafePoint = false;
                    }        
        }
    
            
    private IEnumerator AttackCooldownRoutine(){ //simple attack speed cooldown
        isOnAttackCooldown = true; 
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    //ATTACK - WILL BE UPDATED!!
}
