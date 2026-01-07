using System.Collections; 
using System.Collections.Generic; 
using UnityEngine; 
using UnityEngine.InputSystem; 

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class RevolverScript : MonoBehaviour 
{
    //References for the revolver
    [Header("ShootSound")] 
    public AudioClip shootSound;
    [Header("ShootEffect")] 
    public GameObject impactEffectPrefab; 
    [Header("ShootMuzzleSmokeEffect")]
    public GameObject muzzleFlashPrefab; 
    [Header("ShootMuzzleSmokeLocation")] 
    public Transform muzzlePoint; 
    [Header("MuzzleLightEffect")]
    public GameObject muzzleLightEffectPrefab; 

    //Properties of the revolver
    [Header("ShootMuzzleFlashDuration")]
    [Range(0.05f, 2f)] public float muzzleFlashDuration = 0.2f; 
    [Header("ShootRevolverSpread")] 
    [Range(0f, 5f)] public float hipFireSpread = 0.5f; 
    [Range(0f, 5f)] public float aimedSpread = 0.1f; 
    [Header("FirerateRevolver)")]
    [Range(0.2f, 5f)] public float fireRate = 1f; 

    
    //References
    private Animator animator; 
    private AudioSource audioSource; 
    private Camera mainCamera; 
    
    //Variable initialization
    private bool canShoot = true; 
    private bool isAiming = false; 

    //Input button definitions
    private InputAction shootAction; 
    private InputAction aimAction; 



    void Awake() //Activation and declaration for keybindings
    {
        shootAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton"); 
        aimAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/rightButton"); 
        shootAction.Enable(); 
        aimAction.Enable(); 
    }

    void Start() //Declaration of game components
    {
        animator = GetComponent<Animator>(); 
        audioSource = GetComponent<AudioSource>(); 
        mainCamera = Camera.main; 
    }

    void Update() 
    {
        //Aim detection for communication with animator
        isAiming = aimAction.IsPressed(); 
        animator.SetBool("IsAiming", isAiming); //Name of bool, bool value

        //Shoot action - into two functions
        if (shootAction.WasPressedThisFrame() && canShoot)
        {
            StartCoroutine(ShootRoutine()); 
            StartCoroutine(ShootEffects()); 
        }
    }

    private IEnumerator ShootEffects() 
    {
        GameObject muzzleLight = Instantiate(muzzleLightEffectPrefab, muzzlePoint);
        GameObject muzzleSmoke = Instantiate(muzzleFlashPrefab, muzzlePoint);
        yield return new WaitForSeconds(0.05f);
        Destroy(muzzleLight);
        yield return new WaitForSeconds(5);
        Destroy(muzzleSmoke);
    }


    private IEnumerator ShootRoutine() //Shoot function with animator
    {
        canShoot = false; //
        animator.SetTrigger("Shoot"); 
        audioSource.PlayOneShot(shootSound); 
        Vector3 direction = GetShootDirectionWithSpread(); //Call bullet-spread function
        Ray ray = new Ray(mainCamera.transform.position, direction); 
        RaycastHit hit; //Return of raycast

        if (Physics.Raycast(ray, out hit))
        {
            Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal)); 
            //change effect depending on hit object tag!
        }

        //Shot delay calculation
        float delay = 1f / fireRate; 
        yield return new WaitForSeconds(delay); 
        canShoot = true; 
    }

    private Vector3 GetShootDirectionWithSpread() 
    {
        Vector3 forward = mainCamera.transform.forward; // direction of camera
        float currentSpread = isAiming ? aimedSpread : hipFireSpread; // which spread to use
                           // 1/0        1             0   
        float spreadX = Random.Range(-currentSpread, currentSpread) * 0.1f; // spread X
        float spreadY = Random.Range(-currentSpread, currentSpread) * 0.1f; // spread Y
        Vector3 direction = forward + mainCamera.transform.right * spreadX + mainCamera.transform.up * spreadY; // end result
        return direction.normalized; 
    }

    void OnDisable() // Unity input safety
    {
        shootAction.Disable(); 
        aimAction.Disable(); 
    }
}
