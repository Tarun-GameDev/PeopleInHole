// LevelRuleTileManager.cs
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

    [SerializeField] private GameObject playerGroupPrefab;
    [SerializeField] private GameObject playerGroupParent;

    // In-scene caches (not serialized)
    private Dictionary<Vector2Int, GameObject> innerTiles = new();
    public HashSet<Vector2Int> placedTilesCache = new();
    public HashSet<Vector2Int> innerTilePositionsCache = new();    // ← re-added

    private Dictionary<Vector2Int, GameObject> holeGameObjects = new();
    public HashSet<Vector2Int> holePositionsCache = new();
    private Dictionary<Vector2Int, ColorEnum> holeColorCache = new();

    private Dictionary<Vector2Int, GameObject> playerGameObjects = new();
    public HashSet<Vector2Int> playerPositionsCache = new();
    private Dictionary<Vector2Int, ColorEnum> playerColorCache = new();

    // Serialized data for persistence
    [SerializeField] private List<PlacedObj> innerTileList = new();
    [SerializeField] private List<PlacedObj> placedHoles = new();
    [SerializeField] private List<PlacedObj> placedPlayers = new();

    [HideInInspector] public ColorEnum selectedHoleColor;
    [HideInInspector] public ColorEnum selectedPlayerColor;

    /// <summary>
    /// Call this on Awake/Start to rebuild dictionaries from serialized data.
    /// </summary>
    public void Init()
    {
        RebuildDictionary();
        RebuildInnerTilesDictionary();

        placedTilesCache = new HashSet<Vector2Int>(placedTiles.Keys);

        // Holes
        holePositionsCache    = new HashSet<Vector2Int>();
        holeColorCache        = new Dictionary<Vector2Int, ColorEnum>();
        holeGameObjects       = new Dictionary<Vector2Int, GameObject>();
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
                    holeGameObjects[hole.position] = hole.Obj;
                }
                else
                {
                    Debug.LogWarning($"[LevelRuleTileManager] Hole at {hole.position} missing HoleController; removing.", this);
                    placedHoles.Remove(hole);
                }
            }
            else
            {
                Debug.LogWarning($"[LevelRuleTileManager] Null hole object at {hole.position}; removing.", this);
                placedHoles.Remove(hole);
            }
        }

        // Player Groups
        playerPositionsCache  = new HashSet<Vector2Int>();
        playerColorCache      = new Dictionary<Vector2Int, ColorEnum>();
        playerGameObjects     = new Dictionary<Vector2Int, GameObject>();
        foreach (var p in placedPlayers.ToList())
        {
            if (p.Obj != null)
            {
                var ctrl = p.Obj.GetComponent<PlayerGroup>();
                if (ctrl != null)
                {
                    playerPositionsCache.Add(p.position);
                    playerColorCache[p.position] = ctrl.playerColor;
                    ctrl.playerPos = p.position;
                    ctrl.UpdatePlayerMaterials();
                    playerGameObjects[p.position] = p.Obj;
                }
                else
                {
                    Debug.LogWarning($"[LevelRuleTileManager] Player at {p.position} missing PlayerGroup; removing.", this);
                    placedPlayers.Remove(p);
                }
            }
            else
            {
                Debug.LogWarning($"[LevelRuleTileManager] Null player object at {p.position}; removing.", this);
                placedPlayers.Remove(p);
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
                Debug.LogWarning($"[LevelRuleTileManager] Null inner tile at {tile.position}; removing.", this);
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
        foreach (var kv in holeGameObjects)
            if (kv.Value != null)
                placedHoles.Add(new PlacedObj { position = kv.Key, Obj = kv.Value });
    }

    private void SyncPlayerGroupList()
    {
        placedPlayers.Clear();
        foreach (var kv in playerGameObjects)
            if (kv.Value != null)
                placedPlayers.Add(new PlacedObj { position = kv.Key, Obj = kv.Value });
    }

    private GameObject FindHoleGameObject(Vector2Int position)
    {
        if (holeEmptyParent == null) return null;
        foreach (Transform child in holeEmptyParent.transform)
        {
            var ctrl = child.GetComponent<HoleController>();
            if (ctrl != null && GetGridPosition(child.position) == position)
                return child.gameObject;
        }
        return null;
    }

    private GameObject FindPlayerGroupGameObject(Vector2Int position)
    {
        if (playerGroupParent == null) return null;
        foreach (Transform child in playerGroupParent.transform)
        {
            var ctrl = child.GetComponent<PlayerGroup>();
            if (ctrl != null && GetGridPosition(child.position) == position)
                return child.gameObject;
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
        foreach (var kv in innerTiles.ToList())
            if (kv.Value != null)
                DestroyImmediate(kv.Value);
        innerTiles.Clear();

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

    public void RemoveInnerTiles()
    {
        Undo.RecordObject(this, "Remove Inner Tiles");
        foreach (var kv in innerTiles.ToList())
            if (kv.Value != null)
                DestroyImmediate(kv.Value);
        innerTiles.Clear();
        SyncInnerTileList();
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public void SpawnHole()
    {
        if (holePrefab == null || holeEmptyParent == null)
        {
            Debug.LogError("[LevelRuleTileManager] Hole Prefab or Parent not assigned!", this);
            return;
        }
        if (selectedCells == null || selectedCells.Count == 0)
        {
            Debug.LogWarning("[LevelRuleTileManager] No cells selected for hole.", this);
            return;
        }

        Undo.RecordObject(this, "Spawn Hole");
        foreach (var cell in selectedCells)
        {
            if (holePositionsCache.Contains(cell))
            {
                Debug.LogWarning($"Already a hole at {cell}. Skipping.", this);
                continue;
            }
            if (placedTilesCache.Contains(cell))
            {
                Debug.LogWarning($"Tile blocks hole at {cell}. Skipping.", this);
                continue;
            }

            Vector3 spawnPos = GetWorldPosition(cell.x, cell.y);
            var holeGO = (GameObject)PrefabUtility.InstantiatePrefab(holePrefab, holeEmptyParent.transform);
            holeGO.transform.position = spawnPos;

            var ctrl = holeGO.GetComponent<HoleController>();
            if (ctrl == null)
            {
                Debug.LogError($"HoleController missing on prefab! Destroying.", this);
                DestroyImmediate(holeGO);
                continue;
            }

            ctrl.holeColor = selectedHoleColor;
            ctrl.UpdateHoleMaterials();
            UpdateHolePos(ctrl, cell);
            Debug.Log($"Spawned hole at {cell} ({selectedHoleColor})", this);
        }

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public void RemoveHoles()
    {
        if (selectedCells == null || selectedCells.Count == 0)
        {
            Debug.LogWarning("[LevelRuleTileManager] No cells selected to remove holes.", this);
            return;
        }

        Undo.RecordObject(this, "Remove Holes");
        foreach (var cell in selectedCells.ToList())
        {
            var entry = placedHoles.Find(h => h.position == cell);
            if (entry.Obj != null)
            {
                var ctrl = entry.Obj.GetComponent<HoleController>();
                if (ctrl != null)
                {
                    UpdateHolePos(ctrl, null);
                    DestroyImmediate(entry.Obj);
                    Debug.Log($"Removed hole at {cell}", this);
                }
                else
                {
                    Debug.LogWarning($"HoleController missing at {cell}. Cleaning caches.", this);
                    holePositionsCache.Remove(cell);
                    holeColorCache.Remove(cell);
                    placedHoles.RemoveAll(h => h.position == cell);
                }
            }
            else if (holePositionsCache.Contains(cell))
            {
                Debug.LogWarning($"Orphaned hole at {cell}. Cleaning caches.", this);
                holePositionsCache.Remove(cell);
                holeColorCache.Remove(cell);
                placedHoles.RemoveAll(h => h.position == cell);
            }
        }

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public void SpawnPlayerGroup()
    {
        if (playerGroupPrefab == null || playerGroupParent == null)
        {
            Debug.LogError("[LevelRuleTileManager] Player Prefab or Parent not assigned!", this);
            return;
        }
        if (selectedCells == null || selectedCells.Count == 0)
        {
            Debug.LogWarning("[LevelRuleTileManager] No cells selected for player.", this);
            return;
        }

        Undo.RecordObject(this, "Spawn Player Group");
        foreach (var cell in selectedCells)
        {
            if (playerPositionsCache.Contains(cell))
            {
                Debug.LogWarning($"Already a player at {cell}. Skipping.", this);
                continue;
            }
            if (placedTilesCache.Contains(cell) || holePositionsCache.Contains(cell))
            {
                Debug.LogWarning($"Cell {cell} is blocked. Skipping.", this);
                continue;
            }

            Vector3 spawnPos = GetWorldPosition(cell.x, cell.y);
            var pgGO = (GameObject)PrefabUtility.InstantiatePrefab(playerGroupPrefab, playerGroupParent.transform);
            pgGO.transform.position = spawnPos;

            var ctrl = pgGO.GetComponent<PlayerGroup>();
            if (ctrl == null)
            {
                Debug.LogError($"PlayerGroup missing on prefab! Destroying.", this);
                DestroyImmediate(pgGO);
                continue;
            }

            ctrl.playerColor = selectedPlayerColor;
            ctrl.UpdatePlayerMaterials();
            UpdatePlayerPos(ctrl, cell);
            Debug.Log($"Spawned player at {cell} ({selectedPlayerColor})", this);
        }

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public void RemovePlayerGroups()
    {
        if (selectedCells == null || selectedCells.Count == 0)
        {
            Debug.LogWarning("[LevelRuleTileManager] No cells selected to remove players.", this);
            return;
        }

        Undo.RecordObject(this, "Remove Player Groups");
        foreach (var cell in selectedCells.ToList())
        {
            var entry = placedPlayers.Find(p => p.position == cell);
            if (entry.Obj != null)
            {
                var ctrl = entry.Obj.GetComponent<PlayerGroup>();
                if (ctrl != null)
                {
                    UpdatePlayerPos(ctrl, null);
                    DestroyImmediate(entry.Obj);
                    Debug.Log($"Removed player at {cell}", this);
                }
                else
                {
                    Debug.LogWarning($"PlayerGroup missing at {cell}. Cleaning caches.", this);
                    playerPositionsCache.Remove(cell);
                    playerColorCache.Remove(cell);
                    placedPlayers.RemoveAll(p => p.position == cell);
                }
            }
            else if (playerPositionsCache.Contains(cell))
            {
                Debug.LogWarning($"Orphaned player at {cell}. Cleaning caches.", this);
                playerPositionsCache.Remove(cell);
                playerColorCache.Remove(cell);
                placedPlayers.RemoveAll(p => p.position == cell);
            }
        }

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
        RemovePlayerGroups();
        RemoveHoles();
        base.DeleteSelectedTiles();
        GenerateInnerGrid();
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public override void DeleteAllChildren()
    {
        Undo.RecordObject(this, "Delete All Children");

        // Clear holes
        holePositionsCache.Clear();
        holeColorCache.Clear();
        placedHoles.Clear();
        foreach (Transform child in holeEmptyParent?.transform ?? new GameObject().transform)
        {
            if (child.GetComponent<HoleController>() != null)
                DestroyImmediate(child.gameObject);
        }

        // Clear players
        playerPositionsCache.Clear();
        playerColorCache.Clear();
        placedPlayers.Clear();
        foreach (Transform child in playerGroupParent?.transform ?? new GameObject().transform)
        {
            if (child.GetComponent<PlayerGroup>() != null)
                DestroyImmediate(child.gameObject);
        }

        // Base clears placedTiles and children
        base.DeleteAllChildren();

        // Inner tiles
        RemoveInnerTiles();

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public void CleanupOrphanedEntries()
    {
        // Holes
        for (int i = placedHoles.Count - 1; i >= 0; i--)
        {
            var hole = placedHoles[i];
            if (hole.Obj == null)
            {
                placedHoles.RemoveAt(i);
                holePositionsCache.Remove(hole.position);
                holeColorCache.Remove(hole.position);
                Debug.Log($"Cleaned orphaned hole at {hole.position}");
            }
        }

        // Players
        for (int i = placedPlayers.Count - 1; i >= 0; i--)
        {
            var p = placedPlayers[i];
            if (p.Obj == null)
            {
                placedPlayers.RemoveAt(i);
                playerPositionsCache.Remove(p.position);
                playerColorCache.Remove(p.position);
                Debug.Log($"Cleaned orphaned player at {p.position}");
            }
        }

        // Inner tiles
        foreach (var kv in innerTiles.ToList())
        {
            if (kv.Value == null)
            {
                innerTiles.Remove(kv.Key);
                Debug.Log($"Cleaned orphaned inner tile at {kv.Key}");
            }
        }

        SyncInnerTileList();
        SyncHoleList();
        SyncPlayerGroupList();
    }
#endif

    // Query methods
    public bool IsBlocked(Vector2Int pos) => placedTilesCache.Contains(pos);
    
    public bool IsHole(Vector2Int pos) => holePositionsCache.Contains(pos);
    public ColorEnum? GetHoleColor(Vector2Int pos) =>
        holeColorCache.TryGetValue(pos, out var hc) ? hc : (ColorEnum?)null;

    public bool HasInnerTile(Vector2Int pos) => innerTilePositionsCache.Contains(pos);
    
    public InnerTile GetInnerTile(Vector2Int gridPos) =>
        innerTiles.TryGetValue(gridPos, out var go) ? go.GetComponent<InnerTile>() : null;

    public bool IsPlayerGroup(Vector2Int pos) => playerPositionsCache.Contains(pos);
    public ColorEnum? GetPlayerGroupColor(Vector2Int pos) =>
        playerColorCache.TryGetValue(pos, out var pc) ? pc : (ColorEnum?)null;
    public PlayerGroup GetPlayerGroup(Vector2Int gridPos) =>
        playerGameObjects.TryGetValue(gridPos, out var go) ? go.GetComponent<PlayerGroup>() : null;

    public void RemovePlayerGroup(Vector2Int pos)
    {
        playerPositionsCache.Remove(pos);
        playerColorCache.Remove(pos);
        playerGameObjects.Remove(pos);
    }

    public void RemoveHole(Vector2Int pos)
    {
        holePositionsCache.Remove(pos);
        holeColorCache.Remove(pos);
        holeGameObjects.Remove(pos);
    }

    public void UpdateHolePos(HoleController holeController, Vector2Int? newPos)
    {
        Vector2Int oldPos = holeController.holePos;
        if (!newPos.HasValue)
        {
            if (holePositionsCache.Remove(oldPos))
                holeColorCache.Remove(oldPos);
            placedHoles.RemoveAll(po => po.position == oldPos && po.Obj == holeController.gameObject);
            holeGameObjects.Remove(oldPos);
            return;
        }

        Vector2Int target = newPos.Value;
        if (holePositionsCache.Remove(oldPos))
            holeColorCache.Remove(oldPos);
        placedHoles.RemoveAll(po => po.position == oldPos && po.Obj == holeController.gameObject);
        holeGameObjects.Remove(oldPos);

        holePositionsCache.Add(target);
        holeColorCache[target] = holeController.holeColor;
        placedHoles.Add(new PlacedObj { position = target, Obj = holeController.gameObject });
        holeGameObjects[target] = holeController.gameObject;

        holeController.transform.position = GetWorldPosition(target.x, target.y);
        holeController.holePos = target;
    }

    public void UpdatePlayerPos(PlayerGroup ctrl, Vector2Int? newPos)
    {
        Vector2Int oldPos = ctrl.playerPos;
        if (!newPos.HasValue)
        {
            if (playerPositionsCache.Remove(oldPos))
                playerColorCache.Remove(oldPos);
            placedPlayers.RemoveAll(po => po.position == oldPos && po.Obj == ctrl.gameObject);
            playerGameObjects.Remove(oldPos);
            return;
        }

        Vector2Int target = newPos.Value;
        if (playerPositionsCache.Remove(oldPos))
            playerColorCache.Remove(oldPos);
        placedPlayers.RemoveAll(po => po.position == oldPos && po.Obj == ctrl.gameObject);
        playerGameObjects.Remove(oldPos);

        playerPositionsCache.Add(target);
        playerColorCache[target] = ctrl.playerColor;
        placedPlayers.Add(new PlacedObj { position = target, Obj = ctrl.gameObject });
        playerGameObjects[target] = ctrl.gameObject;

        ctrl.transform.position = GetWorldPosition(target.x, target.y);
        ctrl.playerPos = target;
        ctrl.UpdatePlayerMaterials();
    }
}
