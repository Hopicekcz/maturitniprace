using UnityEngine;
using System.Collections;

public class DestroyableMaterial : MonoBehaviour
{

    private GameObject thisItem;
    private MeshRenderer thisMeshRenderer;
    private AudioSource audioSrc;
    private AudioClip[] audioClips;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        thisItem = this.gameObject;
        audioClips = Resources.LoadAll<AudioClip>("glassFX");
        audioSrc = this.gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
        thisMeshRenderer = this.gameObject.GetComponent<MeshRenderer>();

        audioSrc.volume = 0.3f;
        audioSrc.spatialBlend = 1f;
        audioSrc.minDistance = 2f;
        audioSrc.maxDistance = 15f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void BreakGlass()
    {
        Debug.Log(audioClips.Length);

        audioSrc.clip = audioClips[Random.Range(0, audioClips.Length)];
        audioSrc.Play();
        thisMeshRenderer.enabled = false;
        StartCoroutine(DestroyObject());
    }

    private IEnumerator DestroyObject(){
        yield return new WaitForSeconds(2f);
        Destroy(thisItem);
    }
}
