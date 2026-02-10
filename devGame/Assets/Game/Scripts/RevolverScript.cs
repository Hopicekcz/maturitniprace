using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class RevolverScript : MonoBehaviour
{
    //References for the revolver
    [Header("Sounds")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip breakSound;

    [Header("ShootEffects")]
    [SerializeField] private GameObject obstacleHitEffectPrefab;
    [SerializeField] private GameObject enemyHitEffectPrefab;
    [SerializeField] private GameObject groundHitEffectPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject muzzleLightEffectPrefab;
    [Header("ShootMuzzleSmokeLocation")]
    [SerializeField] private Transform muzzlePoint;
   
    [Header("AmmoCounter")]
    [SerializeField] private Text ammoCounter;


    //Properties of the revolver
    [Header("Revolver properties")]
    [SerializeField] [Range(0f, 5f)] private float hipFireSpread = 0.5f;
    [SerializeField] [Range(0f, 5f)] private float aimedSpread = 0.1f;
    [SerializeField] [Range(0.2f, 5f)] private float fireRate = 1f;
    [SerializeField] private int maxRevolverAmmoCount = 6;
    

    //References
    [Header("Other References")]
    [SerializeField]private Animator handAnimator;
    [SerializeField]private Animator gunAnimator;
    [SerializeField]private AudioSource audioSource;
    [SerializeField] private Camera mainCamera;

    //Variable initialization
    private bool canShoot = true;
    private bool isAiming = false;
    private bool isReloading = false;
    private int revolverAmmoCount;
    private int ogRevolverAmmoCount;
    
    

    //Input button definitions
    private InputAction shootAction;
    private InputAction aimAction;
    private InputAction reloadAction;


    //INITIALIZATION
    void Awake() //Declaration and activation for keybindings
    {
        
    }

    void Start() //Declaration of game components
    {
        shootAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        aimAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/rightButton");
        reloadAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/r");
        shootAction.Enable();
        aimAction.Enable();
        reloadAction.Enable();
        revolverAmmoCount = maxRevolverAmmoCount; 
        handAnimator = GetComponent<Animator>();
        gunAnimator = GetComponentInChildrenOnly<Animator>();
        audioSource = GetComponent<AudioSource>();
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

    void OnDisable() // Unity input safety
    {
       
    }
        
    //INITIALIZATION

    //MAIN FUNCTIONS
    void Update()
    {
        WeaponState();
    }

    private void WeaponState(){
        if(!isReloading) {  //ignore if trying to aim while reloading
            isAiming = aimAction.IsPressed();
        } else {
            isAiming = false;
        }
        handAnimator.SetBool("IsAiming", isAiming); //set animation to current bool state of aiming
        ammoCounter.text = revolverAmmoCount + "/" + maxRevolverAmmoCount; //ui hud of current ammo in revolver
        
        if (shootAction.WasPressedThisFrame() && canShoot && (revolverAmmoCount > 0) && !isReloading) //Shoot action, split into two - physical part, effect part
        {
            StartCoroutine(ShootRoutine());
            StartCoroutine(ShootEffects());
        }

        if (shootAction.WasPressedThisFrame() && canShoot && (revolverAmmoCount == 0) && !isReloading){ //shoot action when weapon is empty
            audioSource.PlayOneShot(clickSound);
        }

        if (reloadAction.WasPressedThisFrame() && (revolverAmmoCount < maxRevolverAmmoCount) && !isReloading){ //Reload function
            StartCoroutine(ReloadWeapon());
        }
    }

    private IEnumerator ReloadWeapon()
    {
        if(isAiming){ //if currently in aiming animation, wait until complete
            isReloading = true;
            yield return new WaitForSeconds(0.27f);
        } else {
            isReloading = true;
        }
        
        audioSource.PlayOneShot(breakSound);
        handAnimator.SetTrigger("Reload");  //Move hand to reloading position
        handAnimator.SetBool("isReloading", isReloading); //Keep hand in reloading position
       
        yield return new WaitForSeconds(0.2f);
        gunAnimator.SetBool("isReloading", isReloading); //Set revolver to reloading position
        gunAnimator.SetTrigger("Reload"); //Keep revolver in reloading position
        yield return new WaitForSeconds(0.2f);
        ogRevolverAmmoCount = revolverAmmoCount; //"Temporary" variable used only for separate values to set how many times the reload sound should play, while the HUD displays the correct current value
        for(int i = 0; i < (maxRevolverAmmoCount-ogRevolverAmmoCount); i++){ //loops for how many bullets are missing in the cylinder
            audioSource.PlayOneShot(reloadSound);
            yield return new WaitForSeconds(0.6f);
            revolverAmmoCount++;
        }

        handAnimator.SetBool("isReloading", false); //Move hand out of reloading position
        gunAnimator.SetBool("isReloading", false); //Move revolver out of reloading position
        
        yield return new WaitForSeconds(0.4f);
        audioSource.PlayOneShot(breakSound);
        isReloading = false; //reloading end
    }

    private IEnumerator ShootEffects()
    {
        GameObject muzzleLight = Instantiate(muzzleLightEffectPrefab, muzzlePoint); //Create instantiation of prefabs of effects on the pre-defined transform point "muzzlePoint" placed on the tip of the barrel
        GameObject muzzleSmoke = Instantiate(muzzleFlashPrefab, muzzlePoint);
        yield return new WaitForSeconds(0.05f);
        Destroy(muzzleLight);
        yield return new WaitForSeconds(5);
        Destroy(muzzleSmoke);
    }


    private IEnumerator ShootRoutine() //Shoot function with animator
    {
        revolverAmmoCount--;
        canShoot = false; //added bool for detection of delay
        gunAnimator.SetTrigger("Shoot");
        audioSource.PlayOneShot(shootSound);
        Vector3 direction = GetShootDirectionWithSpread(); //Call bullet-spread function
        Ray ray = new Ray(mainCamera.transform.position, direction); //raycast of bullet from camera center with direction with applied spread
        RaycastHit hit; //Return of raycast

        if (Physics.Raycast(ray, out hit))  //which effect prefab to use depending on hit object tag
        {
            switch(hit.collider.tag){
                case "Untagged":
                break;

                case "Obstacle": 
                Instantiate(obstacleHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                break;

                case "Ground":
                Instantiate(groundHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                break;

                case "Character": 
                hit.transform.SendMessage ("HitByPlayerRevolver");
                Instantiate(enemyHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                break;
            }
            
        }

        //Shot delay calculation
        float delay = 1f / fireRate;
        yield return new WaitForSeconds(delay);
        canShoot = true;
    }

    private Vector3 GetShootDirectionWithSpread()
    {
        Vector3 forward = mainCamera.transform.forward; // direction of camera
        float currentSpread = isAiming ? aimedSpread : hipFireSpread; // current spread is set to a predefined value determined by whether or not the player is aiming
                            // 1/0        1             0   
        float spreadX = Random.Range(-currentSpread, currentSpread) * 0.1f; // spread on the X and Y axis, normalized by 0.1f to be able to use bigger numbers in the variable declaration
        float spreadY = Random.Range(-currentSpread, currentSpread) * 0.1f; 
        Vector3 direction = forward + mainCamera.transform.right * spreadX + mainCamera.transform.up * spreadY; //direction is set to the camera direction with applied rules of direction
        return direction.normalized; //normalize vector3 with math function
    }
    //MAIN FUNCTIONS

    

    
}

