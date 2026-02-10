using UnityEngine;
using System.Collections;
using UnityEngine.UI;

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
    private float maxHP;
    CharacterController cc;
    

    void Awake()
    {
        cc = this.GetComponent<CharacterController>();
        maxHP = hp;
    }
    
    void Update()
    {
        HPSystem();

    }

    void HitByEnemyRevolver(){ 
        hp -= Random.Range(30f, 60f);
        Debug.Log("Hit " + hp);
    }

    private void HPSystem()
    {
        if(0 > hp){
            Debug.Log("Die");
            StartCoroutine(PlayerDeath());
        }
    }

    private IEnumerator PlayerDeath(){
        hp = maxHP;
        fpc.enabled = false;
        playerWeapon.GetComponent<RevolverScript>().enabled = false;
        playerWeapon.GetComponent<AudioSource>().enabled = false;
        cc.enabled = false;
        ammoCounter.enabled = false;
        this.transform.position = deathPoint.transform.position;
        yield return new WaitForSeconds(0.1f);
        camera.enabled = false;
        yield return new WaitForSeconds(respawnTime);
        ammoCounter.enabled = true;
        playerWeapon.GetComponent<RevolverScript>().enabled = true;
        playerWeapon.GetComponent<AudioSource>().enabled = true;
        camera.enabled = true;
        this.transform.position = respawnPoint.transform.position;
        cc.enabled = true;
        fpc.enabled = true;
    }
}
