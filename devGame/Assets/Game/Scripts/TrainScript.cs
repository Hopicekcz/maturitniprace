using UnityEngine;
using System.Collections;

public class TrainScript : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float trainWait = 5f;
    public AudioClip[] musicClips; 
    [SerializeField] private Collider movingCollider;

    public float volume = 1f; 
    [SerializeField] private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(TrainCycle());
        
    }

    private IEnumerator TrainCycle(){
        yield return new WaitForSeconds(Random.Range(trainWait*0.5f, trainWait));
        audioSource.Stop();
        audioSource.clip = musicClips[2];
        audioSource.Play();
        animator.SetTrigger("arrive");
        yield return new WaitUntil(TrainNoAnimation);
        movingCollider.enabled = false;
        audioSource.Stop();
        audioSource.clip = musicClips[1];
        audioSource.Play();
        yield return new WaitForSeconds(Random.Range(trainWait, trainWait *2f));
        audioSource.Stop();
        audioSource.clip = musicClips[0];
        audioSource.Play();
        animator.SetTrigger("depart");
        yield return new WaitForSeconds(3f);
        movingCollider.enabled = true;
        yield return new WaitForSeconds(Random.Range(trainWait, trainWait*2f));
        StartCoroutine(TrainCycle());
    }

    bool TrainNoAnimation() {
         return animator.GetCurrentAnimatorStateInfo(0).IsName("trainStation") && !animator.IsInTransition(0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "CharacterListener")
        {
            other.gameObject.SendMessage("TrainHit");
        } else {
        }
    }
    
    void Update()
    {
    }
}
