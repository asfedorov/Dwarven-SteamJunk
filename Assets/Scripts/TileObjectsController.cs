using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


[System.Serializable]
public struct Sigil
{
    public string name;
    public string pattern;
    public GameObject prefab;
}


public class TileObjectsController : MonoBehaviour
{
    struct DirtLivingTile{
        public Vector3Int pos;
        public float birthtime;
    }

    Dictionary<Vector3Int, GameObject> tilesToObjectMapping = new Dictionary<Vector3Int, GameObject>();
    Vector3Int power;

    Dictionary<Vector3Int, GameObject> towers = new Dictionary<Vector3Int, GameObject>();
    Dictionary<Vector3Int, GameObject> factories = new Dictionary<Vector3Int, GameObject>();


    List<Vector3Int> ocupiedTiles = new List<Vector3Int>();
    // List<Vector3Int> towers = new List<Vector3Int>();

    List<List<Vector3Int>> powerLines = new List<List<Vector3Int>>();

    public TileManager tileManager;


    Queue<DirtLivingTile> dirtQueue = new Queue<DirtLivingTile>();
    public float dirtTTL = 10;

    public Sigil[] availableSigils;
    public Dictionary<Vector3Int, Sigil> registeredSigils = new Dictionary<Vector3Int, Sigil>();
    public Dictionary<Vector3Int, GameObject> registeredSigilsObj = new Dictionary<Vector3Int, GameObject>();

    public float dirtCheckFreq = 2f;
    float _currentDurtCheck = 2f;

    public GameObject world;

    public AudioSource globalEffects;
    public AudioClip onSigilCreated;

