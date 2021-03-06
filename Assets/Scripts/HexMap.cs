﻿using QPath;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMap : MonoBehaviour, IQPathWorld {

    public GameObject HexPrefab;

    public Mesh MeshWater;
    public Mesh MeshFlat;
    public Mesh MeshHill;
    public Mesh MeshMountain;

    public Material MatOcean;
    public Material MatPlains;
    public Material MatGrasslands;
    public Material MatMountains;
    public Material MatDesert;

    public GameObject ForestPrefab;
    public GameObject JunglePrefab;

    public GameObject UnitDwarfPrefab;

    // Height thresholds to determine tile type
    [System.NonSerialized] public float HeightMountain = 1f;
    [System.NonSerialized] public float HeightHill = 0.6f;
    [System.NonSerialized] public float HeightFlat = 0.0f;

    [System.NonSerialized] public float MoistureJungle = 0.66f;
    [System.NonSerialized] public float MoistureForest = 0.33f;
    [System.NonSerialized] public float MoistureGrasslands = 0.0f;
    [System.NonSerialized] public float MoisturePlains = -0.5f;

    [System.NonSerialized] public int NumRows = 30;
    [System.NonSerialized] public int NumColumns = 60;

    [System.NonSerialized] public bool AllowWrapEastWest = true;
    [System.NonSerialized] public bool AllowWrapNorthSouth = false;

    private Hex[,] hexes;
    private Dictionary<Hex, GameObject> hexToGameObjectMap;
    private Dictionary<GameObject, Hex> gameObjectToHexMap;

    private HashSet<Unit> units;
    private Dictionary<Unit, GameObject> unitToGameObjectMap;

    
    // Use this for initialization
    void Start () {
        GenerateMap();
	}

    public bool AnimationIsPlaying = false;

    private void Update()
    {
        // TESTING: Hit space to advance to next turn
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine( DoAllUnitMoves() );
        }
    }

    public void EndTurn()
    {

        // Reset unit movement
        foreach(Unit u in units)
        {
            u.RefreshMovement();
        }
    }

    IEnumerator DoAllUnitMoves()
    {
        if (units != null)
        {
            foreach (Unit u in units)
            {
                yield return DoUnitMoves(u);
            }
        }
    }

    public IEnumerator DoUnitMoves( Unit u )
    {
        while (u.DoMove())
        {
            Debug.Log("DoMove returned true -- will be called again");
            // TODO: Check to see if an animation is playing. 
            // If so, wait for it to finish
            while (AnimationIsPlaying) { yield return null; }
        }
    }

    virtual public void GenerateMap()
    {
        hexes = new Hex[NumColumns, NumRows];
        hexToGameObjectMap = new Dictionary<Hex, GameObject>();
        gameObjectToHexMap = new Dictionary<GameObject, Hex>();

        for (int column = 0; column < NumColumns; column++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                // Instantiate a hex
                Hex h = new Hex(this, column, row);
                h.Elevation = -0.5f;

                hexes[column, row] = h;

                Vector3 pos = h.PositionFromCamera(
                    Camera.main.transform.position,
                    NumRows,
                    NumColumns
                );

                GameObject hexGO = (GameObject)Instantiate(
                    HexPrefab, 
                    pos,
                    Quaternion.identity,
                    this.transform
                );

                hexToGameObjectMap[h] = hexGO;
                gameObjectToHexMap[hexGO] = h;

                h.TerrainType = Hex.TERRAIN_TYPE.OCEAN;
                h.ElevationType = Hex.ELEVATION_TYPE.WATER;

                hexGO.name = string.Format("{0},{1}", column, row);
                hexGO.GetComponent<HexComponent>().Hex = h;
                hexGO.GetComponent<HexComponent>().HexMap = this;

                hexGO.GetComponentInChildren<TextMesh>().text = string.Format("{0},{1}\n{2}", column, row, h.BaseMovementCost(false, false, false));
            }
        }

        UpdateHexVisuals();

        //StaticBatchingUtility.Combine(this.gameObject);
    }

    public Hex GetHexAt(int x, int y)
    {
        if (hexes == null)
        {
            Debug.LogError("Hexes array not yet instantiated");
            return null;
        }

        if (AllowWrapEastWest)
        {
            x = x % NumColumns;
            if (x < 0)
            {
                x += NumColumns;
            }
        }
        if (AllowWrapNorthSouth)
        {
            y = y % NumRows;
            if (y < 0)
            {
                y += NumRows;
            }
        }

        return hexes[x, y];
    }

    public Hex GetHexFromGameObject(GameObject hexGO)
    {
        if (gameObjectToHexMap.ContainsKey(hexGO))
        {
            return gameObjectToHexMap[hexGO];
        }

        return null;
    }

    public GameObject GetHexGO(Hex h)
    {
        if (hexToGameObjectMap.ContainsKey(h))
        {
            return hexToGameObjectMap[h];
        }

        return null;
    }

    public Vector3 GetHexPosition(int q, int r)
    {
        Hex hex = GetHexAt(q, r);

        return GetHexPosition(hex);
    }

    public Vector3 GetHexPosition(Hex hex)
    {
        return hex.PositionFromCamera(Camera.main.transform.position, NumRows, NumColumns);
    }


    public void UpdateHexVisuals()
    {
        for (int column = 0; column < NumColumns; column++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                Hex h = hexes[column, row];
                GameObject hexGO = hexToGameObjectMap[h];

                MeshRenderer mr = hexGO.GetComponentInChildren<MeshRenderer>();
                MeshFilter mf = hexGO.GetComponentInChildren<MeshFilter>();

                if (h.Elevation >= HeightFlat && h.Elevation < HeightMountain)
                {
                    if (h.Moisture >= MoistureJungle)
                    {
                        mr.material = MatGrasslands;
                        h.TerrainType = Hex.TERRAIN_TYPE.GRASSLANDS;
                        h.FeatureType = Hex.FEATURE_TYPE.RAINFOREST;

                        // Spawn jungle
                        Vector3 treePos = hexGO.transform.position;
                        if (h.Elevation > HeightHill)
                        {
                            treePos.y += 0.25f;
                        }

                        GameObject.Instantiate(JunglePrefab, treePos, Quaternion.identity, hexGO.transform);
                    }
                    else if (h.Moisture >= MoistureForest)
                    {
                        mr.material = MatGrasslands;
                        h.TerrainType = Hex.TERRAIN_TYPE.GRASSLANDS;
                        h.FeatureType = Hex.FEATURE_TYPE.FOREST;

                        // Spawn forest
                        Vector3 treePos = hexGO.transform.position;
                        if(h.Elevation > HeightHill)
                        {
                            treePos.y += 0.25f;
                        }

                        h.FeatureType = Hex.FEATURE_TYPE.FOREST;

                        GameObject.Instantiate(ForestPrefab, treePos, Quaternion.identity, hexGO.transform);
                    }
                    else if (h.Moisture >= MoistureGrasslands)
                    {
                        mr.material = MatGrasslands;
                        h.TerrainType = Hex.TERRAIN_TYPE.GRASSLANDS;
                    }
                    else if (h.Moisture >= MoisturePlains)
                    {
                        mr.material = MatPlains;
                        h.TerrainType = Hex.TERRAIN_TYPE.PLAINS;
                    }
                    else
                    {
                        mr.material = MatDesert;
                        h.TerrainType = Hex.TERRAIN_TYPE.DESERT;
                    }
                }

                if (h.Elevation >= HeightMountain)
                {
                    mr.material = MatMountains;
                    mf.mesh = MeshMountain;
                    h.ElevationType = Hex.ELEVATION_TYPE.MOUNTAIN;
                }
                else if (h.Elevation >= HeightHill)
                {
                    mf.mesh = MeshHill;
                    h.ElevationType = Hex.ELEVATION_TYPE.HILL;
                }
                else if (h.Elevation >= HeightFlat)
                {
                    mf.mesh = MeshFlat;
                    h.ElevationType = Hex.ELEVATION_TYPE.FLAT;
                }
                else
                {
                    mr.material = MatOcean;
                    mf.mesh = MeshFlat;
                    h.ElevationType = Hex.ELEVATION_TYPE.WATER;
                }

                hexGO.GetComponentInChildren<TextMesh>().text = string.Format("{0},{1}\n{2}", column, row, h.BaseMovementCost(false, false, false));
            }
        }
    }

    public Hex[] GetHexesWithinRangeOf(Hex centerHex, int range)
    {
        List<Hex> results = new List<Hex>();

        for(int dx = -range; dx < range-1; dx++)
        {
            for (int dy = Mathf.Max(-range+1, -dx-range); dy < Mathf.Min(range, -dx+range-1); dy++)
            {
                results.Add(GetHexAt(centerHex.Q + dx, centerHex.R + dy));
            }
        }

        return results.ToArray();
    }

    public void SpawnUnitAt(Unit unit, GameObject prefab, int q, int r)
    {
        if(units == null)
        {
            units = new HashSet<Unit>();
            unitToGameObjectMap = new Dictionary<Unit, GameObject>();
        }

        Hex hex = GetHexAt(q, r);
        GameObject hexGO = hexToGameObjectMap[hex];
        unit.SetHex(hex);

        GameObject unitGO = Instantiate(prefab, hexGO.transform.position, Quaternion.identity, hexGO.transform);
        unit.OnUnitMoved += unitGO.GetComponent<UnitView>().OnUnitMoved;

        units.Add(unit);
        unitToGameObjectMap.Add(unit, unitGO);
    }
}
