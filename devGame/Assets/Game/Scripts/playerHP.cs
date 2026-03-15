using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class playerHP : MonoBehaviour
{
    [SerializeField] private float hp = 150f;
    [SerializeField] private float respawnTime = 2f;
    [SerializeField] private GameObject deathPoint;
    [SerializeField] private GameObject playerWeapon;
    [SerializeField] private MonoBehaviour fpc;
    [SerializeField] private GameObject respawnPoint;
    [SerializeField] private Camera camera;
    [SerializeField] private Text ammoCounter;
    [SerializeField] private Text deathText;
    [SerializeField] private float radius = 5f;
     private RevolverScript revolverScript;
    private float maxHP;
    CharacterController cc;

    public GameObject player; 
    private Vector3 lastPos;
    public float tolerance = 0.01f;  
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    private bool isPlaying = false;
    [SerializeField] private float delayBetweenClips = 0.6f;
    [SerializeField] private float shiftDelayFactor = 0.5f;
    [SerializeField] private float volume = 0.5f; 

    private InputAction sprintAction;
    

    void Start()
    {
        revolverScript = GameObject.Find("Weapon").GetComponent<RevolverScript>();
        deathPoint = GameObject.Find("DeathPoint");
        respawnPoint = GameObject.Find("RespawnPoint");
        cc = this.GetComponent<CharacterController>();
        maxHP = hp;

        player = this.gameObject;
        sprintAction = new InputAction(type: InputActionType.Button, binding:"<Keyboard>/left shift");
        sprintAction.Enable();
    }

    
    void Update()
    {
        HPSystem();
        FootstepSounds();
    }

    void FootstepSounds(){
         if (Vector3.Distance(player.transform.position, lastPos) > tolerance && IsGrounded())
        {
            if (!isPlaying)
            {
                Debug.Log("play");
                StartCoroutine(PlayNextClip());
            }
        }
        lastPos = player.transform.position;  
    }

    void HeadHitByEnemyRevolver()
    {
        hp -= Random.Range(100f, 150f);
    }

    void HeadHitByEnemyShotgun()
    {
        hp -= Random.Range(25f, 35f);
    }

    void BodyHitByEnemyRevolver()
    {
        hp -= Random.Range(50f, 70f);
    }

    void BodyHitByEnemyShotgun()
    {
        hp -= Random.Range(15f, 25f);
    }

    private void HPSystem()
    {
        if(0 > hp){
            StartCoroutine(PlayerDeath());
        }
    }

     Vector3 RandomNavmeshLocation() {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += transform.position;
            UnityEngine.AI.NavMeshHit hit;
            Vector3 finalPosition = Vector3.zero;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, radius, 1)) {
                finalPosition = hit.position;            
            }
            Debug.Log(finalPosition);
            return finalPosition;
            
    }

    private IEnumerator PlayerDeath(){

        
        respawnPoint.transform.position = RandomNavmeshLocation();
        deathText.enabled = true;
        hp = maxHP;
        fpc.enabled = false;
        playerWeapon.GetComponent<AudioSource>().enabled = false;
        revolverScript.weapon1Object.SetActive(false);
        revolverScript.weapon2Object.SetActive(false);
        revolverScript.isDead = true;
        cc.enabled = false;
        ammoCounter.enabled = false;
        this.transform.position = deathPoint.transform.position;
        yield return new WaitForSeconds(0.1f);
        camera.enabled = false;
        yield return new WaitForSeconds(respawnTime);
        ammoCounter.enabled = true;
        deathText.enabled = false;
        deathText.enabled = false;
        playerWeapon.GetComponent<AudioSource>().enabled = true;
        revolverScript.currentWeapon = revolverScript.weapon1;
        revolverScript.weapon1Object.SetActive(true);
        camera.enabled = true;
        revolverScript.isDead = false;
        this.transform.position = respawnPoint.transform.position;
        cc.enabled = true;
        fpc.enabled = true;
    }

    private IEnumerator PlayNextClip()
    {
        isPlaying = true;
        int randomIndex = Random.Range(0, audioClips.Length);
        audioSource.clip = audioClips[randomIndex];
        if(IsGrounded()){
             audioSource.Play();
        }
        
        if(Keyboard.current.shiftKey.isPressed == true){
            yield return new WaitForSeconds(delayBetweenClips * shiftDelayFactor);
            isPlaying = false;
        } else {
            yield return new WaitForSeconds(delayBetweenClips);
            isPlaying = false;
        }
        float delay = sprintAction.WasPressedThisFrame()
            ? delayBetweenClips * shiftDelayFactor 
            : delayBetweenClips;
        yield return new WaitForSeconds(delay);
    }

    private bool IsGrounded()
    {
        RaycastHit hit;
        return Physics.Raycast(player.transform.position, Vector3.down, out hit, 1.1f);
    }
}

