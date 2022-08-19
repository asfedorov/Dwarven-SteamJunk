using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DieableController : MonoBehaviour
{
    public AudioClip clip;
    public AudioSource audioDieSource;
    public bool countOnUtilized = false;

    public virtual void Die() {
        audioDieSource = GetComponent<AudioSource>();
        audioDieSource.loop = false;
        audioDieSource.clip = clip;
        audioDieSource.Play();

        GetComponent<SpriteRenderer>().enabled = false;

        if (countOnUtilized)
        {
            AppController appController = FindObjectOfType<AppController>();
            appController.score.LeftoversUtilized += 1;
        }

        StartCoroutine(WaitToDie());
    }

    IEnumerator WaitToDie()
    {
        yield return new WaitWhile(() => audioDieSource.isPlaying);

        Destroy(gameObject);
    }
}
