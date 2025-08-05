using DT.GridSystem.Ruletile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private LevelRuleTileManager levelRuleTileManger;

    public LevelRuleTileManager LevelRuleTileManger { get => levelRuleTileManger; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        levelRuleTileManger = FindObjectOfType<LevelRuleTileManager>();
    }

    private void Start()
    {
        levelRuleTileManger.Init();
    }
}
