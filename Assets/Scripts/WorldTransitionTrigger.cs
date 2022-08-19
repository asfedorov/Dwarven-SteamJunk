using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldTransitionTrigger : MonoBehaviour
{

    public WorldTransitionController controller;
    public WorldTransitionController.Direction direction;


    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            controller.TransitionToDirection(direction);
        }
    }
}
