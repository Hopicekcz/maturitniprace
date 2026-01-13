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

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer; //layerMask for the Ground to determine walkable checks
    [SerializeField] private LayerMask playerLayerMask; //layerMask for the Player to determine where the player is

    [Header("Patrol Settings")] //customizable setting of the patrol radius
    [SerializeField] private float patrolRadius = 5f; //the patrol radius
    private Vector3 currentPatrolPoint; //variable storing 3D coordinates of the current Patrol point
    private bool hasPatrolPoint; 

    [Header("Combat Settings")] //custmizable settings of the bullet and attack speed using rigidbody physics
    [SerializeField] private float attackCooldown = 1f;
    private bool isOnAttackCooldown;
    [SerializeField] private float forwardShotForce = 10f;
    [SerializeField] private float verticalShotForce = 5f;

    [Header("Detection Ranges")] //customizable settings for the vision (follow player) and engagement (attack) range
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float engagementRange = 5f;

    private bool isPlayerVisible;
    private bool isPlayerInRange;

    //UTILITIES
    private void Awake(){ //Fail-safe check (should not be needed)
        if (playerTransform == null){
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null){
                playerTransform = playerObj.transform;
            }
        }

        if(navAgent == null){
            navAgent = GetComponent<NavMeshAgent>();
        }
    }

     private void OnDrawGizmosSelected() //Helper for debugging and play-testing with gizmos
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
    //UTILITIES

    //MAIN FUNCTIONS
    private void Update(){ //Constant activation of functions
        DetectPlayer();
        UpdateBehaviourState();
    }

    private void DetectPlayer()
    {
        isPlayerVisible = Physics.CheckSphere(transform.position, visionRange, playerLayerMask);
        isPlayerInRange = Physics.CheckSphere(transform.position, engagementRange, playerLayerMask);
    }

    private void UpdateBehaviourState(){ //Behaviour state switcher
        if(!isPlayerVisible && !isPlayerInRange){ //Cant see player, Player isnt close
            MoveToPatrolPoint();
        }
        else if (isPlayerVisible && !isPlayerInRange){ //Can see player, player isnt close
            PerformChase();
        }
        else if (isPlayerVisible && isPlayerInRange){ //can see player, player is close
            PerformAttack();
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
            navAgent.SetDestination(playerTransform.position); //simple follow the player
        }
    }
    //CHASE
    
    //ATTACK - WILL BE UPDATED!!
    private void PerformAttack(){
        navAgent.SetDestination(transform.position); //set to own transforms position to stand still while shooting, change in the future to strafe!!
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
