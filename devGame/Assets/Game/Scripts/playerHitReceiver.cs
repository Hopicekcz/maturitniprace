using UnityEngine;
using System.Collections;

public class playerHitReceiver : MonoBehaviour
{
[SerializeField] private GameObject playerParent;
private playerHP playerHP;

void Start(){
   playerParent.GetComponent<playerHP>();
}

        void HeadHitByEnemyRevolver()
        {
            playerParent.SendMessage("HeadHitByEnemyRevolver");
        }

        void HeadHitByEnemyShotgun()
        {
            playerParent.SendMessage("HeadHitByEnemyShotgun");
        }

        void BodyHitByEnemyRevolver()
        {
            Debug.Log("hit body");
            playerParent.SendMessage("BodyHitByEnemyRevolver");
        }

        void BodyHitByEnemyShotgun()
        {
            playerParent.SendMessage("BodyHitByEnemyShotgun");
        }
}
