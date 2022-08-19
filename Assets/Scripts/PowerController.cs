using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerController : DieableController
{
    public FogController fogController;

    public float tickTime = 2f;
    float _currentTime = 2f;

    public float amountPerTick;

    public Vector3Int selfPos;

    List<Vector3Int> tilesToGenerate = new List<Vector3Int>();

    public void SetTileToGenerateFog(Vector3Int pos)
    {
        tilesToGenerate.Add(pos);
    }

    // Start is called before the first frame update
    void Start()
    {
        fogController = FindObjectOfType<FogController>();

        SetTileToGenerateFog(selfPos + Vector3Int.up);
        SetTileToGenerateFog(selfPos + Vector3Int.up + Vector3Int.right);
        SetTileToGenerateFog(selfPos + Vector3Int.up + Vector3Int.up);
        SetTileToGenerateFog(selfPos + Vector3Int.up + Vector3Int.up + Vector3Int.right);
    }

    // Update is called once per frame
    void Update()
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
