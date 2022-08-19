using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Tilemaps;

public class FogController : MonoBehaviour
{
    public Tilemap fogTilemap;
    public TileManager tileManager;

    public Tile[] fogTiles;

    public float tickTime = 2f;
    float _currentTime = 2f;

    public float loosePerTick = 0.2f;
    public float receivePerTick = 2f;
    public float maxCapacity = 1f;

    float[,] tilesCapacity;
    int sizeX;
    int sizeY;

    void Start()
    {
        sizeX = (int)(tileManager.MapSize.x * 7 / 3);
        sizeY = (int)(tileManager.MapSize.y * 7 / 3);
        tilesCapacity = new float[sizeX, sizeY];
        _currentTime = tickTime;
    }

    int2[] GetNeighbours(int2 pos)
    {
        List<int2> result = new List<int2>();
        if (pos.x > 0)
        {
            result.Add(pos + new int2(-1, 0));
        }
        if (pos.x < sizeX - 1)
        {
            result.Add(pos + new int2(1, 0));
        }
        if (pos.y > 0)
        {
            result.Add(pos + new int2(0, -1));
        }
        if (pos.y < sizeY - 1)
        {
            result.Add(pos + new int2(0, 1));
        }

        return result.ToArray();
    }

    int2[] GetNeighbours(int2 pos, HashSet<int2> except)
    {
        List<int2> result = new List<int2>();

        if (pos.x > 0)
        {
            var newPos = pos + new int2(-1, 0);
            int groundX = (int)(pos.x * 3 / 7);
            int groundY = (int)(pos.y * 3 / 7);
            int layer = tileManager.GetCellLayer(new Vector3Int(groundX, groundY, 0));
            if (!except.Contains(newPos) && layer < 2)
            {
                result.Add(newPos);
            }
        }
        if (pos.x < sizeX - 1)
        {
            var newPos = pos + new int2(1, 0);
            int groundX = (int)(pos.x * 3 / 7);
            int groundY = (int)(pos.y * 3 / 7);
            int layer = tileManager.GetCellLayer(new Vector3Int(groundX, groundY, 0));
            if (!except.Contains(newPos) && layer < 2)
            {
                result.Add(newPos);
            }
        }
        if (pos.y > 0)
        {
            var newPos = pos + new int2(0, -1);
            int groundX = (int)(pos.x * 3 / 7);
            int groundY = (int)(pos.y * 3 / 7);
            int layer = tileManager.GetCellLayer(new Vector3Int(groundX, groundY, 0));
            if (!except.Contains(newPos) && layer < 2)
            {
                result.Add(newPos);
            }
        }
        if (pos.y < sizeY - 1)
        {
            var newPos = pos + new int2(0, 1);
            int groundX = (int)(pos.x * 3 / 7);
            int groundY = (int)(pos.y * 3 / 7);
            int layer = tileManager.GetCellLayer(new Vector3Int(groundX, groundY, 0));
            if (!except.Contains(newPos) && layer < 2)
            {
                result.Add(newPos);
            }
        }

        return result.ToArray();
    }


    HashSet<int2> prevs = new HashSet<int2>();
    public void AddFog(Vector3Int cellPos, float amount)
    {
        // Debug.Log($"Requested to add fog at pos {cellPos.x},{cellPos.y}");
        tilesCapacity[cellPos.x, cellPos.y] += amount;

        int2 current = new int2(cellPos.x, cellPos.y);
        prevs.Add(current);

        // Debug.Log($"New amount in {cellPos.x},{cellPos.y}: {tilesCapacity[cellPos.x, cellPos.y]}");

        if (tilesCapacity[cellPos.x, cellPos.y] > maxCapacity)
        {
            float fogToDistribute = tilesCapacity[cellPos.x, cellPos.y] - maxCapacity;
            tilesCapacity[cellPos.x, cellPos.y] = 1f;


            // if (fogToDistribute > 0f)
            // {
                // if (prevs == null)
                // {
                //     // prevs = new HashSet<int2>();
                // }
            int2[] neighbours = GetNeighbours(current, prevs);
            if (neighbours.Length <= 0)
            {
                return;
            }

            int smallest = 0;

            for (int i = 0; i < neighbours.Length; i++)
            {
                if (
                    tilesCapacity[neighbours[i].x, neighbours[i].y] <
                    tilesCapacity[neighbours[smallest].x, neighbours[smallest].y]
                )
                {
                    smallest = i;
                }
            }

            AddFog(
                new Vector3Int(
                    neighbours[smallest].x,
                    neighbours[smallest].y,
                    0
                ),
                fogToDistribute
            );

            // }
        }
        else
        {
            UpdateTiles(prevs);
            prevs.Clear();
        }
    }

    void UpdateTiles(HashSet<int2> updated)
    {
        // Debug.Log($"Requested to update {updated.Count} tiles");
        foreach (var pos in updated)
        {
            float amount = tilesCapacity[pos.x, pos.y];

            if (amount <= 0f)
            {
                fogTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
            }
            else if (amount <= 0.1f)
            {
                fogTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), fogTiles[0]);
            }
            else if (amount <= 0.2f)
            {
                fogTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), fogTiles[1]);
            }
            else if (amount <= 0.4f)
            {
                fogTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), fogTiles[2]);
            }
            else if (amount <= 0.6f)
            {
                fogTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), fogTiles[3]);
            }
            else if (amount <= 0.8f)
            {
                fogTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), fogTiles[4]);
            }
            else if (amount >= 1f)
            {
                fogTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), fogTiles[5]);
            }

        }
        prevs.Clear();
    }

    void DegradeFog()
    {
        HashSet<int2> updated = new HashSet<int2>();
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                if (tilesCapacity[i, j] <= 0f)
                {
                    continue;
                }

                tilesCapacity[i, j] = Mathf.Max(0f, tilesCapacity[i, j] - loosePerTick);
                updated.Add(new int2(i, j));
            }
        }
        UpdateTiles(updated);
    }

    void Update()
    {
        _currentTime -= Time.deltaTime;

        if (_currentTime <= 0f)
        {
            DegradeFog();
            _currentTime = tickTime;
        }

    }
}