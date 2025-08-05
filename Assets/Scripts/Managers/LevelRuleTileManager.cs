using DT.GridSystem.Ruletile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct PlacedObj
{
    public Vector2Int position;
    public GameObject tile;
}

public class LevelRuleTileManager : RuleTileManger
{
    [SerializeField] GameObject innerTilePrefab;
    Dictionary<Vector2Int, GameObject> innerTiles = new();
    
    // Add serialized list for persistent inner tile data
    [SerializeField] private List<PlacedObj> innerTileList = new List<PlacedObj>();
    
    private HashSet<Vector2Int> placedTilesCache = new HashSet<Vector2Int>();

    // Performance optimization: Cache hole positions for O(1) lookups
    private HashSet<Vector2Int> holePositionsCache = new HashSet<Vector2Int>();
    
    // Performance optimization: Pre-calculated Manhattan neighbor offsets
    private static readonly Vector2Int[] manhattanOffsets = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0)   // Left
    };

    // Add this new cache for O(1) color-specific hole lookups
    private Dictionary<Vector2Int, HoleColor> holeColorCache = new Dictionary<Vector2Int, HoleColor>();

    public void Init()
    {
        // Rebuild the placedTiles dictionary from serialized data (works in builds)
        RebuildDictionary();
        
        // Rebuild inner tiles dictionary from serialized data
        RebuildInnerTilesDictionary();

        // Initialize the cache for placed tiles
        placedTilesCache = new HashSet<Vector2Int>(placedTiles.Keys);
    }

    // Method to rebuild innerTiles dictionary from serialized list
    private void RebuildInnerTilesDictionary()
    {
        innerTiles.Clear();
        
        foreach (var innerTile in innerTileList)
        {
            if (innerTile.tile != null)
            {
                innerTiles[innerTile.position] = innerTile.tile;
            }
        }
    }

    // Method to sync innerTiles dictionary to serialized list
    private void SyncInnerTileList()
    {
        innerTileList.Clear();
        
        foreach (var kvp in innerTiles)
        {
            if (kvp.Value != null)
            {
                innerTileList.Add(new PlacedObj
                {
                    position = kvp.Key,
                    tile = kvp.Value
                });
            }
        }
    }

#if UNITY_EDITOR

    void GenerateInnerGrid()
    {
        foreach (var innerTile in innerTiles)
        {
            DestroyImmediate(innerTile.Value);
        }

        innerTiles.Clear();

        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                if (!placedTiles.ContainsKey(new Vector2Int(i, j)))
                {
                    var innerTile = (GameObject)PrefabUtility.InstantiatePrefab(innerTilePrefab, container);
                    innerTile.transform.SetPositionAndRotation(GetWorldPosition(i, j, true), Quaternion.identity);
                    innerTiles.Add(new Vector2Int(i, j), innerTile);
                }
            }
        }
        
        // Sync to serialized list
        SyncInnerTileList();
    }
    

    public void RemoveInnerTiles()
    {
        foreach (var innerTile in innerTiles)
        {
            if (innerTile.Value != null)
            {
                DestroyImmediate(innerTile.Value);
            }
        }
        innerTiles.Clear();
        
        // Sync to serialized list
        SyncInnerTileList();
    }

    public override void DeleteSelectedTiles()
    {
        base.DeleteSelectedTiles();

        GenerateInnerGrid();
    }

    public override void DeleteAllChildren()
    {
        base.DeleteAllChildren();

        RemoveInnerTiles();
    }

#endif

    public bool IsBlocked(Vector2Int pos)
    {
        return placedTilesCache.Contains(pos);
    }

    public bool IsHole(Vector2Int pos)
    {
        return holeColorCache.ContainsKey(pos);
    }

    /*public bool IsOurHole(Vector2Int pos, CrowdBoxController crowdBoxController)
    {
        return holeColorCache.TryGetValue(pos, out HoleColor holeColor) && 
               holeColor == crowdBoxController.CrowdBoxControllerColor;
    }*/

    public List<Vector2Int> GetManhattanNeighbourTiles(Vector2Int gridPos)
    {
        // Pre-allocate list with known capacity for better performance
        List<Vector2Int> result = new List<Vector2Int>(4);

        // Use pre-calculated offsets instead of enum iteration and direction conversion
        for (int i = 0; i < manhattanOffsets.Length; i++)
        {
            Vector2Int neighbor = gridPos + manhattanOffsets[i];
            
            // Check if the neighbor position is within grid bounds
            if (IsInBounds(neighbor))
            {
                result.Add(neighbor);
            }
        }

        return result;
    }

    public List<Vector2Int> GetNeighbourTiles(Vector2Int gridPos)
    {
        List<Vector2Int> result = new();
        Vector2Int currentGridPos = gridPos;

        var directions = Enum.GetValues(typeof(GridDirection)).Cast<GridDirection>();

        // Add neighboring positions if they're valid
        foreach (var direction in directions)
        {
            var neighbor = currentGridPos + Utils.GetDirection(direction);
            // Check if the neighbor position is within grid bounds
            if (IsInBounds(neighbor))
            {
                result.Add(new(neighbor.x, neighbor.y));
            }
        }

        return result;
    }
}