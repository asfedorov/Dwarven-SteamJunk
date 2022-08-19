using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;


[System.Serializable]
public struct Effect
{
    public string Name;
    public float Duration;
    public UnityEvent OnStart;
    public UnityEvent OnEnd;
    public Sprite Icon;
    public GameObject effectIconPrefab;
}


[System.Serializable]
public struct PlayerState
{
    public string Name;
    public PlayerStateEnum StateEnum;
    public Sprite[] StateSprite;
}

public enum PlayerStateEnum
{
    Idle,
    Moving,
    Jumping,
    Swimming,
    Digging,
    Dying,
    Dead
}

public class PlayerController : DieableController
{

    // Need to be in the same order as array of player state struct

    public enum PlayerEffect
    {
        Levitation,
        Light
    }

    public enum PlayerFaceDirection {
        Left,
        Up,
        Right,
        Down
    }
    // Start is called before the first frame update

    public Effect[] availableEffects;
    public Dictionary<string, float> activeEffects = new Dictionary<string, float>();

    public float playerSpeed = 10f; //speed player moves
    public float jumpSpeed = 15f;
    public float jumpDuration = 2f;
    public float dyingDuration = 1f;
    float _jumpDurationLeft = 0f;
    float _dyingDurationLeft = 0f;
    float _deadDurationLeft = 0.5f;
    public float swimmingSpeed = 5f; //speed player moves
    public float diggingDuration = 0.5f;
    float _diggingDurationLeft;

    float jumpDurationExtra = 0f;

    Vector3 realPos;
    Collider2D collider;

    InputAction moveAction;

    bool move = false;
    Vector2 moveInput;
    SpriteRenderer renderer;

    public PlayerFaceDirection faceDirection = PlayerFaceDirection.Left;

    // need to be in the same order as enum
    public PlayerState[] states;
    public PlayerState currentState;

    public TileManager tileManager;
    public int playerLayer;

    public int _jumpStartLayer;

    public TilemapCollider2D[] tilemapColliders;
    public CompositeCollider2D[] compositeColliders;

    public GameObject target;
    SpriteRenderer targetSpriteRenderer;
    bool digMode = false;

    public UnityEngine.Rendering.Universal.Light2D pointLight;

    public GameObject effectPanel;
    Dictionary<string, Image> effectToIconMapping = new Dictionary<string, Image>();
    public GameObject effectIconPrefab;

    public List<int> jumpLayersIgnore = new List<int>();
    public List<int> jumpLayersIgnoreExtra = new List<int>();

    AudioSource audioSource;

    public AudioClip onDig;
    public AudioClip onDigFail;
    public AudioClip onPowerUp;

    public bool Initialized = false;

    public AppController appController;

