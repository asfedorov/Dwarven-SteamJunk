using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    public float speed = 15f;
    public float maxLife = 5f;
    public Vector3 direction;

    public float birthTime = 1.3f;
    public float aliveIntensity = 0.18f;
    public float dieIntensity = 1.4f;
    public float dieTime = 0.13f;
    float _birthTime = 0f;
    float _dieTime = 0f;

    bool explode = false;

    public AudioClip start;
    public AudioClip die;

    AudioSource _audioSource;

    public UnityEngine.Rendering.Universal.Light2D pointLight;
    // Start is called before the first frame update
    void Start()
    {
        pointLight.intensity = 0f;
        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_birthTime < birthTime)
        {
            _birthTime += Time.deltaTime;
            pointLight.intensity = Mathf.Lerp(
                0f,
                aliveIntensity,
                _birthTime / birthTime
            );
        }

        if (explode)
        {
            if (_dieTime < dieTime)
            {
                _dieTime += Time.deltaTime;
                pointLight.intensity = Mathf.Lerp(
                    aliveIntensity,
                    dieIntensity,
                    _dieTime / dieTime
                );
            }
            else
            {
                pointLight.enabled = false;

                if (!_audioSource.isPlaying)
                {
                    Destroy(gameObject);
                }
            }
        }
        else
        {

            if (direction != Vector3.zero)
            {
                transform.Translate(
                    direction.normalized.x * speed * Time.deltaTime,
                    direction.normalized.y * speed * Time.deltaTime,
                    0
                );

                maxLife -= Time.deltaTime;
                if (maxLife <= 0)
                {
                    Destroy(gameObject);
                }
            }

        }

    }

    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
        _audioSource.clip = start;
        _audioSource.Play();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag != "Projectile")
        {
            explode = true;
            _audioSource.clip = die;
            _audioSource.Play();

            if (other.tag == "Player")
            {
                other.gameObject.GetComponent<PlayerController>().Die();
            }
        }
    }
}
