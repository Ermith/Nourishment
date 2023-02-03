using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

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

    protected void CreateSpriteObject(
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

        sprite00.transform.position += Vector3.left * 0.25f + Vector3.down * 0.25f;
        sprite01.transform.position += Vector3.left * 0.25f + Vector3.up * 0.25f;
        sprite10.transform.position += Vector3.right * 0.25f + Vector3.down * 0.25f;
        sprite11.transform.position += Vector3.right * 0.25f + Vector3.up * 0.25f;

        sprite00.transform.parent = transform;
        sprite01.transform.parent = transform;
        sprite10.transform.parent = transform;
        sprite11.transform.parent = transform;
    }

    public abstract void UpdateSprite();

    public abstract bool IsVisible();

    public virtual void OnDestroy()
    {
        World world = Util.GetWorld();
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            neighTile.UpdateSprite();
        }
    }
}

public class GroundTile : Tile
{
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
        CreateSpriteObject("ground", subSpriteLabels);
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