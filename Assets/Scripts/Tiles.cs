﻿using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum TileType
{
    Air,
    Ground,
    Root,
}

public class TileFactory
{

    private static Tile CreateTile<T>(GameObject parent, int x, int y) where T : Tile
    {
        // Setup game object
        var go = new GameObject();
        go.transform.parent = parent.transform;

        var tile = go.AddComponent<T>();
        tile.X = x; tile.Y = y; // World coordinates
        tile.name = typeof(T).Name;

        // Position in game
        float xOffset = -World.MAP_WIDTH / 2f + 0.5f;
        tile.transform.position = new Vector3(
            x + xOffset,
            y);

        return tile;
    }

    public static Tile GroundTile(GameObject parent, int x, int y)
    {
        return CreateTile<GroundTile>(parent, x, y);
    }

    public static Tile RootTile(GameObject parent, int x, int y)
    {
        var tile = CreateTile<RootTile>(parent, x, y);
        tile.gameObject.AddComponent<SpriteRenderer>();
        return tile;
    }

    public static Tile CreateTile(GameObject parent, int x, int y, TileType type)
    {
        switch (type)
        {
            case TileType.Air:
                return CreateTile<AirTile>(parent, x, y);
            case TileType.Ground:
                return CreateTile<GroundTile>(parent, x, y);
            case TileType.Root:
                return RootTile(parent, x, y);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

public abstract class Tile : MonoBehaviour
{
    public int X;
    public int Y;

    public abstract void UpdateSprite();

    public virtual void OnDestroy()
    {
        World world = Util.GetWorld();
        if (world is null)
            return;
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            if(neighTile)
                neighTile.UpdateSprite();
        }
    }

    public virtual bool CanSpread(Player player, Direction spreadDirection)
    {
        return true;
    }

    public virtual bool CanPass(Entity entity, Direction moveDirection)
    {
        return false;
    }

    public virtual void SimulationStep()
    {
    }
}

public class GroundTile : Tile
{
    private List<GameObject> _subSpriteObjects = new List<GameObject>();

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();
        
        foreach (var subSprite in _subSpriteObjects)
            Destroy(subSprite);
        
        _subSpriteObjects = Util.GroundLikeSprite(gameObject, "ground", dir =>
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            return neighTile is GroundTile;
        });
    }
}

public class AirTile : Tile
{
    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        return true;
    }

    public override void UpdateSprite()
    {
    }
}

public class RootNotFoundException : Exception
{

}

public class RootTile : Tile
{
    public bool Protected;
    
    private bool[] ConnectedDirections;

    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer SpriteRenderer
    {
        get
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            return _spriteRenderer;
        }
        set { _spriteRenderer = value; }
    }

    public RootTile()
    {
        ConnectedDirections = new bool[4];
    }

    public void Start()
    {
    }

    public void ForceConnect(Direction direction)
    {
        ConnectedDirections[(int)direction] = true;
        UpdateSprite();
    }

    public bool ConnectWithNeigh(Direction direction)
    {
        World world = Util.GetWorld();
        Tile neighTile = world.GetTile(X + direction.X(), Y + direction.Y());
        if (neighTile is not RootTile neighRoot)
        {
            return false;
        }

        ConnectedDirections[(int)direction] = true;
        neighRoot.ConnectedDirections[(int)direction.Opposite()] = true;

        UpdateSprite();
        neighRoot.UpdateSprite();
        return true;
    }

    public override void OnDestroy()
    {
        World world = Util.GetWorld();
        if (world is null)
            return;
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            if (ConnectedDirections[(int)dir])
            {
                Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
                if (neighTile is not RootTile neighRoot)
                {
                    if (Protected)
                        continue;
                    else
                        throw new System.Exception("How did we end up connected to a non-root tile?");
                }

                neighRoot.ConnectedDirections[(int)dir.Opposite()] = false;
            }
        }
        base.OnDestroy();
    }

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();
        name = "root";
        var spriteName = "root";
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            spriteName += ConnectedDirections[(int)dir] ? "1" : "0";
        }
        SpriteRenderer.sprite = world.Sprites[spriteName];
    }

    public void ConnectWithAllNeigh()
    {
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
            ConnectWithNeigh(dir);
    }
}