﻿using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.U2D;
using static UnityEngine.UI.GridLayoutGroup;

public class TileComponent : MonoBehaviour
{
    public Tile Tile;
}

public abstract class Tile
{
    public int X;
    public int Y;

    protected Tile(int x, int y)
    {
        X = x;
        Y = y;
    }

    private GameObject CreateCorner(string type, string corner, string cornerType)
    {
        if (cornerType.Length != 0) corner += "-";

        GameObject spriteObject = new GameObject();

        string spriteName = $"{type}{corner}{cornerType}";
        spriteObject.AddComponent<SpriteRenderer>().sprite = Util.GetWorld().Sprites[spriteName];
        spriteObject.name = spriteName;

        return spriteObject;
    }

    protected GameObject CreateSpriteObject(
        string type,
        string _00 = "",
        string _01 = "",
        string _10 = "",
        string _11 = "")
    {
        GameObject sprite00 = CreateCorner(type, "00", _00);
        GameObject sprite01 = CreateCorner(type, "01", _01);
        GameObject sprite10 = CreateCorner(type, "10", _10);
        GameObject sprite11 = CreateCorner(type, "11", _11);

        sprite00.transform.position += Vector3.left * 0.25f + Vector3.down * 0.25f;
        sprite01.transform.position += Vector3.left * 0.25f + Vector3.up * 0.25f;
        sprite10.transform.position += Vector3.right * 0.25f + Vector3.down * 0.25f;
        sprite11.transform.position += Vector3.right * 0.25f + Vector3.up * 0.25f;

        GameObject fatherObject = new GameObject();

        sprite00.transform.parent = fatherObject.transform;
        sprite01.transform.parent = fatherObject.transform;
        sprite10.transform.parent = fatherObject.transform;
        sprite11.transform.parent = fatherObject.transform;

        return fatherObject;
    }

    public abstract GameObject UpdateSprite();

    public virtual void OnDestroy()
    {
    }
}

public class GroundTile : Tile
{
    public GroundTile(int x, int y) : base(x, y)
    {
    }

    public override GameObject UpdateSprite()
    {
        int left = X - 1;
        int top = Y - 1;
        int right = X + 1;
        int bottom = Y + 1;

        // TODO: CHECK NEIGHBORS

        return CreateSpriteObject("ground");
    }
}

public class RootTile : Tile
{
    private bool[] ConnectedDirections;

    public RootTile(int x, int y) : base(x, y)
    {
        ConnectedDirections = new bool[4];
    }

    public void ConnectWithNeigh(Direction direction)
    {
        World world = Util.GetWorld();
        Tile neighTile = world.GetTile(X + direction.X(), Y + direction.Y());
        if (neighTile is not RootTile neighRoot)
        {
            throw new System.Exception("Cannot connect with non-root tile");
        }

        ConnectedDirections[(int)direction] = true;
        neighRoot.ConnectedDirections[(int)direction.Opposite()] = true;

        UpdateSprite();
    }

    public override void OnDestroy()
    {
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            if (ConnectedDirections[(int)dir])
            {
                World world = Util.GetWorld();
                Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
                if (neighTile is not RootTile neighRoot)
                {
                    throw new System.Exception("How did we end up connected to a non-root tile?");
                }

                neighRoot.ConnectedDirections[(int)dir.Opposite()] = false;
                neighRoot.UpdateSprite();
            }
        }
    }

    public override GameObject UpdateSprite()
    {
        World world = Util.GetWorld();
        GameObject spriteObject = new GameObject();
        spriteObject.name = "root";
        var spriteName = "root";
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            spriteName += ConnectedDirections[(int)dir] ? "1" : "0";
        }
        spriteObject.AddComponent<SpriteRenderer>().sprite = world.Sprites[spriteName];

        return spriteObject;
    }
}