using UnityEngine;

public class trainHitListener : MonoBehaviour
{
    [SerializeField] private aiNAV aiNAV;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

     void TrainHit(){
        aiNAV.SendMessage("TrainHit");
     }

    // Update is called once per frame
    void Update()
    {
        
    }
}
