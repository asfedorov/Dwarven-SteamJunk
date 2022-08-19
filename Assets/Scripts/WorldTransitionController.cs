using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldTransitionController : MonoBehaviour
{
    public enum Direction
    {
        LEFT,
        UP,
        RIGHT,
        DOWN
    }

    public GameObject world;
    bool inTransition = false;
    float transitionStep = 56f;

    Vector3 targetPos;
    Vector3 realPos;

    // Start is called before the first frame update
    void Start()
    {
        targetPos = world.transform.localPosition;
        realPos = world.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {

        if (world.transform.localPosition != targetPos)
        {
            inTransition = true;

            // world.transform.localPosition = Vector3.MoveTowards(world.transform.localPosition, targetPos, Time.deltaTime * transitionStep / 2f);
            realPos = Vector3.MoveTowards(realPos, targetPos, Time.deltaTime * transitionStep / 2f);
        }
        else
        {
            inTransition = false;
        }

        world.transform.localPosition = new Vector3(
            Mathf.Round(realPos.x),
            Mathf.Round(realPos.y),
            0f
        );

    }

    public void TransitionToDirection(Direction direction)
    {
        // if (inTransition)
        // {
        //     return;
        // }

        switch (direction)
        {
            case Direction.LEFT:
                targetPos += Vector3.left * transitionStep;
                break;
            case Direction.UP:
                targetPos += Vector3.up * transitionStep;
                break;
            case Direction.RIGHT:
                targetPos += Vector3.right * transitionStep;
                break;
            case Direction.DOWN:
                targetPos += Vector3.down * transitionStep;
                break;

            default:
                break;
        }
    }
}
