using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class aiNAV : MonoBehaviour
{
    [Header("References")] //References for use by the script
    [SerializeField] private Transform playerTransform; //the playerCapsule is used for the playerTransform
    [SerializeField] private NavMeshAgent navAgent; //navigation Agent module for easier scripting
    [SerializeField] private Transform firePoint; //The empty gameobject from where to fire bullets
    [SerializeField] private GameObject projectilePrefab; //projectile prefab (will most probably be a bullet)
    [SerializeField] private Animator animator; 

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer; //layerMask for the Ground to determine walkable checks
    [SerializeField] private LayerMask playerLayerMask; //layerMask for the Player to determine where the player is
    [SerializeField] private LayerMask obstacleLayerMask; 

    [Header("Patrol Settings")] //customizable setting of the patrol radius
    [SerializeField] private float patrolRadius = 5f; //the patrol radius
    [SerializeField] private float followPlayerTimer = 1f;
    private Vector3 currentPatrolPoint; //variable storing 3D coordinates of the current Patrol point
    private bool hasPatrolPoint;  
    

    [Header("Combat Settings")] //customizable settings of the bullet and attack speed using rigidbody physics
    [SerializeField] private float attackCooldown = 1f;
    private bool isOnAttackCooldown;
    [SerializeField] private float forwardShotForce = 10f;
    [SerializeField] private float verticalShotForce = 5f;

    [Header("Detection Ranges")] //customizable settings for the vision (follow player) and engagement (attack) range
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float engagementRange = 5f;

    //Declaration of bools for behavior states
    private bool isPlayerVisible;
    private bool isPlayerInRange;
    private bool isSearching;
    private bool isChasing;
    private bool isShooting;
    private bool wasChasing = false;
    //Ray for checking collisions between the NPC and the Player
    Ray npcToPlayerRay;
    

    //INITIALIZATION
    private void Awake(){ //Fail-safe checks (should not be needed)
        if (playerTransform == null){
            GameObject playerObj = GameObject.Find("Player");
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
    //INITIALIZATION


    //MAIN FUNCTIONS
    private void Update(){ //Constant activation of functions
        DetectPlayer();
        UpdateBehaviourState();
        SetAnimation();
    }

    private void SetAnimation(){ //Animation setter using bool parameters in the animator
        animator.SetBool("isSearching", isSearching);
        animator.SetBool("isChasing", isChasing);
        animator.SetBool("isShooting", isShooting);
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
        if(!isPlayerVisible && !isPlayerInRange && !wasChasing){ //Cant see player, Player isnt close and Player was not being chased = Patrol
            MoveToPatrolPoint();
            isSearching = true;
            isChasing = false;
            isShooting = false;
        }
        else if ((isPlayerVisible && !isPlayerInRange) || (!isPlayerVisible && !isPlayerInRange && wasChasing)){  //Can see player but is not close OR Cant see player, is not close but was being chased = Chase
            PerformChase();
            isSearching = false;
            isChasing = true;
            isShooting = false;
        }
        else if (isPlayerVisible && isPlayerInRange){ //Can see player, is close = Attack
            PerformAttack();
            isSearching = false;
            isChasing = false;
            isShooting = true;
        }
    }
    //MAIN FUNCTIONS


    //PATROL
    private void MoveToPatrolPoint(){ //When Patrolling state
        if (!hasPatrolPoint) //If no patrol point has been decided YET, run the function to find it.
        FindPatrolPoint();

        if (hasPatrolPoint) //Once found, utilize the navigationAgent component to move to the found patrol point.
        navAgent.SetDestination(currentPatrolPoint);

        if(Vector3.Distance(transform.position, currentPatrolPoint) < 1f) //Once reached (conditions are determined by the .Distance property) loop
        hasPatrolPoint = false;
    }


    private void FindPatrolPoint(){ //Finding of the patrol point
        float randomX = Random.Range(-patrolRadius, patrolRadius); //is possible in the future to utilize Y coordinates as well. 
        float randomZ = Random.Range(-patrolRadius, patrolRadius); //IMPLEMENT!!

        Vector3 potentialPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ); //Get a desired point to go to by calculating from the current position with the found randoms within the radius.

        if (Physics.Raycast(potentialPoint, -transform.up, 2f, terrainLayer)){ //Send a raycast to the potential point to check for ground layer 
            currentPatrolPoint = potentialPoint; //if point valid, set it.
            hasPatrolPoint = true;
        }
    }
    //PATROL

    //CHASE
    private void PerformChase(){
        if (playerTransform != null){ //fail-safe to avoid errors
            navAgent.isStopped = false;
            navAgent.SetDestination(playerTransform.position); //simple follow the player
            StartCoroutine(WasChasingTimer()); //Start the Coroutine of checking for when the chase stops, functioning as a short-term memory function
        }
    }

    private IEnumerator WasChasingTimer(){
        bool isChasingWaitCondition(){ //converting the isChasing state into a function boolean for use with the WaitUntil function
            return !isChasing;
        }
        yield return new WaitUntil(isChasingWaitCondition); //Wait until isChasing stops being true. (NPC stopped chasing)
        wasChasing = true;
        yield return new WaitForSeconds(followPlayerTimer); //Once the additional chasing ends, "forget"
        wasChasing = false;
    }
    //CHASE
    
    //ATTACK - WILL BE UPDATED!!
    private void PerformAttack(){
        navAgent.isStopped = true; //set to own transforms position to stand still while shooting, change in the future to strafe!!
        if (playerTransform != null){ //quick check, doesnt have to be there
            transform.LookAt(playerTransform); //look at the player before shooting
        }

        if(!isOnAttackCooldown){ //attack function along with attack cooldown function
            FireWeapon();
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private void FireWeapon(){
        if (projectilePrefab == null || firePoint == null) return; //check

        Rigidbody projectileRb = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity).GetComponent<Rigidbody>(); //create a reference to the rigidbody property of the bullet prefab instantiation firing from the firePoint position with quaternion identity to determine rotation based on the current 
        projectileRb.AddForce(transform.forward * forwardShotForce, ForceMode.Impulse); //add force to the bullet, should be changed in the future to a raycast, a projectile is non-optimal.

        Destroy(projectileRb.gameObject, 3f); //destroy the instantiated prefab 
    }

    private IEnumerator AttackCooldownRoutine(){ //simple attack speed cooldown
        isOnAttackCooldown = true; 
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    //ATTACK - WILL BE UPDATED!!
}
