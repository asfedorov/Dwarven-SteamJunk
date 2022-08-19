using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using WPTest.Scripts;
using Random = UnityEngine.Random;
using RandomM = Unity.Mathematics.Random;


public struct WPTile
{
    public Dictionary<TilePreset, List<TileDirection>> availableTilesAndDirections;
    public HashSet<TileVariant> availableVariants;

    public string[] connections;

    public TilePreset chosenPreset;
    public TileDirection chosenDirection;
    public bool inited;
    public int variantCount;
    public bool nullTile;
    public int3 position;
}

public struct TileVariant
{
    public TilePreset preset;
    // public TileDirection rotation;
}

[Serializable]
public struct DirtMask
{
    public string mask;
    public Tile tile;
}

public class TileManager : MonoBehaviour
{
    [SerializeField] private TilePreset[] AllTilePresets;
    [SerializeField] public int3 MapSize;
    [SerializeField] Tilemap[] tilemaps;
    [SerializeField] Tilemap[] treeAndPropsTilemaps;
    [SerializeField] Tile[] levelBackgroundTile;
    [SerializeField] Tile[] trees;
    [SerializeField] Tile[] bushes;
    [SerializeField] Tile[] flowers;
    [SerializeField] TilePreset waterPreset;
    [SerializeField] Tile[] dirtTiles;
    [SerializeField] Tile powerLineTile;

    [SerializeField] Grid grid;
    [SerializeField] Grid gridProps;

    public DirtMask[] dirtMasks;

    public GameObject debug;

    public GameObject layerDebug;
    public Text dt0;
    public Text dt1;
    public Text dt2;
    public Text dt3;

    private WPTile[,,] _map;
    private Queue<int3> _updateNeighboursQueue = new Queue<int3>();
    private HashSet<int3> _updatedNeighbours = new HashSet<int3>();

    public GameObject towerPrefab;
    public Transform world;
    public int towerCount = 5;

    public GameObject trapPrefab;
    public int trapCount = 25;

    public int factoriesCount = 7;
    public GameObject[] factoriesPrefabs;

    public GameObject powerPrefab;

    public TileObjectsController toController;

    Dictionary<TileVariant, HashSet<TileVariant>[]> _precomputedVariantExclusion = new Dictionary<TileVariant, HashSet<TileVariant>[]>();

    int[,][] tilesLayers;

    public bool Initialized = false;

    public AppController appController;

    RandomM rnd;

    // Start is called before the first frame update
    void Start()
    {
        // rnd = new RandomM((uint)Random.Range(14, 164824819));
    }

