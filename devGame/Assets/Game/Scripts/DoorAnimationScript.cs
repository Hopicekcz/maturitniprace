using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorAnimationScript : MonoBehaviour
{
    bool _trigerEntered = false;
    [SerializeField]
    Animator _animator;

    bool _opened = false;
    public AudioSource audioSource;
    public AudioClip doorOpenClip;
    public AudioClip doorCloseClip;

    private bool _isAnimating = false;
    private float _audioCooldown = 0.5f; 
    private float _lastAudioTime = 0f; 

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_trigerEntered && !_isAnimating)
        {
            if (Keyboard.current[Key.E].wasPressedThisFrame)
            {
                if (_opened)
                {
                    Play(doorCloseClip, "door-close");
                }
                else
                {
                    Play(doorOpenClip, "door-open");
                }
            }
        }
    }

    private void Play(AudioClip clip, string animationName) //nemam na to kamo 
    {
        if (Time.time >= _lastAudioTime + _audioCooldown) //co to je za bullshit
        {
            audioSource.clip = clip; //Time.time kamo co to je
            audioSource.Play();
            _lastAudioTime = Time.time; 
        }

        _animator.Play(animationName);
        _isAnimating = true;

        
        StartCoroutine(ResetAnimatingFlag(animationName));
    }

    private IEnumerator ResetAnimatingFlag(string animationName)
    {
       
        while (_animator.GetCurrentAnimatorStateInfo(0).IsName(animationName) &&
               _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1) //jo je to z chatgpt protoze tohle je fakt bs sorry
        {
            yield return null; 
        }

        _isAnimating = false; 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _trigerEntered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _trigerEntered = false;
        }
    }

    public void Open()
    {
        _opened = true;
    }

    public void Close()
    {
        _opened = false; //boli me brich
    }
}