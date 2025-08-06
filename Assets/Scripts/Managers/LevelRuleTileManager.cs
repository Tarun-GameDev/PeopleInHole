using DT.GridSystem.Ruletile;
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

[Serializable]
public struct PlacedObj
{
    public Vector2Int position;
    public GameObject Obj;
}

public class LevelRuleTileManager : RuleTileManger
{
    [Header("Prefabs & Parents")] [SerializeField]
    private GameObject innerTilePrefab;

    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject holeEmptyParent;

    // In-scene caches (not serialized)
    private Dictionary<Vector2Int, GameObject> innerTiles = new();
    public HashSet<Vector2Int> placedTilesCache = new();
    public HashSet<Vector2Int> holePositionsCache = new();
    private Dictionary<Vector2Int, ColorEnum> holeColorCache = new();

    // Serialized data for persistence
    [SerializeField] private List<PlacedObj> innerTileList = new();
    [SerializeField] private List<PlacedObj> placedHoles = new();

    [HideInInspector] public ColorEnum selectedHoleColor;

    /// <summary>
    /// Call this on Awake/Start to rebuild dictionaries from serialized data.
    /// </summary>
    public void Init()
    {
        RebuildDictionary();
        RebuildInnerTilesDictionary();

        placedTilesCache = new HashSet<Vector2Int>(placedTiles.Keys);
        holePositionsCache = new HashSet<Vector2Int>();
        holeColorCache = new Dictionary<Vector2Int, ColorEnum>();
    
        // Safely rebuild hole caches
        foreach (var hole in placedHoles.ToList()) // Use ToList() to avoid modification during iteration
        {
            if (hole.Obj != null)
            {
                var ctrl = hole.Obj.GetComponent<HoleController>();
                if (ctrl != null)
                {
                    holePositionsCache.Add(hole.position);
                    holeColorCache[hole.position] = ctrl.holeColor;
                }
                else
                {
                    Debug.LogWarning($"[LevelRuleTileManager] Hole at {hole.position} is missing HoleController component! Removing from list.", this);
                    placedHoles.Remove(hole);
                }
            }
            else
            {
                Debug.LogWarning($"[LevelRuleTileManager] Null hole object at {hole.position}! Removing from list.", this);
                placedHoles.Remove(hole);
            }
        }
    }

    private void RebuildInnerTilesDictionary()
    {
        innerTiles.Clear();
        foreach (var tile in innerTileList.ToList()) // Use ToList() to avoid modification during iteration
        {
            if (tile.Obj != null)
                innerTiles[tile.position] = tile.Obj;
            else
            {
                Debug.LogWarning($"[LevelRuleTileManager] Null inner tile object at {tile.position}! Removing from list.", this);
                innerTileList.Remove(tile);
            }
        }
    }

    private void SyncInnerTileList()
    {
        innerTileList.Clear();
        foreach (var kv in innerTiles)
            if (kv.Value != null)
                innerTileList.Add(new PlacedObj { position = kv.Key, Obj = kv.Value });
    }

    private void SyncHoleList()
    {
        placedHoles.Clear();
        foreach (var kv in holePositionsCache)
        {
            var existingHole = FindHoleGameObject(kv);
            if (existingHole != null)
                placedHoles.Add(new PlacedObj { position = kv, Obj = existingHole });
        }
    }

