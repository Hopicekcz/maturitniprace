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
    [SerializeField] private Image hpImage;
    [SerializeField] private Image bloodImage;
    [SerializeField] private Material bloodMaterial;
    [SerializeField] private Sprite[] hpSprites;
    [SerializeField] private Material saturationMaterial;
    [SerializeField] private SkinnedMeshRenderer playerHands;
    [SerializeField] private SkinnedMeshRenderer playerBody;
    [SerializeField] private LayerMask respawnLayerMask;
    [SerializeField] private LayerMask trainLayerMask;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float headshotModifier = 1f;
     private RevolverScript revolverScript;
    private float maxHP;
    CharacterController cc;

    public GameObject player; 
    private Vector3 lastPos;
    private int spriteNumber;
    private float hitEffectDuration = 2f;
    public float tolerance = 0.01f;  
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    private bool isPlaying = false;
    private float transparencyValue = -1f;
    [SerializeField] private float delayBetweenClips = 0.6f;
    [SerializeField] private float shiftDelayFactor = 0.5f;
    [SerializeField] private float volume = 0.5f; 
    [SerializeField] private float revolverDamage = 50f;
    [SerializeField] private float shotgunDamage = 15f;
    [SerializeField] private float rifleDamage = 100f;
    private float saturationValue;
    private bool gotHit;
    private InputAction sprintAction;
    private bool hasRespawnPoint;
    private Vector3 respawnPointVector;

    void Start()
    {
        
        saturationValue = 0;
        transparencyValue = -1f;
        HPSystem();
        revolverScript = GameObject.Find("Weapon").GetComponent<RevolverScript>();
        deathPoint = GameObject.Find("DeathPoint");
        respawnPoint = GameObject.Find("RespawnPoint");
        cc = this.GetComponent<CharacterController>();
        maxHP = hp;
        gotHit = false;

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
                StartCoroutine(PlayNextClip());
            }
        }
        lastPos = player.transform.position;  
    }

    void HeadHitByEnemyRevolver()
    {
        StartCoroutine(HitEffect());
        hp -= Random.Range(revolverDamage * headshotModifier * 1.8f, revolverDamage * headshotModifier * 2.2f);
    }

    void HeadHitByEnemyShotgun()
    {
        StartCoroutine(HitEffect());
        hp -= Random.Range(shotgunDamage * headshotModifier * 1.8f , shotgunDamage * headshotModifier * 2.2f);
    }

    void BodyHitByEnemyRevolver()
    {
        StartCoroutine(HitEffect());
        hp -= Random.Range(revolverDamage, revolverDamage * 1.4f);
    }

    void BodyHitByEnemyShotgun()
    {
        StartCoroutine(HitEffect());
        hp -= Random.Range(shotgunDamage, shotgunDamage * 1.4f);
    }

    void HeadHitByEnemyRifle()
    {
        StartCoroutine(HitEffect());
        hp -= Random.Range(rifleDamage * headshotModifier * 1.8f, rifleDamage * headshotModifier * 2.2f);
    }
    void BodyHitByEnemyRifle()
    {
        StartCoroutine(HitEffect());
        hp -=  Random.Range(rifleDamage, rifleDamage * 1.4f);
    }

     private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "movingTrain")
        {
            StartCoroutine(PlayerDeath());
        } else {
        }
    }



    private void HPSystem()
    {
        saturationValue = (((hp/maxHP)-1)/2);
        Vector4 hsvaValues = new Vector4(0f, saturationValue, 0f, 0f);
        Vector4 transparencyValues = new Vector4(0f, 0f, 0f, transparencyValue);
        bloodMaterial.SetVector("_HSVAAdjust", transparencyValues);
        saturationMaterial.SetVector("_HSVAAdjust", hsvaValues);
        if(maxHP >= hp && hp > maxHP * 0.75f){
            spriteNumber = 0;
        } else if(maxHP * 0.75f >= hp && hp > maxHP * 0.5f) {
            spriteNumber = 1;
        } else if(maxHP * 0.5f >= hp && hp > maxHP * 0.25f) {
            spriteNumber = 2;
        } else if(maxHP * 0.25f >= hp && hp > maxHP * 0f) {
            spriteNumber = 3;
        }
        if(0 > hp){
            StartCoroutine(PlayerDeath());
        }
        hpImage.sprite = hpSprites[spriteNumber];
    }

    private IEnumerator HitEffect(){
            gotHit = true;
            yield return new WaitForSeconds(0.1f);
            gotHit = false;
            transparencyValue = 0;
            yield return new WaitForSeconds(hitEffectDuration);
            for(int i = 0; i < 10; i++){
                if(gotHit){
                    transparencyValue = 0;
                    StopCoroutine(HitEffect());
                    StartCoroutine(HitEffect());
                    break;
                } else {
                    if(transparencyValue >= -1f){
                        transparencyValue -= 0.1f;
                    }
                    
                    yield return new WaitForSeconds(0.1f);
                }
           StopCoroutine(HitEffect());
        }
        
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


    private IEnumerator PlayerDeath(){

        audioSource.mute = true;
        FindRandomNavmeshLocation();
        hpImage.enabled = false;
        deathText.enabled = true;
        hp = maxHP;
        fpc.enabled = false;
        playerHands.enabled = false;
        playerBody.enabled = false;
        revolverScript.weapon1Object.SetActive(false);
        revolverScript.weapon2Object.SetActive(false);
        revolverScript.weapon3Object.SetActive(false);
        revolverScript.isDead = true;
        cc.enabled = false;
        ammoCounter.enabled = false;
        this.transform.position = deathPoint.transform.position;
        yield return new WaitForSeconds(0.1f);
        camera.enabled = false;
        yield return new WaitForSeconds(respawnTime);
        ammoCounter.enabled = true;
        deathText.enabled = false;
        playerHands.enabled = true;
        playerBody.enabled = true;
        audioSource.mute = false;
        revolverScript.currentWeapon = revolverScript.weapon1;
        revolverScript.weapon1Object.SetActive(true);
        camera.enabled = true;
        hpImage.enabled = true;
        revolverScript.isDead = false;
        this.transform.position = respawnPoint.transform.position;
        cc.enabled = true;
        fpc.enabled = true;
        hasRespawnPoint = false;
    }

    private IEnumerator PlayNextClip()
    {
        isPlaying = true;
        int randomIndex = Random.Range(0, audioClips.Length);
        audioSource.clip = audioClips[randomIndex];
        if(IsGrounded()){
             audioSource.Play();
        }
    
        yield return new WaitForSeconds(delayBetweenClips);
        isPlaying = false;
        float delay = delayBetweenClips;
        yield return new WaitForSeconds(delay);
    }

    private bool IsGrounded()
    {
        RaycastHit hit;
        return Physics.Raycast(player.transform.position, Vector3.down, out hit, 1.1f);
    }
}

