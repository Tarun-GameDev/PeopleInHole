using System.ComponentModel;
using UnityEngine;

public static class Utils
{
    public static Vector2Int GetDirection(GridDirection direction)
    {
        return direction switch
        {
            GridDirection.Up => new Vector2Int(0, 1),
            GridDirection.UpRight => new Vector2Int(1, 1),
            GridDirection.Right => new Vector2Int(1, 0),
            GridDirection.DownRight => new Vector2Int(1, -1),
            GridDirection.Down => new Vector2Int(0, -1),
            GridDirection.DownLeft => new Vector2Int(-1, -1),
            GridDirection.Left => new Vector2Int(-1, 0),
            GridDirection.UpLeft => new Vector2Int(-1, 1),
            _ => throw new System.ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static GridDirection GetDirectionFromPos(Vector2Int currentGridPos, Vector2Int gotoGridPos)
    {
        Vector2Int direction = gotoGridPos - currentGridPos;
        if (direction == new Vector2Int(0, 1)) return GridDirection.Up;
        if (direction == new Vector2Int(1, 1)) return GridDirection.UpRight;
        if (direction == new Vector2Int(1, 0)) return GridDirection.Right;
        if (direction == new Vector2Int(1, -1)) return GridDirection.DownRight;
        if (direction == new Vector2Int(0, -1)) return GridDirection.Down;
        if (direction == new Vector2Int(-1, -1)) return GridDirection.DownLeft;
        if (direction == new Vector2Int(-1, 0)) return GridDirection.Left;
        if (direction == new Vector2Int(-1, 1)) return GridDirection.UpLeft;
        throw new WarningException("Invalid direction from positions");
    }

    public static int GetWorldDirFromGridDir(GridDirection gridDirection)
    {
        return gridDirection switch
        {
            GridDirection.Up => 0,
            GridDirection.Right => 90,
            GridDirection.Down => 180,
            GridDirection.Left => -90,
            _ => throw new WarningException("Invalid grid direction for world dir")
        };
    }
    
    public static GridDirection GetGridDirFromWorldDir(float worldDirection)
    {
        // Normalize the angle to handle 270 as -90
        worldDirection = worldDirection % 360;
        if (worldDirection > 180) worldDirection -= 360;
    
        return worldDirection switch
        {
            0 => GridDirection.Up,
            90 => GridDirection.Right,
            180 => GridDirection.Down,
            -90 => GridDirection.Left,
            _ => throw new WarningException($"Invalid world direction for grid dir: {worldDirection}")
        };
    }

    public static GridDirection GetOppositeGridDirection(GridDirection direction)
    {
        return direction switch
        {
            GridDirection.Up => GridDirection.Down,
            GridDirection.Down => GridDirection.Up,
            GridDirection.Left => GridDirection.Right,
            GridDirection.Right => GridDirection.Left,
            _ => throw new System.ArgumentException($"Unsupported direction: {direction}")
        };
    }

    public static Vector2Int GetCardinalDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
    }
}

public enum GridDirection
{
    Up,
    UpRight,
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
    UpLeft,
}

public enum HoleColor
{
    None,
    Red,
    Green,
    Blue,
    Yellow,
    Purple,
    Orange,
    Pink,
    Brown,
    Black,
    White
}
