using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportationEffect : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            AppController appController = FindObjectOfType<AppController>();

            appController.FinishGame();
        //     other.gameObject.GetComponent<PlayerController>().SetEffect(effectName);
        }

        // DieableController dc = other.gameObject.GetComponent<DieableController>();

        // if (dc != null)
        // {
        //     dc.Die();
        // }
    }
}