    void Start()
    {
        realPos = transform.localPosition;
        collider = gameObject.GetComponent<Collider2D>();
        renderer = gameObject.GetComponent<SpriteRenderer>();
        targetSpriteRenderer = target.GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    public void SetState(PlayerStateEnum newState)
    {
        foreach (var state in states)
        {
            if (state.StateEnum != newState)
            {
                continue;
            }

            if (state.StateSprite.Length > 1)
            {
                renderer.sprite = state.StateSprite[(int)faceDirection];
                ((PolygonCollider2D)collider).TryUpdateShapeToAttachedSprite();
            }
            else
            {
                renderer.sprite = state.StateSprite[0];
                ((PolygonCollider2D)collider).TryUpdateShapeToAttachedSprite();
            }

            currentState = state;
        }
    }

    void Update ()
    {
        if (!Initialized)
        {
            return;
        }
        switch (currentState.StateEnum)
        {
            case PlayerStateEnum.Idle:
                if (playerLayer == 0)
                {
                    SetState(PlayerStateEnum.Swimming);
                }
                break;
            case PlayerStateEnum.Moving:
                transform.Translate(
                    moveInput.normalized.x * playerSpeed * Time.deltaTime,
                    moveInput.normalized.y * playerSpeed * Time.deltaTime,
                    0
                );

                if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                {
                    if (moveInput.x > 0)
                    {
                        faceDirection = PlayerFaceDirection.Right;
                    }
                    else
                    {
                        faceDirection = PlayerFaceDirection.Left;
                    }
                }
                else
                {
                    if (moveInput.y > 0)
                    {
                        faceDirection = PlayerFaceDirection.Up;
                    }
                    else
                    {
                        faceDirection = PlayerFaceDirection.Down;
                    }
                }

                break;

            case PlayerStateEnum.Jumping:
                transform.Translate(
                    moveInput.normalized.x * jumpSpeed * Time.deltaTime,
                    moveInput.normalized.y * jumpSpeed * Time.deltaTime,
                    0
                );
                _jumpDurationLeft -= Time.deltaTime;
                if (_jumpDurationLeft <= 0f)
                {
                    SetState(PlayerStateEnum.Idle);
                    OnLanding();
                }
                break;

            case PlayerStateEnum.Swimming:
                transform.Translate(
                    moveInput.normalized.x * swimmingSpeed * Time.deltaTime,
                    moveInput.normalized.y * swimmingSpeed * Time.deltaTime,
                    0
                );

                if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                {
                    if (moveInput.x > 0)
                    {
                        faceDirection = PlayerFaceDirection.Right;
                    }
                    else
                    {
                        faceDirection = PlayerFaceDirection.Left;
                    }
                }
                else
                {
                    if (moveInput.y > 0)
                    {
                        faceDirection = PlayerFaceDirection.Up;
                    }
                    else
                    {
                        faceDirection = PlayerFaceDirection.Down;
                    }
                }

                break;

            case PlayerStateEnum.Digging:
                _diggingDurationLeft -= Time.deltaTime;

                if (_diggingDurationLeft <= 0f)
                {
                    SetState(PlayerStateEnum.Idle);

                    Vector3Int cellPos;
                    if (tileManager.SetDirt(target.transform.position, transform.position, out cellPos))
                    {
                        // Debug.Log(tileManager.FindFigure(cellPos));
                        audioSource.clip = onDig;
                        audioSource.Play();
                    }
                    else
                    {
                        audioSource.clip = onDigFail;
                        audioSource.Play();
                    }
                }
                break;

            case PlayerStateEnum.Dying:
                _dyingDurationLeft -= Time.deltaTime;
                if (_dyingDurationLeft <= 0f)
                {
                    SetState(PlayerStateEnum.Dead);
                }
                break;

            case PlayerStateEnum.Dead:
                _deadDurationLeft -= Time.deltaTime;
                if (_deadDurationLeft <= 0f)
                {
                    appController.OnDeath();
                }
                break;

            default:
                break;
        }

        playerLayer = tileManager.GetLayerFromWorldPos(transform.position);
        target.transform.position = tileManager.GetNeighboursCoordFromWorldPos(transform.position)[(int)faceDirection] + Vector3.up * 0.01f + Vector3.right * 0.01f;

        CheckActiveEffects();
    }


    public void Move(InputAction.CallbackContext context)
    {
        if (
            currentState.StateEnum != PlayerStateEnum.Idle &&
            currentState.StateEnum != PlayerStateEnum.Moving &&
            currentState.StateEnum != PlayerStateEnum.Swimming
        )
        {
            return;
        }
        if (moveAction == null)
        {
            moveAction = context.action;
        }

        if (context.started)
        {
            moveInput = context.ReadValue<Vector2>();
            if (playerLayer == 0)
            {
                SetState(PlayerStateEnum.Swimming);
            }
            else
            {
                SetState(PlayerStateEnum.Moving);
            }
        }
        else if (context.performed)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero;
            SetState(PlayerStateEnum.Idle);
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (digMode)
            {
                _diggingDurationLeft = diggingDuration;
                SetState(PlayerStateEnum.Digging);
                return;
            }

            _jumpDurationLeft = jumpDuration + jumpDurationExtra;
            _jumpStartLayer = playerLayer;

            // Physics2D.IgnoreCollision(collider, tilemapColliders[_jumpStartLayer], true);
            // Physics2D.IgnoreCollision(collider, compositeColliders[_jumpStartLayer], true);
            foreach (int l in jumpLayersIgnore)
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, l, true);
            }
            if (_jumpStartLayer > 1 || activeEffects.ContainsKey("Levitation"))
            {
                foreach (int l in jumpLayersIgnoreExtra)
                {
                    Physics2D.IgnoreLayerCollision(gameObject.layer, l, true);
                }
            }

