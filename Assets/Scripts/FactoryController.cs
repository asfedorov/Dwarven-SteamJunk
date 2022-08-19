using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactoryController : DieableController
{
    public FogController fogController;

    public float tickTime = 2f;
    float _currentTime = 2f;

    public float amountPerTick;

    public Vector3Int selfPos;

    bool powered = false;

    public GameObject[] lights;

    Collider2D collider;

    List<Vector3Int> tilesToGenerate = new List<Vector3Int>();

    public void SetTileToGenerateFog(Vector3Int pos)
    {
        tilesToGenerate.Add(pos);
    }

    public List<Vector3Int> fogOffets = new List<Vector3Int>();

    // Start is called before the first frame update
    void Start()
    {
        fogController = FindObjectOfType<FogController>();

        foreach(var offset in fogOffets)
        {
            SetTileToGenerateFog(selfPos + offset);
        }
    }

    public void PowerOn()
    {
        powered = true;
        collider = GetComponent<Collider2D>();

        foreach(var light in lights)
        {
            light.SetActive(true);
        }
    }

    public void PowerOff()
    {
        powered = false;
        foreach(var light in lights)
        {
            light.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (powered)
        {
            if (fogController != null)
            {
                _currentTime -= Time.deltaTime;

                if (_currentTime <= 0f)
                {
                    foreach(var pos in tilesToGenerate)
                    {
                        fogController.AddFog(pos, amountPerTick);
                    }
                    _currentTime = tickTime;
                }
            }
        }
    }

}