    public AppController appController;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _currentDurtCheck -= Time.deltaTime;
        if (_currentDurtCheck <= 0)
        {
            CheckDirtToDie();
            _currentDurtCheck = dirtCheckFreq;
        }
    }


    public bool IsOccupied(Vector3Int cellPos)
    {
        return tilesToObjectMapping.ContainsKey(cellPos) || ocupiedTiles.Contains(cellPos);
    }


    public void RegisterObjectOnTile(Vector3Int cellPos, GameObject obj)
    {
        if (tilesToObjectMapping.ContainsKey(cellPos))
        {
            return;
        }
        tilesToObjectMapping.Add(cellPos, obj);
    }

    public void TileDestroyed(Vector3Int cellPos)
    {
        if (tilesToObjectMapping.ContainsKey(cellPos))
        {
            DestroyableController dc = tilesToObjectMapping[cellPos].GetComponent<DestroyableController>();
            if (dc != null)
            {
                if (dc.GetHit())
                {
                    tilesToObjectMapping.Remove(cellPos);

                    appController.score.BuildingsDestroyed += 1;
                }
            }
            else
            {
                Destroy(tilesToObjectMapping[cellPos]);
                tilesToObjectMapping.Remove(cellPos);
                appController.score.TrapsDestroyed += 1;
            }
        }

        for( int i = powerLines.Count - 1; i >=0; i--)
        {
            var line = powerLines[i];
            if (line.Contains(cellPos))
            {
                foreach(var keyVal in towers)
                {
                    if (line.Contains(keyVal.Key))
                    {
                        keyVal.Value.GetComponent<TowerController>().PowerOff();
                    }
                }
                foreach(var keyVal in factories)
                {
                    if (line.Contains(keyVal.Key))
                    {
                        keyVal.Value.GetComponent<FactoryController>().PowerOff();
                    }
                }
                powerLines.RemoveAt(i);
                appController.score.PowerLinesDestroyed += 1;
            }
        }
    }

    public void RegisterPower(Vector3Int cellPos)
    {
        power = cellPos;
        ocupiedTiles.Add(cellPos);
        ocupiedTiles.Add(cellPos + Vector3Int.left);
        ocupiedTiles.Add(cellPos + Vector3Int.up);
        ocupiedTiles.Add(cellPos + Vector3Int.right);
        ocupiedTiles.Add(cellPos + Vector3Int.down);
        ocupiedTiles.Add(cellPos + Vector3Int.left + Vector3Int.up);
        ocupiedTiles.Add(cellPos + Vector3Int.up + Vector3Int.right);
        ocupiedTiles.Add(cellPos + Vector3Int.right + Vector3Int.down);
        ocupiedTiles.Add(cellPos + Vector3Int.down + Vector3Int.left);
    }

    public void RegisterTower(Vector3Int cellPos, GameObject tower)
    {
        towers.Add(cellPos, tower);
        ocupiedTiles.Add(cellPos);
        ocupiedTiles.Add(cellPos + Vector3Int.left);
        ocupiedTiles.Add(cellPos + Vector3Int.up);
        ocupiedTiles.Add(cellPos + Vector3Int.right);
        ocupiedTiles.Add(cellPos + Vector3Int.down);
        ocupiedTiles.Add(cellPos + Vector3Int.left + Vector3Int.up);
        ocupiedTiles.Add(cellPos + Vector3Int.up + Vector3Int.right);
        ocupiedTiles.Add(cellPos + Vector3Int.right + Vector3Int.down);
        ocupiedTiles.Add(cellPos + Vector3Int.down + Vector3Int.left);
    }

    public void RegisterFactory(Vector3Int cellPos, GameObject tower)
    {
        factories.Add(cellPos, tower);
        ocupiedTiles.Add(cellPos);
        ocupiedTiles.Add(cellPos + Vector3Int.left);
        ocupiedTiles.Add(cellPos + Vector3Int.up);
        ocupiedTiles.Add(cellPos + Vector3Int.right);
        ocupiedTiles.Add(cellPos + Vector3Int.down);
        ocupiedTiles.Add(cellPos + Vector3Int.left + Vector3Int.up);
        ocupiedTiles.Add(cellPos + Vector3Int.up + Vector3Int.right);
        ocupiedTiles.Add(cellPos + Vector3Int.right + Vector3Int.down);
        ocupiedTiles.Add(cellPos + Vector3Int.down + Vector3Int.left);
    }

    public void RegisterPowerline(List<Vector3Int> powerline)
    {
        powerLines.Add(powerline);
    }

    public Vector3Int GetPower()
    {
        return power;
    }

    public void RegisterDirt(Vector3Int cellPos)
    {
        dirtQueue.Enqueue(
            new DirtLivingTile {
                pos = cellPos,
                birthtime = Time.time
            }
        );

        Vector3Int sigilPos;
        if (CheckIfSigilUpdated(cellPos, out sigilPos))
        {
            DestroySigil(sigilPos);
        }
        else
        {
            CreateSigil(cellPos);
        }
    }

    void CheckDirtToDie()
    {
        if (dirtQueue.Count <= 0)
        {
            return;
        }
        DirtLivingTile dt = dirtQueue.Peek();
        if (dt.birthtime + dirtTTL < Time.time)
        {
            dirtQueue.Dequeue();

            Vector3Int sigilPos;
            if (CheckIfSigilUpdated(dt.pos, out sigilPos))
            {
                DestroySigil(sigilPos);
            }

            tileManager.RemoveDirt(dt.pos);
        }
    }

    public bool CheckIfSigilUpdated(Vector3Int cellPos, out Vector3Int sigilPos)
    {
        int minX;
        int minY;
        List<Vector3Int> connected = tileManager.FindConnectedDirt(cellPos, out minX, out minY);
        foreach (var registeredSigil in registeredSigils.Keys)
        {
            if (connected.Contains(registeredSigil))
            {
                sigilPos = registeredSigil;
                return true;
                // DestroySigil(registeredSigil);
            }
        }

        sigilPos = cellPos;
        return false;
    }

    bool CreateSigil(Vector3Int cellPos)
    {
        int minX;
        int minY;
        List<Vector3Int> connected = tileManager.FindConnectedDirt(cellPos, out minX, out minY);

        Vector3Int figurePos;
        var figure = FindFigure(connected, minX, minY, out figurePos);

        foreach (var sigil in availableSigils)
        {
            if (figure == sigil.pattern)
            {
                registeredSigils.Add(figurePos, sigil);
                registeredSigilsObj.Add(figurePos, tileManager.SetObjectAtPos(figurePos, sigil.prefab));

                globalEffects.clip = onSigilCreated;
                globalEffects.Play();

                return true;
            }
        }
        return false;
    }


    void DestroySigil(Vector3Int cellPos)
    {
        Destroy(registeredSigilsObj[cellPos]);
        registeredSigilsObj.Remove(cellPos);
        registeredSigils.Remove(cellPos);
    }

    public string FindFigure(List<Vector3Int> connected, int minX, int minY, out Vector3Int figurePos)
    {

        Vector3Int[] query = connected.OrderBy(val => val.y).ThenBy(val => val.x).ToArray();

        string result = "";
        foreach(var val in query)
        {
            result += $"{val.x - minX}" + $"{val.y - minY}";
        }

        figurePos = query[0];

        // Debug.Log($"Found figure: {result}");
        return result;
    }
}
