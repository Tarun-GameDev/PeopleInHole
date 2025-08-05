using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdBox : MonoBehaviour
{
    [SerializeField] private Vector2Int currentGridPos;
    private Vector3 currentWorldPos;
    [SerializeField] private GridDirection boxGridDirection;

    LevelRuleTileManager levelRuleTileManager;

    public Vector2Int CurrentGridPos { get => currentGridPos; set => currentGridPos = value; }
    public Vector3 CurrentWorldPos { get => levelRuleTileManager.GetWorldPosition(currentGridPos.x, currentGridPos.y); }
    public GridDirection BoxGridDirection { get => boxGridDirection; set => boxGridDirection = value; }

    private void Start()
    {
        levelRuleTileManager = GameManager.Instance.LevelRuleTileManger;

        currentGridPos = levelRuleTileManager.GetGridPosition(transform.position);
        currentWorldPos = levelRuleTileManager.GetWorldPosition(currentGridPos.x, currentGridPos.y);

        transform.position = currentWorldPos;
    }
    
    public void SetRotationDirection(GridDirection gridDirection)
    {
        boxGridDirection = gridDirection;
        
        var quaternion = transform.rotation;
        quaternion.eulerAngles = new Vector3(0, Utils.GetWorldDirFromGridDir(gridDirection), 0);
        transform.rotation = quaternion;
    }
}