    public void StartGen()
    {
        PreComputeExclusion();
        Generate();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void PreComputeExclusion()
    {
        foreach (var preset in AllTilePresets)
        {
            // foreach(var rotation in preset.PossibleDirections)
            // {
                TileVariant variant = new TileVariant(){
                    preset = preset,
                    // rotation = rotation
                };

                HashSet<TileVariant>[] excluded = new HashSet<TileVariant>[4];
                excluded[0] = new HashSet<TileVariant>();
                excluded[1] = new HashSet<TileVariant>();
                excluded[2] = new HashSet<TileVariant>();
                excluded[3] = new HashSet<TileVariant>();
                // excluded[4] = new HashSet<TileVariant>();
                // excluded[5] = new HashSet<TileVariant>();

                for (int i = 0; i < 4; i++)
                {
                    int checkDirectionIndex = i + 2;
                    if (checkDirectionIndex >= 4)
                    {
                        checkDirectionIndex -= 4;
                    }

                    foreach (var checkPreset in AllTilePresets)
                    {
                        // foreach (var checkRotation in checkPreset.PossibleDirections)
                        // {
                            // var thisTileConnections = thisTile.connections[i];

                            if (i < 4)
                            {
                                var thisTileConnections = checkPreset.GetConnections()[i];
                                // if (checkedConnections.Contains(thisTileConnections))
                                // {
                                //     continue;
                                // }

                                if (!preset.CanConnectByDirection(
                                    thisTileConnections,
                                    (TileDirection)checkDirectionIndex
                                ))
                                {
                                    excluded[i].Add(new TileVariant(){ preset = checkPreset });
                                }

                            }
                            // else
                            // {
                            //     int vi = i - 4;

                            //     int checkVerticalIndex = vi + 1;
                            //     if (checkVerticalIndex >= 2)
                            //     {
                            //         checkVerticalIndex -= 2;
                            //     }

                            //     var thisTileVerticalConnections = checkPreset.GetVerticalConnections()[vi];

                            //     if (!preset.CanConnectByVerticalDirection(
                            //         thisTileVerticalConnections,
                            //         (TileVerticalDirection)checkVerticalIndex
                            //     ))
                            //     {
                            //         excluded[i].Add(new TileVariant(){ preset = checkPreset });
                            //     }

                            // }
                        // }
                    }
                }

                _precomputedVariantExclusion.Add(variant, excluded);
            // }
        }
    }

    WPTile GetTileAtPosition(int3 position)
    {
        if (
            position.x >= 0 &&
            position.x < _map.GetLength(0) &&
            position.y >= 0 &&
            position.y < _map.GetLength(1) &&
            position.z >= 0 &&
            position.z < _map.GetLength(2)
        )
        {
            return _map[position.x, position.y, position.z];
        }

        return new WPTile() { nullTile = true };
    }

    WPTile[] GetNeighbours(int3 pos)
    {
        return new WPTile[]
        {
            GetTileAtPosition(pos - new int3(1, 0, 0)),
            GetTileAtPosition(pos + new int3(0, 1, 0)),
            GetTileAtPosition(pos + new int3(1, 0, 0)),
            GetTileAtPosition(pos - new int3(0, 1, 0))
            // GetTileAtPosition(pos + new int3(0, 0, 1)),
            // GetTileAtPosition(pos - new int3(0, 0, 1)),
        };
    }

    void Generate()
    {
        rnd = new RandomM((uint)Random.Range(14, 164824819));
        while (presetsTilesQueue.Count > 0)
        {
            StopCoroutine(presetsTilesQueue.Dequeue());
        }
        foreach(var tm in tilemaps)
        {
            tm.ClearAllTiles();
        }
        foreach(var tm in treeAndPropsTilemaps)
        {
            tm.ClearAllTiles();
        }

        _map = new WPTile[MapSize.x, MapSize.y, MapSize.z];
        tilesLayers = new int[MapSize.x,MapSize.y][];
        for (int x = 0; x < MapSize.x; x++)
        {
            for (int y = 0; y < MapSize.y; y++)
            {
                for (int z = 0; z < MapSize.z; z++)
                {
                    WPTile tile = _map[x, y, z];
                    // tile.availableTilesAndDirections = new Dictionary<TilePreset, List<TileDirection>>();
                    tile.availableVariants = new HashSet<TileVariant>(_precomputedVariantExclusion.Keys);
                    foreach (var preset in AllTilePresets)
                    {
                        // tile.availableTilesAndDirections[preset] = new List<TileDirection>(preset.PossibleDirections);
                        tile.variantCount++;
                        tile.position = new int3(x, y, z);
                    }

                    _map[x, y, z] = tile;
                    // Debug.Log(tile.availableVariants.Count() + " : " + tile.variantCount);
                }
            }
        }

        for (int i = 0; i < MapSize.x; i++)
        {
            presetsTilesQueue.Enqueue(
                SetRandom(
                    new int3(i, 0, 0),
                    false,
                    true
                )
            );
            presetsTilesQueue.Enqueue(
                SetRandom(
                    new int3(i, MapSize.y - 1, 0),
                    false,
                    true
                )
            );
        }

        for (int i = 0; i < MapSize.y; i++)
        {
            presetsTilesQueue.Enqueue(
                SetRandom(
                    new int3(0, i, 0),
                    false,
                    true
                )
            );
            presetsTilesQueue.Enqueue(
                SetRandom(
                    new int3(MapSize.x - 1, i, 0),
                    false,
                    true
                )
            );
        }

        for (int i = -5; i < 0; i++)
        {
            for (int j = -5; j < MapSize.y + 5; j++)
            {
                tilemaps[0].SetTile(
                    new Vector3Int(
                        i, j, 0
                    ),
                    levelBackgroundTile[0]
                );
                // tilesLayers[i,j] = new int[] {0, 0, 0, 0};
            }
        }

        for (int i = MapSize.x; i < MapSize.x + 5; i++)
        {
            for (int j = -5; j < MapSize.y + 5; j++)
            {
                tilemaps[0].SetTile(
                    new Vector3Int(
                        i, j, 0
                    ),
                    levelBackgroundTile[0]
                );
                // tilesLayers[i,j] = new int[] {0, 0, 0, 0};
            }
        }

        for (int i = -5; i < 0; i++)
        {
            for (int j = -5; j < MapSize.x + 5; j++)
            {
                tilemaps[0].SetTile(
                    new Vector3Int(
                        j, i, 0
                    ),
                    levelBackgroundTile[0]
                );
                // tilesLayers[j,i] = new int[] {0, 0, 0, 0};
            }
        }

        for (int i = MapSize.y; i < MapSize.y + 5; i++)
        {
            for (int j = -5; j < MapSize.x + 5; j++)
            {
                tilemaps[0].SetTile(
                    new Vector3Int(
                        j, i, 0
                    ),
                    levelBackgroundTile[0]
                );
                // tilesLayers[j,i] = new int[] {0, 0, 0, 0};
            }
        }



        // int rndX = Random.Range(1, MapSize.x - 1);
        // int rndY = Random.Range(1, MapSize.y - 1);
        int rndZ = 0;

        int rndX = rnd.NextInt(1, MapSize.x - 1);
        int rndY = rnd.NextInt(1, MapSize.y - 1);


        presetsTilesQueue.Enqueue(SetRandom(new int3(rndX, rndY, rndZ)));

        StartCoroutine(PresettingTilesQueue());
    }

    Queue<IEnumerator> presetsTilesQueue = new Queue<IEnumerator>();
    IEnumerator PresettingTilesQueue()
    {
        yield return new WaitUntil(() => presetsTilesQueue.Count() > 0);

        while (presetsTilesQueue.Count() > 0)
        {

            var f = presetsTilesQueue.Dequeue();
            yield return f;
        }

        yield return SetTrees();
    }

    IEnumerator SetRandom(int3 tilePos, bool recursionUpdate = true, bool fillWithWater = false)
    {
        WPTile rndTile = _map[tilePos.x, tilePos.y, tilePos.z];

        int keysCount = rndTile.availableVariants.Count;
        if (keysCount > 0)
        {
            TileVariant chosenVariant;
            if (fillWithWater)
            {
                chosenVariant = new TileVariant {preset = waterPreset};
            }
            else
            {
                List<TileVariant> weightedVariants = new List<TileVariant>();
                foreach(var variant in rndTile.availableVariants)
                {
                    for(int i = 0; i < variant.preset.probability; i++)
                    {
                        weightedVariants.Add(variant);
                    }

                }
                // int rndVariant = Random.Range(0, weightedVariants.Count);
                int rndVariant = rnd.NextInt(0, weightedVariants.Count);


                chosenVariant = weightedVariants.ElementAt(rndVariant);
            }


            rndTile.chosenPreset = chosenVariant.preset;


            rndTile.connections = rndTile.chosenPreset.GetConnections();

            rndTile.inited = true;
            rndTile.variantCount = 1;
            rndTile.availableVariants.Clear();
            rndTile.availableVariants.Add(chosenVariant);

            if (
                lowestPosition.x == rndTile.position.x &&
                lowestPosition.y == rndTile.position.y &&
                lowestPosition.z == rndTile.position.z
            )
            {
                lowestPosition = new int3(-1, -1, -1);
                lowestVariants = 999;
            }

            tilemaps[rndTile.chosenPreset.level].SetTile(
                new Vector3Int(
                    tilePos.x, tilePos.y, tilePos.z
                ),
                rndTile.chosenPreset.Prefab
            );
            tilesLayers[tilePos.x, tilePos.y] = new int[] {
                Convert.ToInt32(rndTile.chosenPreset.Connections[0][0]) - 48,
                Convert.ToInt32(rndTile.chosenPreset.Connections[1][0]) - 48,
                Convert.ToInt32(rndTile.chosenPreset.Connections[2][0]) - 48,
                Convert.ToInt32(rndTile.chosenPreset.Connections[3][0]) - 48
            };
            if (rndTile.chosenPreset.level > 0)
            {
                tilemaps[rndTile.chosenPreset.level - 1].SetTile(
                    new Vector3Int(
                        tilePos.x, tilePos.y, tilePos.z
                    ),
                    levelBackgroundTile[rndTile.chosenPreset.level - 1]
                );

            }
        }
        else
        {
            Debug.Log($"Null tile at position {tilePos}!");

            rndTile.nullTile = true;

            Generate();
            yield break;
        }

        _map[tilePos.x, tilePos.y, tilePos.z] = rndTile;

        AddTileToUpdateNeighbours(tilePos);

        // yield return null;
        yield return UpdateNeighbours();

        // yield return null;

        if (recursionUpdate)
        {
            FindLowestAndSetRandom();
        }
    }

    IEnumerator UpdateNeighbours()
    {
        // int iter = 0;
        while (_updateNeighboursQueue.Count > 0)
        {
            // if (iter > 4)
            // {
            //     break;
            // }

            yield return UpdateNeighboursAtPosition(_updateNeighboursQueue.Dequeue());
            // iter++;
        }
        _updateNeighboursQueue.Clear();
        _updatedNeighbours.Clear();
    }

    int3 lowestPosition = new int3(-1, -1, -1);
    int lowestVariants = 999;

    bool FindLowestAndSetRandom()
    {
        // int3 lowestPosition = new int3(-1, -1, -1);
        // int3 checkLowest = new int3(-1, -1, -1);
        // int lowestVariants = 999;

        if (lowestPosition.x < 0)
        {
            foreach (var tile in _map)
            {
                if (tile.inited || tile.nullTile)
                {
                    continue;
                }

                if (tile.variantCount < lowestVariants)
                {
                    lowestPosition = tile.position;
                    lowestVariants = tile.variantCount;
                }
            }

        }

        if (GetTileAtPosition(lowestPosition).nullTile)
        {
            return false;
        }
        // Debug.Log($"Lowest variants: {lowestVariants}");
        StartCoroutine(SetRandom(lowestPosition));
        return true;

    }

    void AddTileToUpdateNeighbours(int3 tilePos)
    {
        if (_updatedNeighbours.Contains(tilePos))
        {
            return;
        }

        if (_updateNeighboursQueue.Contains(tilePos))
        {
            return;
        }
        _updateNeighboursQueue.Enqueue(tilePos);
    }

    IEnumerator UpdateNeighboursAtPosition(int3 position)
    {
        var thisTile = GetTileAtPosition(position);
        if (thisTile.nullTile)
        {
            _updatedNeighbours.Add(position);
            yield break;
        }
        var neighbours = GetNeighbours(position);
        for (int i = 0; i < neighbours.Length; i++)
        {
            var n = neighbours[i];
            if (n.nullTile || n.inited)
            {
                continue;
            }

            if (_updatedNeighbours.Contains(n.position))
            {
                continue;
            }

            // if (debug != null)
            // {
            //     debug.transform.position = new Vector3(n.position.x * 1.125f, n.position.y * 1.125f, n.position.z * 1.125f);
            // }

            int lastVariantsCount = n.variantCount;

            // var updateTilesAndDirections = new Dictionary<TilePreset, List<TileDirection>>();
            n.variantCount = 0;

            HashSet<TileVariant> toExclude = new HashSet<TileVariant>(_precomputedVariantExclusion.Keys);

            int checkDirectionIndex = i + 2;
            if (checkDirectionIndex >= 4)
            {
                checkDirectionIndex -= 4;
            }

            // int vi = i - 4;

            // int checkVerticalIndex = vi + 1;
            // if (checkVerticalIndex >= 2)
            // {
            //     checkVerticalIndex -= 2;
            // }

            foreach (var variant in thisTile.availableVariants)
            {
                if (i < 4)
                {
                    toExclude.IntersectWith(_precomputedVariantExclusion[variant][checkDirectionIndex]);
                }
                // else
                // {
                //     toExclude.IntersectWith(_precomputedVariantExclusion[var][4 + checkVerticalIndex]);
                // }
            }

            // Debug.Log("pos: " + position + " excl - " + n.availableVariants.Count() + " : " + toExclude.Count());

            if (toExclude.Count() > 0)
            {
                n.availableVariants.ExceptWith(toExclude);
                n.variantCount = n.availableVariants.Count;

                if (n.variantCount < lowestVariants)
                {
                    lowestVariants = n.variantCount;
                    lowestPosition = n.position;
                }
            }


            // n.availableTilesAndDirections = updateTilesAndDirections;
            _map[n.position.x, n.position.y, n.position.z] = n;

            if (lastVariantsCount != n.variantCount)
            {
                // Debug.Log("pos: " + position + " variants - " + lastVariantsCount + " : " + n.variantCount);
                AddTileToUpdateNeighbours(n.position);
            }

            // yield return null;
        }

        _updatedNeighbours.Add(position);
    }

    // void OnDrawGizmos()
    // {
    //     foreach(var n in _updatedNeighbours)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawSphere(new Vector3(n.x * 1.125f, n.y * 1.125f, n.z * 1.125f), 0.33f);
    //     }
    // }

    public GameObject SetObjectAtPos(Vector3Int cellPos, GameObject prefab)
    {
        var go = Instantiate(prefab, gridProps.CellToWorld(cellPos), Quaternion.identity, world);
        return go;
    }

    bool SetPower()
    {
        int sizeX = (int)(MapSize.x * 7 / 3);
        int sizeY = (int)(MapSize.y * 7 / 3);

        int powerSet = 0;

        int maxTries = 300;

        while (powerSet < 1 && maxTries > 0)
        {
            int i = rnd.NextInt(0, sizeX);
            int j = rnd.NextInt(0, sizeY);
            // int i = Random.Range(0, sizeX);
            // int j = Random.Range(0, sizeY);

            if (!CanBuildOnPos(i, j))
            {
                maxTries -= 1;
                continue;
            }

            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i+1,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i+1,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j + 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j + 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i+1,j + 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i + 1,j + 1,0), null);
            }
            // if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j + 2,0)))
            // {
            //     treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j + 2,0), null);
            // }
            // if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i+1,j + 2,0)))
            // {
            //     treeAndPropsTilemaps[0].SetTile(new Vector3Int(i + 1,j + 2,0), null);
            // }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i-1,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i-1,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j - 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j - 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i-1,j - 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i - 1,j - 1,0), null);
            }

            var pos = new Vector3Int(i,j,0);

            var go = Instantiate(powerPrefab, gridProps.CellToWorld(pos), Quaternion.identity, world);
            toController.RegisterObjectOnTile(pos, go);

            toController.RegisterPower(pos);

            go.GetComponent<PowerController>().selfPos = pos;

            appController.score.BuildingsLeft += 1;

            powerSet++;

            return true;
        }

        return false;
    }

    bool CanBuildOnPos(int x, int y)
    {
        int groundX = (int)(x * 3 / 7);
        int groundY = (int)(y * 3 / 7);

        int layer = GetCellLayer(new Vector3Int(groundX, groundY, 0));
        if (layer != 1)
        {
            return false;
        }
        if (tilemaps[1].GetTile(new Vector3Int(groundX, groundY, 0)) != levelBackgroundTile[1])
        {
            return false;
        }
        if (
            tilemaps[2].HasTile(new Vector3Int(groundX, groundY+1, 0)) ||
            tilemaps[1].GetTile(new Vector3Int(groundX, groundY+1, 0)) != levelBackgroundTile[1]
        )
        {
            return false;
        }

        return !toController.IsOccupied(new Vector3Int(x, y, 0));
    }

    void SetFactories()
    {
        int sizeX = (int)(MapSize.x * 7 / 3);
        int sizeY = (int)(MapSize.y * 7 / 3);

        int factoriesSet = 0;

        int maxTries = 100;

        int rangeIncrease = 0;

        while (factoriesSet < factoriesCount && maxTries > 0)
        {
            if (maxTries < 50)
            {
                rangeIncrease = 15;
            }
            // int i = Random.Range(
            //     Mathf.Max(0, toController.GetPower().x - (15 + rangeIncrease)),
            //     Mathf.Min(sizeX, toController.GetPower().x + (15 + rangeIncrease))
            // );
            // int j = Random.Range(
            //     Mathf.Max(0, toController.GetPower().y - (15 + rangeIncrease)),
            //     Mathf.Min(sizeY, toController.GetPower().y + (15 + rangeIncrease))
            // );
            int i = rnd.NextInt(
                Mathf.Max(0, toController.GetPower().x - (15 + rangeIncrease)),
                Mathf.Min(sizeX, toController.GetPower().x + (15 + rangeIncrease))
            );
            int j = rnd.NextInt(
                Mathf.Max(0, toController.GetPower().y - (15 + rangeIncrease)),
                Mathf.Min(sizeY, toController.GetPower().y + (15 + rangeIncrease))
            );

            if (!CanBuildOnPos(i, j))
            {
                maxTries -= 1;
                continue;
            }

            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i+1,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i+1,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j + 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j + 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i+1,j + 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i + 1,j + 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i-1,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i-1,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j - 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j - 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i-1,j - 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i - 1,j - 1,0), null);
            }

            var pos = new Vector3Int(i,j,0);

            int factory = rnd.NextInt(0, factoriesPrefabs.Length);
            // int factory = Random.Range(0, factoriesPrefabs.Length);
            var go = Instantiate(factoriesPrefabs[factory], gridProps.CellToWorld(pos), Quaternion.identity, world);
            toController.RegisterObjectOnTile(pos, go);

            List<Vector3Int> powerline = GetPath(
                toController.GetPower(),
                pos
            );

            if (powerline.Count() > 0)
            {
                foreach(var v in powerline)
                {
                    treeAndPropsTilemaps[0].SetTile(v, powerLineTile);
                }
                toController.RegisterPowerline(powerline);
                appController.score.PowerLinesLeft += 1;
                go.GetComponent<FactoryController>().PowerOn();
            }

            go.GetComponent<FactoryController>().selfPos = pos;

            toController.RegisterFactory(pos, go);

            appController.score.BuildingsLeft += 1;

            factoriesSet++;
        }
    }

    void SetTowers()
    {
        int sizeX = (int)(MapSize.x * 7 / 3);
        int sizeY = (int)(MapSize.y * 7 / 3);

        int towersSet = 0;

        int maxTries = 100;

        int rangeIncrease = 0;
        while (towersSet < towerCount && maxTries > 0)
        {
            if (maxTries < 50)
            {
                rangeIncrease = 15;
            }
            // int i = Random.Range(
            //     Mathf.Max(0, toController.GetPower().x - (20+rangeIncrease)),
            //     Mathf.Min(sizeX, toController.GetPower().x + (20+rangeIncrease))
            // );
            // int j = Random.Range(
            //     Mathf.Max(0, toController.GetPower().y - (20+rangeIncrease)),
            //     Mathf.Min(sizeY, toController.GetPower().y + (20+rangeIncrease))
            // );
            int i = rnd.NextInt(
                Mathf.Max(0, toController.GetPower().x - (20+rangeIncrease)),
                Mathf.Min(sizeX, toController.GetPower().x + (20+rangeIncrease))
            );
            int j = rnd.NextInt(
                Mathf.Max(0, toController.GetPower().y - (20+rangeIncrease)),
                Mathf.Min(sizeY, toController.GetPower().y + (20+rangeIncrease))
            );

            if (!CanBuildOnPos(i, j))
            {
                maxTries -= 1;
                continue;
            }

            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i+1,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i+1,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j + 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j + 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i+1,j + 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i + 1,j + 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i-1,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i-1,j,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j - 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j - 1,0), null);
            }
            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i-1,j - 1,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i - 1,j - 1,0), null);
            }

            var pos = new Vector3Int(i,j,0);

            var go = Instantiate(towerPrefab, gridProps.CellToWorld(pos), Quaternion.identity, world);
            toController.RegisterObjectOnTile(pos, go);

            List<Vector3Int> powerline = GetPath(
                toController.GetPower(),
                pos
            );

            if (powerline.Count() > 0)
            {
                foreach(var v in powerline)
                {
                    treeAndPropsTilemaps[0].SetTile(v, powerLineTile);
                }
                toController.RegisterPowerline(powerline);
                go.GetComponent<TowerController>().PowerOn();
            }

            toController.RegisterTower(pos, go);

            appController.score.BuildingsLeft += 1;

            towersSet++;
        }
    }

    void SetTraps()
    {
        int sizeX = (int)(MapSize.x * 7 / 3);
        int sizeY = (int)(MapSize.y * 7 / 3);

        int trapSet = 0;

        int maxTries = 30;

        while (trapSet < trapCount)
        {
            // int i = Random.Range(0, sizeX);
            // int j = Random.Range(0, sizeY);
            int i = rnd.NextInt(0, sizeX);
            int j = rnd.NextInt(0, sizeY);

            int groundX = (int)(i * 3 / 7);
            int groundY = (int)(j * 3 / 7);

            int layer = GetCellLayer(new Vector3Int(groundX, groundY, 0));
            if (layer != 1)
            {
                maxTries -= 1;
                continue;
            }
            if (tilemaps[1].GetTile(new Vector3Int(groundX, groundY, 0)) != levelBackgroundTile[1])
            {
                maxTries -= 1;
                continue;
            }

            if (treeAndPropsTilemaps[0].HasTile(new Vector3Int(i,j,0)))
            {
                treeAndPropsTilemaps[0].SetTile(new Vector3Int(i,j,0), null);
            }


            var go = Instantiate(trapPrefab, gridProps.CellToWorld(new Vector3Int(i,j,0)), Quaternion.identity, world);
            toController.RegisterObjectOnTile(new Vector3Int(i,j,0), go);
            appController.score.TrapsLeft += 1;
            trapSet++;
        }
    }

    IEnumerator SetTrees()
    {
        int sizeX = (int)(MapSize.x * 7 / 3);
        int sizeY = (int)(MapSize.y * 7 / 3);
        float scale = 0.020f;

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {

                int groundX = (int)(i * 3 / 7);
                int groundY = (int)(j * 3 / 7);

                yield return new WaitUntil(
                    () => tilemaps[0].HasTile(new Vector3Int(groundX, groundY, 0)) ||
                        tilemaps[1].HasTile(new Vector3Int(groundX, groundY, 0)) ||
                        tilemaps[2].HasTile(new Vector3Int(groundX, groundY, 0))
                );

                int treeLevel = 0;

                if (tilemaps[2].HasTile(new Vector3Int(groundX, groundY, 0)))
                {
                    if (tilemaps[2].GetTile(new Vector3Int(groundX, groundY, 0)) != levelBackgroundTile[2])
                    {
                        continue;
                    }
                    treeLevel = 1;
                }
                else if (tilemaps[1].HasTile(new Vector3Int(groundX, groundY, 0)))
                {
                    if (tilemaps[1].GetTile(new Vector3Int(groundX, groundY, 0)) != levelBackgroundTile[1])
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }


                float xCoord = i / MapSize.x * scale;
                float yCoord = j / MapSize.y * scale;
                float treeSample = Mathf.PerlinNoise(xCoord, yCoord);
                float bushSample = Mathf.PerlinNoise(xCoord + 500f, yCoord);
                float flowerSample = Mathf.PerlinNoise(xCoord, yCoord + 500f);

                if (rnd.NextFloat(0f, 4f) <= treeSample)
                // if (Random.Range(0f, 4f) <= treeSample)
                {
                    treeAndPropsTilemaps[treeLevel].SetTile(
                        new Vector3Int(
                            i, j, 0
                        ),
                        trees[rnd.NextInt(0, trees.Length)]
                        // trees[Random.Range(0, trees.Length)]
                    );
                }
                if (rnd.NextFloat(0f, 4f) <= bushSample)
                // if (Random.Range(0f, 4f) <= bushSample)
                {
                    treeAndPropsTilemaps[treeLevel].SetTile(
                        new Vector3Int(
                            i, j, 0
                        ),
                        bushes[rnd.NextInt(0, bushes.Length)]
                        // bushes[Random.Range(0, bushes.Length)]
                    );
                }
                if (rnd.NextFloat(0f, 4f) <= flowerSample)
                // if (Random.Range(0f, 4f) <= flowerSample)
                {
                    treeAndPropsTilemaps[treeLevel].SetTile(
                        new Vector3Int(
                            i, j, 0
                        ),
                        flowers[rnd.NextInt(0, flowers.Length)]
                        // flowers[Random.Range(0, flowers.Length)]
                    );
                }

            }
        }
        // yield return null;

        CompositeCollider2D[] colliders = GameObject.FindObjectsOfType<CompositeCollider2D>();

        for(int i = 0; i < colliders.Length; ++i)
        {
            // GenerateTilemapShadowCastersInEditor(colliders[i], false);
            ShadowCaster2DGenerator.GenerateTilemapShadowCasters(colliders[i], false);
        }

        if (!SetPower())
        {
            StartGen();
            yield break;
        }
        SetFactories();
        SetTowers();
        SetTraps();

        Initialized = true;
    }

    public Vector3 GetPlayerStartPos()
    {
        Vector3Int powerPos = toController.GetPower();
        int groundX = (int)(powerPos.x * 3 / 7);
        int groundY = (int)(powerPos.y * 3 / 7);

        int2 xRange = new int2(0, MapSize.x);
        int2 yRange = new int2(0, MapSize.y);

        if (groundX < MapSize.x / 2)
        {
            xRange.x = MapSize.x - 5;
        }
        else
        {
            xRange.y = 0 + 5;
        }

        if (groundY < MapSize.y / 2)
        {
            yRange.x = MapSize.y - 5;
        }
        else
        {
            yRange.y = 0 + 5;
        }

        while (true)
        {
            Vector3Int posCandidat = new Vector3Int(
                // Random.Range(xRange.x, xRange.y),
                // Random.Range(yRange.x, yRange.y),
                rnd.NextInt(xRange.x, xRange.y),
                rnd.NextInt(yRange.x, yRange.y),
                0
            );
            if (GetCellLayer(posCandidat) > 0)
            {
                return grid.GetCellCenterWorld(posCandidat);
            }
        }
    }

    public int GetLayerFromWorldPos(Vector3 worldPos)
    {
        Vector3Int cellPos =  grid.WorldToCell(worldPos);

        // int thisCellLayer = GetCellLayer(cellPos);

        // int[] neighboursLayers = new int[] {
        //     GetCellLayer(cellPos + Vector3Int.left),
        //     GetCellLayer(cellPos + Vector3Int.up),
        //     GetCellLayer(cellPos + Vector3Int.right),
        //     GetCellLayer(cellPos + Vector3Int.down)
        // };

        // bool allOneLayer = true;

        // foreach (int nl in neighboursLayers)
        // {
        //     if (nl < thisCellLayer)
        //     {
        //         allOneLayer = false;
        //     }
        // }

        if (layerDebug != null)
        {
            layerDebug.transform.position = new Vector3(
                grid.GetCellCenterWorld(cellPos).x,
                grid.GetCellCenterWorld(cellPos).y,
                -1f
            );
        }

        // if (allOneLayer)
        // {
        //     if (layerDebug != null)
        //     {
        //         dt0.text = thisCellLayer.ToString();
        //         dt1.text = thisCellLayer.ToString();
        //         dt2.text = thisCellLayer.ToString();
        //         dt3.text = thisCellLayer.ToString();
        //     }
        //     return thisCellLayer;
        // }


        // int[] quarterLayers = new int[4] {
        //     thisCellLayer,
        //     thisCellLayer,
        //     thisCellLayer,
        //     thisCellLayer
        // };

        // if (neighboursLayers[0] < thisCellLayer)
        // {
        //     quarterLayers[0] = neighboursLayers[0];
        //     quarterLayers[1] = neighboursLayers[0];
        // }
        // if (neighboursLayers[1] < thisCellLayer)
        // {
        //     quarterLayers[1] = neighboursLayers[1];
        //     quarterLayers[2] = neighboursLayers[1];
        // }
        // if (neighboursLayers[2] < thisCellLayer)
        // {
        //     quarterLayers[2] = neighboursLayers[2];
        //     quarterLayers[3] = neighboursLayers[2];
        // }
        // if (neighboursLayers[3] < thisCellLayer)
        // {
        //     quarterLayers[3] = neighboursLayers[3];
        //     quarterLayers[0] = neighboursLayers[3];
        // }
        int[] quarterLayers;
        if (tilesLayers[cellPos.x, cellPos.y] == null)
        {
            quarterLayers = new int[]{0,0,0,0};
        }
        else
        {
            quarterLayers = tilesLayers[cellPos.x, cellPos.y];
        }

        if (layerDebug != null)
        {
            dt0.text = quarterLayers[0].ToString();
            dt1.text = quarterLayers[1].ToString();
            dt2.text = quarterLayers[2].ToString();
            dt3.text = quarterLayers[3].ToString();
        }

        Vector3 cellWorldPos = grid.GetCellCenterWorld(cellPos);

        if (worldPos.x < cellWorldPos.x)
        {
            if (worldPos.y < cellWorldPos.y)
            {
                return quarterLayers[0];
            }
            else
            {
                return quarterLayers[1];
            }
        }
        else
        {
            if (worldPos.y < cellWorldPos.y)
            {
                return quarterLayers[3];
            }
            else
            {
                return quarterLayers[2];
            }
        }

        // return thisCellLayer;

    }

    public int GetCellLayer(Vector3Int cellPos)
    {
        if (tilemaps[2].HasTile(new Vector3Int(cellPos.x, cellPos.y, 0)))
        {
            return 2;
        }

        if (tilemaps[1].HasTile(new Vector3Int(cellPos.x, cellPos.y, 0)))
        {
            return 1;
        }

        if (tilemaps[0].HasTile(new Vector3Int(cellPos.x, cellPos.y, 0)))
        {
            return 0;
        }

        return 0;
    }

    public Vector3[] GetNeighboursCoordFromWorldPos(Vector3 worldPos)
    {
        Vector3Int cellPos =  gridProps.WorldToCell(worldPos);

        return new Vector3[] {
            gridProps.GetCellCenterWorld(
                cellPos + Vector3Int.left
            ),
            gridProps.GetCellCenterWorld(
                cellPos + Vector3Int.up
            ),
            gridProps.GetCellCenterWorld(
                cellPos + Vector3Int.right
            ),
            gridProps.GetCellCenterWorld(
                cellPos + Vector3Int.down
            )
        };
    }

    public bool SetDirt(Vector3 worldPos, Vector3 playerPos, out Vector3Int cellPos)
    {
        cellPos =  gridProps.WorldToCell(worldPos);
        return SetDirt(cellPos, playerPos);
    }

    public bool SetDirt(Vector3Int cellPos, Vector3 playerPos)
    {
        // Vector3Int terrainCellPos = grid.WorldToCell(gridProps.GetCellCenterWorld(cellPos));
        Vector3Int terrainCellPos = new Vector3Int(
            (int)(cellPos.x * 3 / 7),
            (int)(cellPos.y * 3 / 7),
            0
        );

        Debug.Log(GetCellLayer(terrainCellPos));
        int cellLayer = GetCellLayer(terrainCellPos);
        if (cellLayer == 0)
        {
            Debug.Log("Can not dig on water level");
            return false;
        }

        if (
            GetLayerFromWorldPos(gridProps.GetCellCenterWorld(cellPos)) !=
            GetLayerFromWorldPos(playerPos)
        )
        // if (tilemaps[cellLayer].GetTile(terrainCellPos) != levelBackgroundTile[cellLayer])
        {
            Debug.Log($"Can not dig on non flat tiles: {tilemaps[cellLayer].GetTile(terrainCellPos)}");
            return false;
        }
        treeAndPropsTilemaps[cellLayer - 1].SetTile(cellPos, dirtMasks[0].tile);
        toController.TileDestroyed(cellPos);
        toController.RegisterDirt(cellPos);

        UpdateDirt(cellPos);

        UpdateDirt(cellPos + Vector3Int.left);
        UpdateDirt(cellPos + Vector3Int.up);
        UpdateDirt(cellPos + Vector3Int.right);
        UpdateDirt(cellPos + Vector3Int.down);

        return true;
    }

    public void UpdateDirt(Vector3Int cellPos)
    {
        if (!IsDirtTile(cellPos))
        {
            return;
        }
         Vector3Int terrainCellPos = grid.WorldToCell(gridProps.GetCellCenterWorld(cellPos));

        string mask = String.Format(
            "{0}{1}{2}{3}",
            IsDirtTile(cellPos + Vector3Int.left) ? "1" : "0",
            IsDirtTile(cellPos + Vector3Int.up) ? "1" : "0",
            IsDirtTile(cellPos + Vector3Int.right) ? "1" : "0",
            IsDirtTile(cellPos + Vector3Int.down) ? "1" : "0"
        );

        foreach (var dm in dirtMasks)
        {
            if (mask == dm.mask)
            {
                treeAndPropsTilemaps[GetCellLayer(terrainCellPos) - 1].SetTile(cellPos, dm.tile);
            }
        }
    }

    public void RemoveDirt(Vector3Int cellPos)
    {
        if (!IsDirtTile(cellPos))
        {
            return;
        }
        Vector3Int terrainCellPos = new Vector3Int(
            (int)(cellPos.x * 3 / 7),
            (int)(cellPos.y * 3 / 7),
            0
        );

        Debug.Log(GetCellLayer(terrainCellPos));
        int cellLayer = GetCellLayer(terrainCellPos);
        if (cellLayer == 0)
        {
            return;
        }

        // if (tilemaps[cellLayer].GetTile(terrainCellPos) != levelBackgroundTile[cellLayer])
        // {
        //     return;
        // }
        treeAndPropsTilemaps[cellLayer - 1].SetTile(cellPos, null);

        UpdateDirt(cellPos + Vector3Int.left);
        UpdateDirt(cellPos + Vector3Int.up);
        UpdateDirt(cellPos + Vector3Int.right);
        UpdateDirt(cellPos + Vector3Int.down);
    }

    public bool IsDirtTile(Vector3Int cellPos)
    {
        Vector3Int terrainCellPos = grid.WorldToCell(gridProps.GetCellCenterWorld(cellPos));
        if (GetCellLayer(terrainCellPos) == 0)
        {
            return false;
        }
        // int layer = GetLayerFromWorldPos(gridProps.GetCellCenterWorld(cellPos));
        // if (!treeAndPropsTilemaps[layer - 1].HasTile(cellPos))
        // {
        //     return false;
        // }

        // var current = treeAndPropsTilemaps[layer - 1].GetTile(cellPos);
        var current0 = treeAndPropsTilemaps[0].GetTile(cellPos);
        var current1 = treeAndPropsTilemaps[1].GetTile(cellPos);
        if (!dirtTiles.Contains(current0) && !dirtTiles.Contains(current1))
        {
            return false;
        }

        return true;
    }

    public List<Vector3Int> FindConnectedDirt(Vector3Int cellPos, out int minX, out int minY)
    {
        List<Vector3Int> foundTiles = new List<Vector3Int>();
        minX = 1000;
        minY = 1000;

        if (!IsDirtTile(cellPos))
        {
            return foundTiles;
        }

        List<Vector3Int> tilesLooked = new List<Vector3Int>();
        Queue<Vector3Int> tilesToLook = new Queue<Vector3Int>();

        tilesToLook.Enqueue(cellPos);


        while (tilesToLook.Count() > 0)
        {
            var t = tilesToLook.Dequeue();

            tilesLooked.Add(t);
            if (!IsDirtTile(t))
            {
                continue;
            }

            Vector3Int[] neighbours = new [] {
                t + Vector3Int.left,
                t + Vector3Int.up,
                t + Vector3Int.right,
                t + Vector3Int.down
            };

            foreach (var n in neighbours)
            {
                if (tilesLooked.Contains(n) || tilesToLook.Contains(n))
                {
                    continue;
                }

                tilesToLook.Enqueue(n);
            }

            foundTiles.Add(t);

            if (t.x < minX)
            {
                minX = t.x;
            }

            if (t.y < minY)
            {
                minY = t.y;
            }
        }

        return foundTiles;
    }

    List<Vector3Int> GetPath(Vector3Int target, Vector3Int source){
        List<Vector3Int> path = null;
        Dictionary<Vector3Int, float> dist = new Dictionary<Vector3Int, float>();
        Dictionary<Vector3Int, Vector3Int> prev = new Dictionary<Vector3Int, Vector3Int>();

        List<Vector3Int> unvisited = new List<Vector3Int>();

        Vector3Int nullV = new Vector3Int(-100, -100, -100);


        dist[source] = 0;
        prev[source] = nullV;

        int sizeX = (int)(MapSize.x * 7 / 3);
        int sizeY = (int)(MapSize.y * 7 / 3);

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                Vector3Int v = new Vector3Int(i, j, 0);
                if (v != source){
                    dist[v] = Mathf.Infinity;
                    prev[v] = nullV;
                }
                unvisited.Add(v);
                // unvisited.Add();
            }
        }


        while (unvisited.Count > 0){
            Vector3Int u = nullV;

            foreach (Vector3Int possibleU in unvisited) {
                if (u == nullV || dist[possibleU] < dist[u]) {
                    u = possibleU;
                }
            }

            if (u == target){
                break;
            }

            unvisited.Remove(u);

            Vector3Int[] neighbours = new [] {
                u + Vector3Int.left,
                u + Vector3Int.up,
                u + Vector3Int.right,
                u + Vector3Int.down
            };

            foreach (Vector3Int v in neighbours){
                int groundX = (int)(v.x * 3 / 7);
                int groundY = (int)(v.y * 3 / 7);

                int layer = GetCellLayer(new Vector3Int(groundX, groundY, 0));

                float cost = 4;

                if (layer != 1)
                {
                    cost = cost * 100;
                }

                if (tilemaps[1].GetTile(new Vector3Int(groundX, groundY, 0)) != levelBackgroundTile[1])
                {
                    cost = cost * 100;
                }

                if (treeAndPropsTilemaps[0].HasTile(v))
                {
                    if (treeAndPropsTilemaps[0].GetTile(v) == powerLineTile)
                    {
                        cost = 1;
                    }
                }

                if (cost < 0) {
                    cost = 0;
                }
                float alt = dist[u] + cost;
                if (alt < dist[v]){
                    dist[v] = alt;
                    prev[v] = u;
                }
            }
        }

        path = new List<Vector3Int> ();
        Vector3Int curr = target;

        if (prev [curr] == nullV) {
            return path;
        }

        while (curr != nullV) {
            path.Add (curr);
            curr = prev [curr];
        }
        path.Reverse ();

        return path;
    }
}
