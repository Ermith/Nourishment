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
        switch (direction)
        {
            case Direction.Right:
                return Direction.Left;
            case Direction.Up:
                return Direction.Down;
            case Direction.Left:
                return Direction.Right;
            case Direction.Down:
                return Direction.Up;
            default:
                return Direction.Right;
        }
    }

    public static Vector3 ToVector(this Direction direction)
    {
        return Util.CARDINAL_DIRECTIONS[(int)direction];
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
    public static readonly Vector3[] CARDINAL_DIRECTIONS = new Vector3[]
    {
        Vector3.right,
        Vector3.up,
        Vector3.left,
        Vector3.down
    };

    public static World GetWorld()
    {
        return GameObject.Find("World").GetComponent<World>();
    }
}