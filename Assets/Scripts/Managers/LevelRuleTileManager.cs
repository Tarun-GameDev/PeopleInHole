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
    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject innerTilePrefab;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject holeEmptyParent;

    // In-scene caches (not serialized)
    private Dictionary<Vector2Int, GameObject> innerTiles = new();
    public HashSet<Vector2Int> placedTilesCache = new();
    public HashSet<Vector2Int> holePositionsCache = new();
    public HashSet<Vector2Int> innerTilePositionsCache = new();
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
        foreach (var hole in placedHoles.ToList())
        {
            if (hole.Obj != null)
            {
                var ctrl = hole.Obj.GetComponent<HoleController>();
                if (ctrl != null)
                {
                    holePositionsCache.Add(hole.position);
                    holeColorCache[hole.position] = ctrl.holeColor;
                    ctrl.holePos = hole.position;
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
        foreach (var tile in innerTileList.ToList())
        {
            if (tile.Obj != null)
                innerTiles[tile.position] = tile.Obj;
            else
            {
                Debug.LogWarning($"[LevelRuleTileManager] Null inner tile object at {tile.position}! Removing from list.", this);
                innerTileList.Remove(tile);
            }
        }
        SyncInnerTilePositionsCache();
    }
    private void SyncInnerTileList()
    {
        innerTileList.Clear();
        foreach (var kv in innerTiles)
            if (kv.Value != null)
                innerTileList.Add(new PlacedObj { position = kv.Key, Obj = kv.Value });
        SyncInnerTilePositionsCache();
    }

    private void SyncInnerTilePositionsCache()
    {
        innerTilePositionsCache = new HashSet<Vector2Int>(innerTiles.Keys);
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
        for (int x = 0; x < GridSize.x; x++)
        for (int y = 0; y < GridSize.y; y++)
        {
            var cell = new Vector2Int(x, y);
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
            if (holePositionsCache.Contains(cell))
            {
                Debug.LogWarning($"[LevelRuleTileManager] Hole already exists at {cell}. Skipping.", this);
                continue;
            }
            if (placedTilesCache.Contains(cell))
            {
                Debug.LogWarning($"[LevelRuleTileManager] Cannot place hole at {cell} - position is blocked by a placed tile.", this);
                continue;
            }

            Vector3 spawnPos = GetWorldPosition(cell.x, cell.y);
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

            // Set hole color/material before positioning
            ctrl.holeColor = selectedHoleColor;
            ctrl.UpdateHoleMaterials();

            // CENTRALIZED update: add/move hole in one place
            UpdateHolePos(ctrl, cell);

            Debug.Log($"[LevelRuleTileManager] Successfully spawned hole at {cell} with color {selectedHoleColor}");
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
            var holeEntry = placedHoles.Find(h => h.position == cell);
            if (holeEntry.Obj != null)
            {
                var ctrl = holeEntry.Obj.GetComponent<HoleController>();
                if (ctrl != null)
                {
                    // CENTRALIZED update: delete hole in one place
                    UpdateHolePos(ctrl, null);
                    DestroyImmediate(holeEntry.Obj);
                    Debug.Log($"[LevelRuleTileManager] Removed hole at {cell}");
                }
                else
                {
                    Debug.LogWarning($"[LevelRuleTileManager] HoleController missing on object at {cell}. Cleaning up manually.", this);
                    holePositionsCache.Remove(cell);
                    holeColorCache.Remove(cell);
                    placedHoles.RemoveAll(h => h.position == cell);
                }
            }
            else if (holePositionsCache.Contains(cell))
            {
                Debug.LogWarning($"[LevelRuleTileManager] Found orphaned hole entry at {cell}. Cleaning up cache.", this);
                holePositionsCache.Remove(cell);
                holeColorCache.Remove(cell);
                placedHoles.RemoveAll(h => h.position == cell);
            }
        }

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
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

        // Remove holes first via centralized method
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

        // Clear all hole data
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

    // Query methods
    public bool IsBlocked(Vector2Int pos) => placedTilesCache.Contains(pos);
    public bool IsHole(Vector2Int pos) => holeColorCache.ContainsKey(pos);
    public ColorEnum? GetHoleColor(Vector2Int pos) =>
        holeColorCache.TryGetValue(pos, out ColorEnum color) ? color : null;
    public bool HasInnerTile(Vector2Int pos) => innerTiles.ContainsKey(pos);
    
    public InnerTile GetInnerTile(Vector2Int gridPos)
    {
        if (innerTiles.TryGetValue(gridPos, out var go))
            return go.GetComponent<InnerTile>();
        return null;
    }
    
    public void UpdateHolePos(HoleController holeController, Vector2Int? newPos)
    {
        // 1) Grab old position
        Vector2Int oldPos = holeController.holePos;

        // 2) Deletion path
        if (!newPos.HasValue)
        {
            if (holePositionsCache.Remove(oldPos))
                holeColorCache.Remove(oldPos);

            placedHoles.RemoveAll(po =>
                po.position == oldPos &&
                po.Obj == holeController.gameObject
            );
            return;
        }

        // 3) Add / Move path
        Vector2Int targetPos = newPos.Value;

        // 3a) Clean up old entries
        if (holePositionsCache.Remove(oldPos))
            holeColorCache.Remove(oldPos);

        placedHoles.RemoveAll(po =>
            po.position == oldPos &&
            po.Obj == holeController.gameObject
        );

        // 3b) Add new entries
        holePositionsCache.Add(targetPos);
        holeColorCache[targetPos] = holeController.holeColor;
        placedHoles.Add(new PlacedObj {
            position = targetPos,
            Obj      = holeController.gameObject
        });

        // 3c) Update GameObject and controller
        holeController.transform.position = GetWorldPosition(targetPos.x, targetPos.y);
        holeController.holePos = targetPos;
    }
}
