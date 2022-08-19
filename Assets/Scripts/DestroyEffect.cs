using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyEffect : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // if (other.tag == "Player")
        // {
        //     other.gameObject.GetComponent<PlayerController>().SetEffect(effectName);
        // }

        DieableController dc = other.gameObject.GetComponent<DieableController>();

        if (dc != null)
        {
            dc.Die();
        }
    }
}
