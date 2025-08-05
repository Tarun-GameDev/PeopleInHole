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

    [SerializeField] private HolesScriptable holesScriptable;
    [SerializeField] private List<PlacedObj> placedBlockTiles = new List<PlacedObj>();
    [SerializeField] private List<PlacedObj> placedHoles = new List<PlacedObj>();

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
        
        // Initialize the cache with existing holes
        RefreshHolePositionsCache();

        // Initialize the cache for placed tiles
        placedTilesCache = new HashSet<Vector2Int>(placedTiles.Keys);
    }

    // Update RefreshHolePositionsCache to also build color cache
    private void RefreshHolePositionsCache()
    {
        holePositionsCache.Clear();
        holeColorCache.Clear();
        
        foreach (var hole in placedHoles)
        {
            holePositionsCache.Add(hole.position);
            
            // Cache the hole color for O(1) lookups
            var holeComponent = hole.tile?.GetComponent<Hole>();
            if (holeComponent != null)
            {
                holeColorCache[hole.position] = holeComponent.HoleColor;
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
    }

    public void RegenerateGrid()
    {
        DeleteAllChildren();

        selectedCells.Clear();
        selectedCells = placedTileList.Select(cell => cell.position).ToHashSet();

        // Regenerate the grid
        GenerateGrid();
        selectedCells.Clear();
    }

    public void GenerateBlocks()
    {
        if (selectedCells.Count == 0) return;

        HashSet<Vector2Int> toUpdate = new HashSet<Vector2Int>();

        // Collect all selected + their neighbors
        foreach (var cell in selectedCells)
        {
            toUpdate.Add(cell);
            toUpdate.Add(new Vector2Int(cell.x + 1, cell.y));
            toUpdate.Add(new Vector2Int(cell.x - 1, cell.y));
            toUpdate.Add(new Vector2Int(cell.x, cell.y + 1));
            toUpdate.Add(new Vector2Int(cell.x, cell.y - 1));
            toUpdate.Add(new Vector2Int(cell.x - 1, cell.y - 1));
            toUpdate.Add(new Vector2Int(cell.x - 1, cell.y + 1));
            toUpdate.Add(new Vector2Int(cell.x + 1, cell.y - 1));
            toUpdate.Add(new Vector2Int(cell.x + 1, cell.y + 1));
        }

        foreach (var pos in toUpdate)
        {
            if (pos.x < 0 || pos.x >= gridSize.x || pos.y < 0 || pos.y >= gridSize.y)
                continue;

            CreateOrUpdateBlockTile(pos.x, pos.y);
        }
    }

    private void CreateOrUpdateBlockTile(int x, int y)
    {
        Vector2Int pos = new(x, y);

        bool isActiveTile = selectedCells.Contains(pos);

        if (!isActiveTile && !placedTiles.ContainsKey(pos))
            return;

        if (placedTiles.TryGetValue(pos, out GameObject existing))
        {
            
        }

        var result = ruleTile.GetPrefabForPosition(x, y, placedTiles, selectedCells);
        if (result.prefab == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(result.prefab, container);

        Vector3 blockPos = existing.transform.position;
        blockPos.y = 0.35f; // Adjust height if necessary

        instance.transform.position = blockPos;
        instance.transform.rotation = result.rotation;

        instance.transform.parent = existing.transform;

        placedBlockTiles.Add(new PlacedObj { position = pos, tile = instance });
    }

    public void PlaceHoles(HoleColor holeColor)
    {
        foreach (Vector2Int cell in selectedCells)
        {
            PlaceHole(cell, holeColor);
        }
        
        // Refresh cache after placing holes
        RefreshHolePositionsCache();
    }

    private void PlaceHole(Vector2Int gridPos, HoleColor holeColor)
    {
        if (holesScriptable == null)
        {
            Debug.LogWarning("HolesScriptable is not assigned!");
            return;
        }

        // Find the matching hole type for the specified color
        HoleType holeType = holesScriptable.holeTypes.FirstOrDefault(h => h.holeColor == holeColor);
        
        if (holeType == null)
        {
            Debug.LogWarning($"No hole type found for color: {holeColor}");
            return;
        }

        if (holeType.holePrefab == null)
        {
            Debug.LogWarning($"No hole prefab assigned for color: {holeColor}. Please reassign the prefab in the HolesScriptable.");
            return;
        }

        // Convert grid position to world position
        Vector3 worldPos = GetWorldPosition(gridPos.x, gridPos.y);
        worldPos.y = 0.35f; // Adjust height if necessary

#if UNITY_EDITOR
        // In editor, use PrefabUtility to maintain prefab connection
        GameObject instantiatedObject = (GameObject)PrefabUtility.InstantiatePrefab(holeType.holePrefab.gameObject, container);
        Hole holeInstance = instantiatedObject.GetComponent<Hole>();
#else
        // In runtime, use regular Instantiate
        GameObject instantiatedObject = Instantiate(holeType.holePrefab.gameObject, container);
        Hole holeInstance = instantiatedObject.GetComponent<Hole>();
#endif

        if (holeInstance == null)
        {
            Debug.LogError($"The assigned prefab for {holeColor} does not have a Hole component!");
            if (instantiatedObject != null)
                DestroyImmediate(instantiatedObject);
            return;
        }

        holeInstance.transform.position = worldPos;
        holeInstance.transform.rotation = Quaternion.identity;
        holeInstance.HoleColor = holeColor;

        // Parent the placed hole to the tile it is placed on
        var holePlacedTile = placedTileList.Find(tile => tile.position == gridPos);
        holeInstance.transform.parent = holePlacedTile.tile.transform;

        // Optional: Name the instance for easier identification in the hierarchy
        holeInstance.name = $"Hole_{holeColor}_{gridPos.x}_{gridPos.y}";

        // Add the hole instance to the list of placed holes
        placedHoles.Add(new PlacedObj { position = gridPos, tile = holeInstance.gameObject });
    }

    public void RemoveBlocks()
    {
        var blockTiles = placedBlockTiles.ToList();

        foreach (var block in blockTiles)
        {
            if (block.tile != null)
            {
                DestroyImmediate(block.tile);
                placedBlockTiles.Remove(block);
            }
        }
    }

    public void RemoveHoles()
    {
        foreach (var selectedCell in selectedCells)
        {
            var selectedHole = placedHoles.Find(hole => hole.position == selectedCell);

            if (selectedHole.tile != null)
            {
                DestroyImmediate(selectedHole.tile);
                placedHoles.Remove(selectedHole);
            }
        }
        
        // Refresh cache after removing holes
        RefreshHolePositionsCache();
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
    }

    public override void DeleteSelectedTiles()
    {
        // Also Remove the placed holes from list
        RemoveHoles();

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
        return !placedTilesCache.Contains(pos);
    }

    public bool IsHole(Vector2Int pos)
    {
        return holeColorCache.ContainsKey(pos);
    }

    public bool IsOurHole(Vector2Int pos, CrowdBoxController crowdBoxController)
    {
        return holeColorCache.TryGetValue(pos, out HoleColor holeColor) && 
               holeColor == crowdBoxController.CrowdBoxControllerColor;
    }

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

    public bool IsMovingInwards(Vector2Int currentGridPos, Vector2Int gotoGridPos, List<CrowdBox> crowdBoxes = null)
    {
        // Check if the 
        return crowdBoxes.Any(box => box.CurrentGridPos == gotoGridPos);
    }

    // Returns true if gotogridpos is diagonal to currentgridpos
    public bool IsDiagonal(Vector2Int currentGridPos, Vector2Int gotoGridPos)
    {
        if (Utils.GetDirectionFromPos(currentGridPos, gotoGridPos) is GridDirection.DownLeft or GridDirection.DownRight or GridDirection.UpLeft or GridDirection.UpRight)
            return true;

        return false;
    }

    void OnGUI()
    {
        // Create a custom style for larger, black text
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 50; // Increase font size (default is usually around 12-14)
        labelStyle.normal.textColor = Color.black; // Set text color to black

        GUI.Label(new Rect(100, 250, 1000, 1000), placedTilesCache.Count.ToString(), labelStyle);
    }
}