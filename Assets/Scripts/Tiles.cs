using UnityEngine;

public class TileFactory
{
    private static Tile CreateTile<T>(int x, int y) where T : Tile
    {
        var go = new GameObject();
        go.SetActive(false);
        var tile = go.AddComponent<T>();
        tile.X = x;
        tile.Y = y;
        return tile;
    }

    public static Tile GroundTile(int x, int y)
    {
        return CreateTile<GroundTile>(x, y);
    }

    public static Tile RootTile(int x, int y)
    {
        var tile = CreateTile<RootTile>(x, y);
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
        string _00 = "",
        string _01 = "",
        string _10 = "",
        string _11 = ""
        )
    {
        GameObject sprite00 = CreateCorner(type, "00", _00);
        GameObject sprite01 = CreateCorner(type, "01", _01);
        GameObject sprite10 = CreateCorner(type, "10", _10);
        GameObject sprite11 = CreateCorner(type, "11", _11);

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

    public virtual void OnDestroy()
    {
    }
}

public class GroundTile : Tile
{
    public override void UpdateSprite()
    {
        CreateSpriteObject("ground");
    }
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
            throw new System.Exception("Cannot connect with non-root tile");
        }

        ConnectedDirections[(int)direction] = true;
        neighRoot.ConnectedDirections[(int)direction.Opposite()] = true;

        UpdateSprite();
        neighRoot.UpdateSprite();
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
}