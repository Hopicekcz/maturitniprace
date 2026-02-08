using UnityEngine;
using System.Collections;
using UnityEngine.AI;



public class aiNAV : MonoBehaviour
{
    [Header("References")] //References for use by the script
    [SerializeField] private Transform playerTransform; //the playerCapsule is used for the playerTransform
    [SerializeField] private NavMeshAgent navAgent; //navigation Agent module for easier scripting
    [SerializeField] private Transform firePoint; //The empty gameobject from where to fire bullets
    [SerializeField] private Animator animator; 

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer; //layerMask for the Ground to determine walkable checks
    [SerializeField] private LayerMask playerLayerMask; //layerMask for the Player to determine where the player is
    [SerializeField] private LayerMask obstacleLayerMask; 

    [Header("Patrol Settings")] //customizable setting of the patrol radius
    [SerializeField] private float patrolRadius; //the patrol radius
    private Vector3 currentPatrolPoint; //variable storing 3D coordinates of the current Patrol point
    private Vector3 strafePoint;
    private bool hasPatrolPoint;  
    

    [Header("Combat Settings")] //customizable settings of the bullet and attack speed using rigidbody physics
    [SerializeField] private float attackCooldown = 1f;
    private int randomStrafe;
    [SerializeField] private float npcHP = 100f;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float hitTimer = 2f;
    [SerializeField] private float patrolPointTimer = 2.5f;
    [SerializeField] private float staggeredSpeed = 1f;
    [SerializeField] private float followPlayerTimer = 15f;

    [Header("Detection Ranges")] //customizable settings for the vision (follow player) and engagement (attack) range
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float engagementRange = 7f;
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
    private bool sawPlayer;
    //Ray for checking collisions between the NPC and the Player
    Ray npcToPlayerRay;
    Ray raySide;
    RaycastHit hitWall;
    RaycastHit hitGround;
    private Vector3 playerLastPosition;

    //TO ADD NEXT!!! - isPlayerVisible and isPlayerInRange checks need to be sent from an empty gameobject of the npc head to the player head (obstacles make player invisible)
    //readd shortterm chase memory, but keep the previous point to get to. there should just be a timer once the player cant be seen, then once the timer is up the npc should go to the last known point.
    //atleast optimize a bit
    //comment a lot!!!!!

    //INITIALIZATION
    private void Awake(){ //Because the EnemyNPC is a prefab and the playertransform cannot be assigned into the reference, a script hard-reference is needed
        if (playerTransform == null){
            GameObject playerObj = GameObject.Find("PlayerCapsule");
            if (playerObj != null){
                playerTransform = playerObj.transform;
            }
        }

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
        npcToPlayerRay = new Ray(transform.position, ((playerTransform.position - transform.position).normalized)); //assigning properties to the Raycast, being the instance´s position and the direction of a normalized 3D-vector (from the object to the player)

        isPlayerVisible =  !(Physics.Raycast(npcToPlayerRay, out RaycastHit hitVisible, (Vector3.Distance(transform.position, playerTransform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, visionRange, playerLayerMask);
        isPlayerInRange = !(Physics.Raycast(npcToPlayerRay, out RaycastHit hitRange, (Vector3.Distance(transform.position, playerTransform.position)), obstacleLayerMask)) && Physics.CheckSphere(transform.position, engagementRange, playerLayerMask);
        //variables for determining behaviour state - Raycast for checking if there are any obstacles between the npc and the player, CheckSphere for checking if the npc is in range of the player.
        //the RaycastHit variables are declared inside the function, to avoid unnecessary variable declaration in the initialization.
    }

    private void UpdateBehaviourState(){ //Behaviour state switcher
     Debug.Log(navAgent.destination);
        if(!isPlayerVisible && !isPlayerInRange && sawPlayer){ //Cant see player, Player isnt close and Player was not being chased = Patrol
            PerformPatrol();
        }
        else if ((isPlayerVisible && !isPlayerInRange) || (!sawPlayer)){  //Can see player but is not close OR Cant see player, is not close but was being chased = Chase
            PerformChase();
        }
        else if (isPlayerVisible && isPlayerInRange && sawPlayer){ //Can see player, is close = Attack
            PerformAttack();
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
        if(isPlayerVisible && !isPlayerInRange){
            sawPlayer = false;
            playerLastPosition = playerTransform.position;
        } else if(!isPlayerVisible  && (Vector3.Distance(transform.position, playerLastPosition) < 0.1f) || isPlayerInRange){
            sawPlayer = true;
        }
        navAgent.SetDestination(playerLastPosition);
         //simple follow the player
    }

    
    //CHASE
    
    //ATTACK - WILL BE UPDATED!!
    private void PerformAttack(){
        isMoving = true;
        fightMode = true;
        //if(!isOnAttackCooldown){ //attack function along with attack cooldown function
            FireWeapon();
            AttackMovement();
            AttackCooldownRoutine();
        //}
    }

    private void FireWeapon(){
        
    }

    private void AttackMovement(){
        if(Physics.CheckSphere(transform.position, tooCloseRange, playerLayerMask)){
            transform.LookAt(playerTransform);
            navAgent.SetDestination(transform.position - transform.forward); 
        } else {
                randomStrafe = Random.Range(0, 2);
                if(randomStrafe == 0){
                    raySide = new Ray(transform.position - transform.right * 3, Vector3.down);
                } else {
                    raySide = new Ray(transform.position + transform.right * 3, Vector3.down);
                }
                transform.LookAt(playerTransform);
                if(!isOnStrafePoint){
                    StartCoroutine(FindStrafePoint());
                }
        }
        
    }
    private IEnumerator FindStrafePoint(){
            if((Physics.Raycast(raySide, out hitGround, 3f, terrainLayer)) && !isOnStrafePoint){
                    isOnStrafePoint = true;
                    navAgent.SetDestination(hitGround.point);
                    transform.LookAt(playerTransform);
                    Debug.DrawLine(raySide.origin, hitGround.point);
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
