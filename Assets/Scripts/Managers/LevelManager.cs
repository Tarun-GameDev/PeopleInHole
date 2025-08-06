using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public LevelRuleTileManager levelRuleTileManager;

    public HashSet<Vector2Int> preSpawnedTilePoints;
    public HashSet<Vector2Int> innerTilePoints;
    public HashSet<Vector2Int> holeTilePoints;
    public HashSet<Vector2Int> peopleTilePoints;
    
    public HashSet<Vector2Int> onlyEmptyTilePoints;

    private void Start()
    {
        levelRuleTileManager = FindObjectOfType<LevelRuleTileManager>();
        if (levelRuleTileManager != null)
        {
            InitialiseDataFromTileManager();
        }
    }

    void InitialiseDataFromTileManager()
    {
        preSpawnedTilePoints = levelRuleTileManager.placedTilesCache;
    }
}
