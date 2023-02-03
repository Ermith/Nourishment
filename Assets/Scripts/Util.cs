using System;
using UnityEngine;

public enum Direction
{
    Right,
    Up,
    Left,
    Down
}

public static class DirectionExtensions
{
    public static Direction Opposite(this Direction direction)
    {
        return direction switch
        {
            Direction.Right => Direction.Left,
            Direction.Up => Direction.Down,
            Direction.Left => Direction.Right,
            Direction.Down => Direction.Up,
            _ => Direction.Right
        };
    }

    public static bool IsHorizontal(this Direction direction)
    {
        return direction is Direction.Right or Direction.Left;
    }

    public static bool IsVertical(this Direction direction)
    {
        return direction is Direction.Up or Direction.Down;
    }

    public static Vector3 ToVector(this Direction direction)
    {
        return Util.CARDINAL_VECTORS[(int)direction];
    }
    
    public static int X(this Direction direction)
    {
        return (int)direction.ToVector().x;
    }

    public static int Y(this Direction direction)
    {
        return (int)direction.ToVector().y;
    }
}

public class Util
{
    public static readonly Vector3[] CARDINAL_VECTORS = new Vector3[]
    {
        Vector3.right,
        Vector3.up,
        Vector3.left,
        Vector3.down
    };

    public static readonly Direction[] CARDINAL_DIRECTIONS = new Direction[]
    {
        Direction.Right,
        Direction.Up,
        Direction.Left,
        Direction.Down
    };

    public static World GetWorld()
    {
        return GameObject.Find("World").GetComponent<World>();
    }
}