using System;
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
        var go = new GameObject();
        go.SetActive(true);
        go.transform.parent = parent.transform;
        var tile = go.AddComponent<T>();
        tile.X = x;
        tile.Y = y;
        tile.name = typeof(T).Name;
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

    private GameObject CreateCorner(string type, string corner, string cornerType)
    {
        if (cornerType.Length != 0) corner += "-";

        GameObject spriteObject = new GameObject();

        string spriteName = $"{type}{corner}{cornerType}";
        spriteObject.AddComponent<SpriteRenderer>().sprite = Util.GetWorld().Sprites[spriteName];
        spriteObject.name = spriteName;

        return spriteObject;
    }

    protected List<GameObject> CreateSpriteObject(
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
            subSprite.transform.parent = transform;
            subSprite.transform.position = transform.position;
        }

        sprite00.transform.position += Vector3.left * 0.25f + Vector3.down * 0.25f;
        sprite01.transform.position += Vector3.left * 0.25f + Vector3.up * 0.25f;
        sprite10.transform.position += Vector3.right * 0.25f + Vector3.down * 0.25f;
        sprite11.transform.position += Vector3.right * 0.25f + Vector3.up * 0.25f;

        return subSprites;
    }

    public abstract void UpdateSprite();

    public abstract bool IsVisible();

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

    public override bool IsVisible()
    {
        bool isVisible = false;

        var renderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
            isVisible |= renderer.isVisible;

        return isVisible;
    }

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();
        bool[,] cornerGroundCounts = new bool[4,2];
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            if (neighTile is GroundTile)
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

        foreach (var child in _subSpriteObjects)
            Destroy(child);
        
        _subSpriteObjects = CreateSpriteObject("ground", subSpriteLabels);
    }
}

public class AirTile : Tile
{
    public override bool IsVisible()
    {
        return false;
    }

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

    public void ConnectWithNeigh(Direction direction)
    {
        World world = Util.GetWorld();
        Tile neighTile = world.GetTile(X + direction.X(), Y + direction.Y());
        if (neighTile is not RootTile neighRoot)
        {
            throw new RootNotFoundException();
        }

        ConnectedDirections[(int)direction] = true;
        neighRoot.ConnectedDirections[(int)direction.Opposite()] = true;

        UpdateSprite();
        neighRoot.UpdateSprite();
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

    public override bool IsVisible() => SpriteRenderer.isVisible;
}