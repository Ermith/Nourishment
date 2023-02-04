using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

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

    public static Direction Clockwise(this Direction direction)
    {
        return direction switch
        {
            Direction.Right => Direction.Down,
            Direction.Up => Direction.Right,
            Direction.Left => Direction.Up,
            Direction.Down => Direction.Left,
            _ => Direction.Right
        };
    }

    public static float Angle(this Direction direction)
    {
        return direction switch
        {
            Direction.Right => 0.0f,
            Direction.Up => 90.0f,
            Direction.Left => 180.0f,
            Direction.Down => 270.0f,
            _ => 0.0f
        };
    }

    public static Direction CounterClockwise(this Direction direction)
    {
        return direction switch
        {
            Direction.Right => Direction.Up,
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
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
        return GameObject.Find("World")?.GetComponent<World>();
    }

    public static Flower GetFlower()
    {
        return GameObject.Find("Flower")?.GetComponent<Flower>();
    }

    public static EntityFactory GetEntityFactory()
    {
        return GameObject.Find("EntityFactory")?.GetComponent<EntityFactory>();
    }

    public static List<GameObject> CreateSpriteObject(
        GameObject parent,
        string type,
        string[] labels = null
    )
    {
        if (labels is null)
            labels = Array.Empty<string>();

        GameObject sprite00 = CreateCorner(type, "00", labels.ElementAtOrDefault(0) ?? "");
        GameObject sprite01 = CreateCorner(type, "01", labels.ElementAtOrDefault(1) ?? "");
        GameObject sprite10 = CreateCorner(type, "10", labels.ElementAtOrDefault(2) ?? "");
        GameObject sprite11 = CreateCorner(type, "11", labels.ElementAtOrDefault(3) ?? "");

        var subSprites = new List<GameObject>(new[] { sprite00, sprite01, sprite10, sprite11 });

        foreach (var subSprite in subSprites)
        {
            subSprite.transform.parent = parent.transform;
            subSprite.transform.position = parent.transform.position;
        }

        sprite00.transform.position += Vector3.left * 0.25f + Vector3.down * 0.25f;
        sprite01.transform.position += Vector3.left * 0.25f + Vector3.up * 0.25f;
        sprite10.transform.position += Vector3.right * 0.25f + Vector3.down * 0.25f;
        sprite11.transform.position += Vector3.right * 0.25f + Vector3.up * 0.25f;

        return subSprites;
    }

    private static GameObject CreateCorner(string type, string corner, string cornerType)
    {
        if (cornerType.Length != 0) corner += "-";

        GameObject spriteObject = new GameObject();

        string spriteName = $"{type}{corner}{cornerType}";
        spriteObject.AddComponent<SpriteRenderer>().sprite = Util.GetWorld().Sprites[spriteName];
        spriteObject.name = spriteName;

        return spriteObject;
    }

    public static List<GameObject> GroundLikeSprite(GameObject parent, string type, Predicate<Direction> connectsTo)
    {
        bool[,] cornerGroundCounts = new bool[4, 2];
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            if (connectsTo(dir))
            {
                int horiz = dir.IsHorizontal() ? 1 : 0;
                cornerGroundCounts[(int)(dir + 1) % 4, horiz] = true;
                cornerGroundCounts[(int)(dir + 2) % 4, horiz] = true;
            }
        }

        string[] subSpriteLabels = new string[4];
        for (int i = 0; i < 4; i++)
        {
            subSpriteLabels[i] = (cornerGroundCounts[i, 0], cornerGroundCounts[i, 1]) switch
            {
                (false, false) => "corner",
                (true, false) => "hedge",
                (false, true) => "vedge",
                (true, true) => "",
            };
        }

        (subSpriteLabels[0], subSpriteLabels[1], subSpriteLabels[2], subSpriteLabels[3]) = (subSpriteLabels[0], subSpriteLabels[3], subSpriteLabels[1], subSpriteLabels[2]);

        return CreateSpriteObject(parent, type, subSpriteLabels);
    }
}