            SetState(PlayerStateEnum.Jumping);
        }
        else if (context.canceled)
        {
            if (digMode)
            {
                return;
            }
            SetState(PlayerStateEnum.Idle);
            OnLanding();
        }
    }

    public void DigMode(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            digMode = !digMode;
            targetSpriteRenderer.enabled = digMode;
        }
    }

    public override void Die()
    {
        _dyingDurationLeft = dyingDuration;
        SetState(PlayerStateEnum.Dying);

        audioDieSource = GetComponent<AudioSource>();
        audioDieSource.loop = false;
        audioDieSource.clip = clip;
        audioDieSource.Play();
    }

    void OnLanding()
    {
        // Physics2D.IgnoreCollision(collider, tilemapColliders[_jumpStartLayer], false);
        // Physics2D.IgnoreCollision(collider, compositeColliders[_jumpStartLayer], false);
        foreach (int l in jumpLayersIgnore)
        {
            Physics2D.IgnoreLayerCollision(gameObject.layer, l, false);
        }

        foreach (int l in jumpLayersIgnoreExtra)
        {
            Physics2D.IgnoreLayerCollision(gameObject.layer, l, false);
        }

        if (activeEffects.ContainsKey("Levitation"))
        {
            Physics2D.IgnoreLayerCollision(gameObject.layer, 10, true);
        }

        if (playerLayer == 0)
        {
            return;
        }

        if (playerLayer < _jumpStartLayer)
        {
            if (!activeEffects.ContainsKey("Levitation"))
            {
                Die();
            }
        }
    }

    public void SetEffect(string name)
    {
        foreach (var effect in availableEffects)
        {
            if(effect.Name != name)
            {
                continue;
            }

            if (activeEffects.ContainsKey(name))
            {
                activeEffects[name] = Time.time;
            }
            else
            {
                activeEffects[name] = Time.time;
                effect.OnStart.Invoke();

                var icon = Instantiate(effect.effectIconPrefab, effectPanel.transform);
                effectToIconMapping[name] = icon.GetComponent<Image>();
            }

            audioSource.clip = onPowerUp;
            audioSource.Play();
        }
    }

    void CheckActiveEffects()
    {
        foreach (var effect in availableEffects)
        {
            if (!activeEffects.ContainsKey(effect.Name))
            {
                continue;
            }

            if (activeEffects[effect.Name] + effect.Duration < Time.time)
            {
                effect.OnEnd.Invoke();
                activeEffects.Remove(effect.Name);
                Destroy(effectToIconMapping[effect.Name].gameObject);
                effectToIconMapping.Remove(effect.Name);
            }
            else
            {
                float rate = (activeEffects[effect.Name] + effect.Duration - Time.time) / effect.Duration;
                if (rate <= 0.2f)
                {
                    effectToIconMapping[effect.Name].color = new Color(1f, 1f, 1f, 0.2f);
                }
                else if (rate <= 0.5f)
                {
                    effectToIconMapping[effect.Name].color = new Color(1f, 1f, 1f, 0.5f);
                }
                else
                {
                    effectToIconMapping[effect.Name].color = new Color(1f, 1f, 1f, 1f);
                }
            }
        }
        // foreach( var effect in activeEffects)
        // {
        //     effect.DurationLeft -= Time.deltaTime;
        //     if (effect.DurationLeft <= 0)
        //     {
        //         effect.OnEnd();
        //     }
        // }
    }

    public void OnLightEffStart()
    {
        pointLight.intensity = 1f;

        pointLight.shadowIntensity = 0.7f;
        pointLight.shapeLightFalloffSize = 20f;

    }

    public void OnLightEffEnd()
    {
        pointLight.intensity = 0.021f;

        pointLight.shadowIntensity = 0f;
        pointLight.shapeLightFalloffSize = 0.33f;
    }

    public void OnLevitationEffectStart()
    {
        jumpDurationExtra = 1f;
        Physics2D.IgnoreLayerCollision(gameObject.layer, 10, true);
    }

    public void OnLevitationEffectEnd()
    {
        jumpDurationExtra = 0f;
        Physics2D.IgnoreLayerCollision(gameObject.layer, 10, false);
    }

}