    private GameObject FindHoleGameObject(Vector2Int position)
    {
        if (holeEmptyParent == null) return null;
        
        foreach (Transform child in holeEmptyParent.transform)
        {
            var holeCtrl = child.GetComponent<HoleController>();
            if (holeCtrl != null)
            {
                Vector2Int childGridPos = GetGridPosition(child.position);
                if (childGridPos == position)
                    return child.gameObject;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    public override void GenerateGrid()
    {
        Undo.RecordObject(this, "Generate Grid");
        base.GenerateGrid();
        GenerateInnerGrid();
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void GenerateInnerGrid()
    {
        Undo.RecordObject(this, "Generate Inner Grid");

        // Clean up existing inner tiles
        foreach (var kv in innerTiles.ToList()) 
        {
            if (kv.Value != null)
                DestroyImmediate(kv.Value);
        }
        innerTiles.Clear();

        // Generate new inner tiles only where there are no placed tiles
        // Holes can exist on top of inner tiles, so we don't check for holes here
        for (int x = 0; x < GridSize.x; x++)
        for (int y = 0; y < GridSize.y; y++)
        {
            var cell = new Vector2Int(x, y);
            
            // Skip only if there's a placed tile at this position
            // Inner tiles can exist underneath holes
            if (placedTiles.ContainsKey(cell))
                continue;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(innerTilePrefab, container);
            go.transform.SetPositionAndRotation(GetWorldPosition(x, y, true), Quaternion.identity);
            innerTiles[cell] = go;
        }

        SyncInnerTileList();
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
    
    public void SpawnHole()
    {
        // Guard missing references
        if (holePrefab == null)
        {
            Debug.LogError("[LevelRuleTileManager] Hole Prefab is not assigned!", this);
            return;
        }

        if (holeEmptyParent == null)
        {
            Debug.LogError("[LevelRuleTileManager] Hole Empty Parent is not assigned!", this);
            return;
        }

        if (selectedCells == null || selectedCells.Count == 0)
        {
            Debug.LogWarning("[LevelRuleTileManager] No cells selected – nothing to do.", this);
            return;
        }

        Undo.RecordObject(this, "Spawn Hole");

        foreach (var cell in selectedCells)
        {
            // Check if hole already exists at this position
            if (holePositionsCache.Contains(cell))
            {
                Debug.LogWarning($"[LevelRuleTileManager] Hole already exists at {cell}. Skipping.", this);
                continue;
            }

            // Check if there's a placed tile at this position - holes cannot be placed on placed tiles
            if (placedTilesCache.Contains(cell))
            {
                Debug.LogWarning($"[LevelRuleTileManager] Cannot place hole at {cell} - position is blocked by a placed tile.", this);
                continue;
            }

            Vector3 spawnPos = GetWorldPosition(cell.x, cell.y);
            
            Debug.Log($"[LevelRuleTileManager] Attempting to spawn hole at {cell} (world pos: {spawnPos})");
            
            var holeGO = (GameObject)PrefabUtility.InstantiatePrefab(holePrefab, holeEmptyParent.transform);
            
            if (holeGO == null)
            {
                Debug.LogError($"[LevelRuleTileManager] Failed to instantiate hole prefab at {cell}!", this);
                continue;
            }
            
            holeGO.transform.position = spawnPos;

            var ctrl = holeGO.GetComponent<HoleController>();
            if (ctrl == null)
            {
                Debug.LogError($"[LevelRuleTileManager] HoleController component missing on hole prefab! GameObject: {holeGO.name}", this);
                DestroyImmediate(holeGO);
                continue;
            }

            ctrl.holeColor = selectedHoleColor;
            ctrl.UpdateHoleMaterials();

            // Update all caches and lists
            placedHoles.Add(new PlacedObj { position = cell, Obj = holeGO });
            holePositionsCache.Add(cell);
            holeColorCache[cell] = selectedHoleColor;

            // DO NOT remove inner tiles - holes are placed on top of them
            // Inner tiles should remain underneath holes
            
            Debug.Log($"[LevelRuleTileManager] Successfully spawned hole at {cell} with color {selectedHoleColor} (inner tile preserved)");
        }

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public void RemoveHoles()
    {
        if (selectedCells == null || selectedCells.Count == 0)
        {
            Debug.LogWarning("[LevelRuleTileManager] No cells selected – nothing to remove.", this);
            return;
        }

        Undo.RecordObject(this, "Remove Holes");

        foreach (var cell in selectedCells.ToList())
        {
            // Find and remove from placedHoles list
            var holeEntry = placedHoles.Find(h => h.position == cell);
            if (holeEntry.Obj != null)
            {
                DestroyImmediate(holeEntry.Obj);
                placedHoles.Remove(holeEntry);
                
                // Update caches
                holePositionsCache.Remove(cell);
                holeColorCache.Remove(cell);
                
                Debug.Log($"[LevelRuleTileManager] Removed hole at {cell}");
            }
            else if (holePositionsCache.Contains(cell))
            {
                // Handle case where cache has entry but GameObject is missing
                Debug.LogWarning($"[LevelRuleTileManager] Found orphaned hole entry at {cell}. Cleaning up cache.", this);
                holePositionsCache.Remove(cell);
                holeColorCache.Remove(cell);
                placedHoles.RemoveAll(h => h.position == cell);
            }
        }

        // DO NOT regenerate inner tiles - they should already exist underneath the holes
        // Holes are placed on top, so removing them just reveals the inner tiles below

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void RegenerateInnerTilesInCells(HashSet<Vector2Int> cells)
    {
        if (innerTilePrefab == null) return;

        foreach (var cell in cells)
        {
            // Only generate inner tile if there's no placed tile at this position
            // Holes can exist on top of inner tiles, so we don't check for holes
            if (!placedTilesCache.Contains(cell))
            {
                // Check if inner tile already exists
                if (!innerTiles.ContainsKey(cell))
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(innerTilePrefab, container);
                    go.transform.SetPositionAndRotation(GetWorldPosition(cell.x, cell.y, true), Quaternion.identity);
                    innerTiles[cell] = go;
                }
            }
        }

        SyncInnerTileList();
    }

    public void RemoveInnerTiles()
    {
        Undo.RecordObject(this, "Remove Inner Tiles");

        foreach (var kv in innerTiles.ToList())
        {
            if (kv.Value != null)
                DestroyImmediate(kv.Value);
        }
        innerTiles.Clear();
        SyncInnerTileList();

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public override void DeleteSelectedTiles()
    {
        if (selectedCells == null || selectedCells.Count == 0)
        {
            Debug.LogWarning("[LevelRuleTileManager] No cells selected – nothing to delete.", this);
            return;
        }

        Undo.RecordObject(this, "Delete Selected Tiles");

        // Remove holes first
        RemoveHoles();
        
        // Remove placed tiles (from base class)
        base.DeleteSelectedTiles();
        
        // Regenerate inner grid to fill in gaps
        GenerateInnerGrid();

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public override void DeleteAllChildren()
    {
        Undo.RecordObject(this, "Delete All Children");

        // Clear all caches first
        holePositionsCache.Clear();
        holeColorCache.Clear();
        placedHoles.Clear();

        // Delete all children (from base class)
        base.DeleteAllChildren();
        
        // Remove inner tiles
        RemoveInnerTiles();

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    /// <summary>
    /// Clean up any orphaned entries in lists and caches
    /// </summary>
    public void CleanupOrphanedEntries()
    {
        // Clean up holes
        for (int i = placedHoles.Count - 1; i >= 0; i--)
        {
            var hole = placedHoles[i];
            if (hole.Obj == null)
            {
                placedHoles.RemoveAt(i);
                holePositionsCache.Remove(hole.position);
                holeColorCache.Remove(hole.position);
                Debug.Log($"[LevelRuleTileManager] Cleaned up orphaned hole entry at {hole.position}");
            }
        }

        // Clean up inner tiles
        foreach (var kv in innerTiles.ToList())
        {
            if (kv.Value == null)
            {
                innerTiles.Remove(kv.Key);
                Debug.Log($"[LevelRuleTileManager] Cleaned up orphaned inner tile entry at {kv.Key}");
            }
        }

        SyncInnerTileList();
    }
#endif

    /// <summary>
    /// Returns true if a placed (blocking) tile exists at the given coord.
    /// </summary>
    public bool IsBlocked(Vector2Int pos) => placedTilesCache.Contains(pos);

    /// <summary>
    /// Returns true if a hole of any color exists at the given coord.
    /// </summary>
    public bool IsHole(Vector2Int pos) => holeColorCache.ContainsKey(pos);

    /// <summary>
    /// Returns the color of the hole at the given position, or null if no hole exists.
    /// </summary>
    public ColorEnum? GetHoleColor(Vector2Int pos)
    {
        return holeColorCache.TryGetValue(pos, out ColorEnum color) ? color : null;
    }

    /// <summary>
    /// Returns true if there's an inner tile at the given position.
    /// </summary>
    public bool HasInnerTile(Vector2Int pos) => innerTiles.ContainsKey(pos);
}