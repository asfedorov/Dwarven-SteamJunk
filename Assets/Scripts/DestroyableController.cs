using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class DestroyableController : MonoBehaviour
{

    public int hitPoints;
    public GameObject[] leftovers;

    public Sprite[] destructedSprites;

    int _maxHitpoints;
    SpriteRenderer renderer;

    public AudioClip onImpact;
    AudioSource _audioSource;

    void Start()
    {
        _maxHitpoints = hitPoints;
        renderer = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();
    }

    public virtual bool GetHit() {
        hitPoints -= 1;
        if (hitPoints <= 0)
        {
            DieableController dc = gameObject.GetComponent<DieableController>();
            if (dc != null)
            {
                dc.Die();
            }
            else
            {
                Destroy(gameObject);
            }
            SpawnLeftovers();
            return true;
        }

        if (destructedSprites.Length > 0)
        {
            float hitPointsStep = _maxHitpoints / destructedSprites.Length;
            int index = (int)((_maxHitpoints - hitPoints) / hitPointsStep);

            if (index >= destructedSprites.Length)
            {
                index = destructedSprites.Length - 1;
            }

            renderer.sprite = destructedSprites[index];

        }
        // _audioSource.clip = onImpact;
        // _audioSource.Play();

        return false;
    }

    void SpawnLeftovers()
    {
        AppController appController = FindObjectOfType<AppController>();
        foreach (var go in leftovers)
        {
            Instantiate(go, gameObject.transform.position, Quaternion.identity, gameObject.transform.parent);
            appController.score.LeftoversLeft += 1;
        }
    }
}

/// 10 / 4 = 2.5 - step
/// 