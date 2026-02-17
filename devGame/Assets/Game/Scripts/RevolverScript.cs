using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using StarterAssets;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class RevolverScript : MonoBehaviour
{
    //References for the revolver
    [Header("RevolverSounds")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip breakSound;
    [Header("ShotgunSounds")]
    [SerializeField] private AudioClip shotgunShootSound;
    [SerializeField] private AudioClip shotgunBreakSound;
    [SerializeField] private AudioClip shotgunReloadSound;
    [SerializeField] private AudioClip shotgunClickSound;


    [Header("RevolverShootEffects")]
    [SerializeField] private GameObject obstacleHitEffectPrefab;
    
    [SerializeField] private GameObject enemyHitEffectPrefab;
    [SerializeField] private GameObject groundHitEffectPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject muzzleLightEffectPrefab;
    [Header("ShotgunShootEffects")]
    [SerializeField] private GameObject muzzleFlashPrefabShotgun;
    [SerializeField] private GameObject obstacleHitEffectPrefabShotgun;
    [Header("ShootMuzzleSmokeLocation")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private Transform shotgunMuzzlePoint;
   
    [Header("AmmoCounter")]
    [SerializeField] private Text ammoCounter;


    //Properties of the revolver
    [Header("Revolver properties")]
    [SerializeField] [Range(0f, 5f)] private float hipFireSpread = 0.5f;
    [SerializeField] [Range(0f, 5f)] private float aimedSpread = 0.1f;
    [SerializeField] [Range(0.2f, 5f)] private float fireRate = 1f;
    [SerializeField] private int maxRevolverAmmoCount = 6;
    [SerializeField] private int maxShotgunAmmoCount = 2;

    [Header("Shotgun properties")]
    [SerializeField] private int shotgunPelletCount;
    
    [Header("Weapon References")]
    [SerializeField] private GameObject revolverObject;
    [SerializeField] private GameObject shotgunObject;
    public GameObject weapon1Object;
    public GameObject weapon2Object;

    //References
    [Header("Other References")]
    [SerializeField]private Animator handAnimator;
    [SerializeField]private Animator gunAnimator;
    [SerializeField] private Animator shotgunAnimator;
    [SerializeField]private AudioSource audioSource;
    [SerializeField] private Camera mainCamera;
    private FirstPersonController firstPersonController;

    //Variable initialization
    private bool canShoot;
    private bool isAiming ;
    private bool isReloading;
    public bool isDead;
    private int revolverAmmoCount;
    private int ogRevolverAmmoCount;
    private int shotgunAmmoCount;
    private int ogShotgunAmmoCount;
    private string weapon1;
    private string weapon2;
    private string currentWeapon;
    private int currentWeaponNumber;
    private bool interruptReload;
    private bool switchingWeapon;
    
    

    //Input button definitions
    private InputAction shootAction;
    private InputAction aimAction;
    private InputAction reloadAction;
    private InputAction switchWeaponAction1;
    private InputAction switchWeaponAction2;


    //INITIALIZATION
    
    void Start() //Declaration of game components
    {

        weapon1 = "revolver";
        weapon2 = "shotgun";
        currentWeapon = weapon1;
        

        revolverAmmoCount = maxRevolverAmmoCount;
        shotgunAmmoCount = maxShotgunAmmoCount;
        canShoot = true;
        isReloading = false;
        isAiming = false;
        shootAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        aimAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/rightButton");
        reloadAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/r");
        switchWeaponAction1 = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/1");
        switchWeaponAction2 = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/2");
        shootAction.Enable();
        aimAction.Enable();
        reloadAction.Enable();
        switchWeaponAction1.Enable();
        switchWeaponAction2.Enable();
        //handAnimator = GetComponent<Animator>();
        //gunAnimator = GetComponentInChildrenOnly<Animator>();

        weapon1Object = revolverObject;
        weapon2Object = shotgunObject;
        weapon1Object.SetActive(true);
        weapon2Object.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        firstPersonController = GameObject.Find("PlayerCapsule").GetComponent<FirstPersonController>();
        mainCamera = Camera.main;
    }

    T GetComponentInChildrenOnly<T>() where T : Component
    {
        foreach (Transform child in transform)
        {
            T comp = child.GetComponentInChildren<T>();
            if (comp != null)
                return comp;
        }
        return null;
    }

    void PlayerDied() // Unity input safety
    {
        
        GameObject[] RevolverEffects;
        RevolverEffects = GameObject.FindGameObjectsWithTag("RevolverEffect");
        foreach(var i in RevolverEffects){
            Destroy(i);
        }
    }

        
    //INITIALIZATION

    //MAIN FUNCTIONS
    void Update()
    {
        if(!isDead){
            WeaponState();
            PlayerSpeed();
        } else{
            PlayerDied();
            revolverAmmoCount = maxRevolverAmmoCount;
            shotgunAmmoCount = maxShotgunAmmoCount;
            canShoot = true;
            isReloading = false;
            isAiming = false;
        }
    }

    void PlayerSpeed(){
        if(isAiming){
            firstPersonController.MoveSpeed = 1.5f;
            firstPersonController.SprintSpeed = 1.5f;
        } else {
            firstPersonController.MoveSpeed = 3f;
            firstPersonController.SprintSpeed = 4f;
        }
    }

    private void WeaponState(){
        if(!isReloading && !switchingWeapon) {  //ignore if trying to aim while reloading
            isAiming = aimAction.IsPressed();
        } else {
            isAiming = false;
        }
        handAnimator.SetBool("IsAiming", isAiming); //set animation to current bool state of aiming
        StartCoroutine(SwitchWeapon());


        switch(currentWeapon){
            case "revolver":
            ammoCounter.text = revolverAmmoCount + "/" + maxRevolverAmmoCount; //ui hud of current ammo in revolver
        
            if(shootAction.WasPressedThisFrame()){
                if (canShoot && (revolverAmmoCount > 0) && !isReloading) //Shoot action, split into two - physical part, effect part
                {
                    StartCoroutine(ShootRoutine());
                }

                if(isReloading){
                    interruptReload = true;
                }

                if (canShoot && (revolverAmmoCount == 0) && !isReloading){ //shoot action when weapon is empty
                    audioSource.PlayOneShot(clickSound);
                }
            }
            
            if (reloadAction.WasPressedThisFrame() && (revolverAmmoCount < maxRevolverAmmoCount) && !isReloading){ //Reload function
                StartCoroutine(ReloadWeapon());
            }
            break;

            case "shotgun":
            ammoCounter.text = shotgunAmmoCount + "/" + maxShotgunAmmoCount;
                if(shootAction.WasPressedThisFrame()){
                    if(canShoot && !isReloading  && (shotgunAmmoCount > 0)){
                        StartCoroutine(ShootRoutine());
                    }

                    if(isReloading){
                        interruptReload = true;
                    }

                    if(canShoot && (shotgunAmmoCount == 0) && !isReloading){
                        audioSource.PlayOneShot(shotgunClickSound);
                    }
                }

                if (reloadAction.WasPressedThisFrame() && (shotgunAmmoCount < maxShotgunAmmoCount) && !isReloading){ //Reload function
                StartCoroutine(ReloadWeapon());
                 }
            break;
        }
    }

    private IEnumerator SwitchWeapon(){
        if (switchWeaponAction1.WasPressedThisFrame() && canShoot && !(currentWeapon == weapon1)){
            switchingWeapon = true;
            if(isAiming){
                isAiming = false;
                yield return new WaitForSeconds(0.27f);
            }
            if(isReloading){
                interruptReload = true;
                yield return new WaitUntil(() => !interruptReload);
                yield return new WaitForSeconds(0.2f);
            } 
            handAnimator.SetBool("switchingWeapon", switchingWeapon);
            yield return new WaitForSeconds(0.3f);
            weapon1Object.SetActive(true);
            weapon2Object.SetActive(false);
            currentWeapon = weapon1;
            switchingWeapon = false;
            handAnimator.SetBool("switchingWeapon", switchingWeapon);
        }
        if(switchWeaponAction2.WasPressedThisFrame() && canShoot && !(currentWeapon == weapon2)){
            
            switchingWeapon = true;
            if(isAiming){
                isAiming = false;
                yield return new WaitForSeconds(0.27f);
            }
            if(isReloading){
                interruptReload = true;
                yield return new WaitUntil(() => !interruptReload);
                yield return new WaitForSeconds(0.2f);
            } 
            handAnimator.SetBool("switchingWeapon", switchingWeapon);
            yield return new WaitForSeconds(0.3f);
            weapon1Object.SetActive(false);
            weapon2Object.SetActive(true);
            currentWeapon = weapon2;
            switchingWeapon = false;
            handAnimator.SetBool("switchingWeapon", switchingWeapon);
        }
    }
    

    private IEnumerator ReloadWeapon()
    {
        if(isAiming){
                    isReloading = true;
                    yield return new WaitForSeconds(0.27f);
                } else {
                    isReloading = true;
                }
                handAnimator.SetTrigger("Reload");
                handAnimator.SetBool("isReloading", isReloading);
                yield return new WaitForSeconds(0.2f);

        switch(currentWeapon){
            case "revolver":
                gunAnimator.SetBool("isReloading", isReloading); //Set revolver to reloading position
                gunAnimator.SetTrigger("Reload"); //Keep revolver in reloading position
                yield return new WaitForSeconds(0.2f);
                ogRevolverAmmoCount = revolverAmmoCount; //"Temporary" variable used only for separate values to set how many times the reload sound should play, while the HUD displays the correct current value
                for(int i = 0; i < (maxRevolverAmmoCount-ogRevolverAmmoCount); i++){ //loops for how many bullets are missing in the cylinder
                    if(interruptReload){
                        break;
                    }
                    audioSource.PlayOneShot(reloadSound);
                    yield return new WaitForSeconds(0.6f);
                    revolverAmmoCount++;
                }
                handAnimator.SetBool("isReloading", false); //Move hand out of reloading position
                gunAnimator.SetBool("isReloading", false); //Move revolver out of reloading position
                
                yield return new WaitForSeconds(0.4f);
                audioSource.PlayOneShot(breakSound);
                isReloading = false;
                interruptReload = false;
            break;

            case "shotgun":
                shotgunAnimator.SetBool("isReloading", isReloading);
                yield return new WaitForSeconds(0.2f);
                ogShotgunAmmoCount = shotgunAmmoCount;
                audioSource.PlayOneShot(shotgunBreakSound);
                yield return new WaitForSeconds(0.6f);
                for(int i = 0; i < (maxShotgunAmmoCount-ogShotgunAmmoCount); i++){
                    if(interruptReload){
                        break;
                    }
                    audioSource.PlayOneShot(shotgunReloadSound);
                    yield return new WaitForSeconds(0.6f);
                    shotgunAmmoCount++;
                }
                handAnimator.SetBool("isReloading", false);
                shotgunAnimator.SetBool("isReloading", false);
                yield return new WaitForSeconds(0.2f);
                audioSource.PlayOneShot(shotgunBreakSound);
                yield return new WaitForSeconds(0.4f);
                isReloading = false;
                interruptReload = false;
            break;
            
        }
        
    }

    private IEnumerator ShootEffects()
    {
        switch(currentWeapon){
            case "revolver":
                GameObject muzzleLight = Instantiate(muzzleLightEffectPrefab, muzzlePoint); //Create instantiation of prefabs of effects on the pre-defined transform point "muzzlePoint" placed on the tip of the barrel
                GameObject muzzleSmoke = Instantiate(muzzleFlashPrefab, muzzlePoint);
                muzzleLight.gameObject.tag = "RevolverEffect";
                muzzleSmoke.gameObject.tag = "RevolverEffect";
                yield return new WaitForSeconds(0.05f);
                Destroy(muzzleLight);
                yield return new WaitForSeconds(5f);
                Destroy(muzzleSmoke);
            
            break;

            case "shotgun":
                GameObject muzzleLightShotgun = Instantiate(muzzleLightEffectPrefab, shotgunMuzzlePoint);
                GameObject muzzleSmokeShotgun = Instantiate(muzzleFlashPrefabShotgun, shotgunMuzzlePoint);
                yield return new WaitForSeconds(0.05f);
                Destroy(muzzleLightShotgun);
                yield return new WaitForSeconds(5f);
                Destroy(muzzleSmokeShotgun);
            break;
        }
        }
        


    private IEnumerator ShootRoutine() //Shoot function with animator
    {
        switch(currentWeapon){

            case "revolver":
                    StartCoroutine(ShootEffects());
                    revolverAmmoCount--;
                    canShoot = false; //added bool for detection of delay
                    gunAnimator.SetTrigger("Shoot");
                    audioSource.PlayOneShot(shootSound);
                    ShootRaycasts();
                    //Shot delay calculation
                    float delay = 1f / fireRate;
                    yield return new WaitForSeconds(delay);
                    canShoot = true;
            break;

            case "shotgun":
                StartCoroutine(ShootEffects());
                shotgunAmmoCount--;
                canShoot = false;
                shotgunAnimator.SetTrigger("Shoot");
                audioSource.PlayOneShot(shotgunShootSound);
                for(int i = 0; i < shotgunPelletCount; i++){
                   ShootRaycasts();
                }
                yield return new WaitForSeconds(1.5f);
                canShoot = true;
            break;
            

        }
        
    }

    private void ShootRaycasts(){
        Vector3 direction = GetShootDirectionWithSpread(); //Call bullet-spread function
        Ray ray = new Ray(mainCamera.transform.position, direction); //raycast of bullet from camera center with direction with applied spread
        RaycastHit hit; //Return of raycast
        if (Physics.Raycast(ray, out hit))  //which effect prefab to use depending on hit object tag
                {
                    switch(hit.collider.tag){
                        case "Untagged":
                        break;

                        case "Obstacle": 
                        Instantiate(obstacleHitEffectPrefabShotgun, hit.point, Quaternion.LookRotation(hit.normal));
                        break;

                        case "Ground":
                        Instantiate(groundHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        break;

                        case "Character": 
                        hit.transform.SendMessage ("HitByPlayerShotgun");
                        Instantiate(enemyHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        break;
                    }
                    
                }
    }

    private Vector3 GetShootDirectionWithSpread()
    {
        Vector3 forward = mainCamera.transform.forward; // direction of camera
        float currentSpread = !isAiming || (currentWeapon == "shotgun") ? hipFireSpread : aimedSpread; // current spread is set to a predefined value determined by whether or not the player is aiming
                            // 1/0        1             0   
        float spreadX = Random.Range(-currentSpread, currentSpread) * 0.1f; // spread on the X and Y axis, normalized by 0.1f to be able to use bigger numbers in the variable declaration
        float spreadY = Random.Range(-currentSpread, currentSpread) * 0.1f; 
        Vector3 direction = forward + mainCamera.transform.right * spreadX + mainCamera.transform.up * spreadY; //direction is set to the camera direction with applied rules of direction
        return direction.normalized; //normalize vector3 with math function
    }
    //MAIN FUNCTIONS

    

    
}

