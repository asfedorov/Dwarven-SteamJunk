using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    // Start is called before the first frame update

    public Sprite openedSprite;

    SpriteRenderer renderer;


    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            // explode = true;
            // _audioSource.clip = die;
            // _audioSource.Play();
            renderer.sprite = openedSprite;

            other.gameObject.GetComponent<PlayerController>().Die();
        }
    }
}
