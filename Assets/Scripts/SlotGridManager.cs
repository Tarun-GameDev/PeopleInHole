using DT.GridSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotGridManager : GridSystem3D<int>
{
    [SerializeField] Vector2 gridSpacing = new Vector2(1f, 1f);

    public override Vector3 GetWorldPosition(int x, int y, bool snapToGrid = true)
    {
        if (snapToGrid)
        {
            return (new Vector3((x - gridSize.x / 2f) * gridSpacing.x, 0, (y - gridSize.y / 2f) * gridSpacing.y) + transform.position) + new Vector3(gridSpacing.x, 0, gridSpacing.y) * 0.5f;
        }
        else
        {
            return new Vector3((x - gridSize.x / 2f) * gridSpacing.x, 0, (y - gridSize.y / 2f) * gridSpacing.y) + transform.position;
        }
    }

    public override void GetGridPosition(Vector3 worldPosition, out int x, out int y)
    {
        float relativeX = (worldPosition.x - transform.position.x) / gridSpacing.x;
        float relativeY = (worldPosition.z - transform.position.z) / gridSpacing.y;

        x = Mathf.FloorToInt(relativeX + gridSize.x / 2f);
        y = Mathf.FloorToInt(relativeY + gridSize.y / 2f);

        x = Mathf.Clamp(x, 0, gridSize.x - 1);
        y = Mathf.Clamp(y, 0, gridSize.y - 1);
    }
}
