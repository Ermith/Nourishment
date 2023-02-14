using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public enum TileType
{
    Air,
    Ground,
    Root,
    Grass,
    SuperGround,
    EvilGround,
    NutritionGround,
    FlowerGrass
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
        var tile = (RootTile)CreateTile<RootTile>(parent, x, y);
        tile.gameObject.AddComponent<SpriteRenderer>();
        tile.Status = global::RootTile.RootStatus.Spawned;
        return tile;
    }

    public static Tile GrassTile(GameObject parent, int x, int y)
    {
        var tile = CreateTile<GrassTile>(parent, x, y);
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
            case TileType.Grass:
                return GrassTile(parent, x, y);
            case TileType.SuperGround:
                return CreateTile<SuperGroundTile>(parent, x, y);
            case TileType.EvilGround:
                return CreateTile<EvilGroundTile>(parent, x, y);
            case TileType.NutritionGround:
                return CreateTile<NutritionGroundTile>(parent, x, y);
            case TileType.FlowerGrass:
                return CreateTile<FlowerGrassTile>(parent, x, y);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

public abstract class Tile : MonoBehaviour
{
    public int X;
    public int Y;
    public virtual float Hardness => -1;
    public bool Diggable => Hardness >= 0;
    public abstract TileType Type { get; }
    public virtual string Audio => "";

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
    
    public virtual bool CanFluidPass(Fluid fluid, Direction moveDirection)
    {
        return false;
    }
}

public class GroundTile : Tile
{
    public override float Hardness => 4;
    private List<GameObject> _subSpriteObjects = new List<GameObject>();
    public override string Audio => "Dirt";

    public override TileType Type => TileType.Ground;

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();
        
        foreach (var subSprite in _subSpriteObjects)
            Destroy(subSprite);
        
        _subSpriteObjects = Util.GroundLikeSprite(gameObject, "ground", dir =>
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            return neighTile is GroundTile || neighTile is GrassTile;
        });
    }
}

public class SuperGroundTile : GroundTile
{
    public override float Hardness => -1;
    private List<GameObject> _subSpriteObjects = new List<GameObject>();

    public override TileType Type => TileType.SuperGround;

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();

        foreach (var subSprite in _subSpriteObjects)
            Destroy(subSprite);

        _subSpriteObjects = Util.GroundLikeSprite(gameObject, "superGround", dir =>
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            return neighTile is GroundTile || neighTile is GrassTile;
        });

        foreach (var subSprite in _subSpriteObjects)
        {
            subSprite.GetComponent<SpriteRenderer>().color = Color.gray;
        }
    }
}

public class EvilGroundTile : GroundTile
{
    public override float Hardness => 10;
    private List<GameObject> _subSpriteObjects = new List<GameObject>();

    public override TileType Type => TileType.EvilGround;

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();

        foreach (var subSprite in _subSpriteObjects)
            Destroy(subSprite);

        _subSpriteObjects = Util.GroundLikeSprite(gameObject, "evil_ground", dir =>
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            return neighTile is GroundTile || neighTile is GrassTile;
        });
    }
}

public class NutritionGroundTile : GroundTile
{
    public float Nutrition = 12;
    public override float Hardness => 0;
    private List<GameObject> _subSpriteObjects = new List<GameObject>();

    public override TileType Type => TileType.NutritionGround;

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();

        foreach (var subSprite in _subSpriteObjects)
            Destroy(subSprite);

        _subSpriteObjects = Util.GroundLikeSprite(gameObject, "nutrition_ground", dir =>
        {
            Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
            return neighTile is GroundTile || neighTile is GrassTile;
        });
    }
}

public class GrassTile : Tile
{

    public override TileType Type => TileType.Grass;
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

    public override void UpdateSprite()
    {
        World world = Util.GetWorld();
        name = "Grass";
        if (!SpriteRenderer.sprite.IsUnityNull())
            return;
        var subTypes = new string[] { "world_grass_01", "world_grass_03", "world_grass_02", "world_grass_04", "world_grass_left", "world_grass_right" };
        string spriteName;
        switch (Random.value)
        {
            case <= 0.40f:
                spriteName = subTypes[0];
                break;
            case <= 0.70f:
                spriteName = subTypes[1];
                break;
            case <= 0.90f:
                spriteName = subTypes[2];
                break;
            case <= 1.00f:
                spriteName = subTypes[3];
                break;
            default:
                spriteName = subTypes[0];
                break;
        }
        if (X == 0)
            spriteName = subTypes[0];//4
        if (X == World.MAP_WIDTH - 1)
            spriteName = subTypes[0];//5

        SpriteRenderer.sprite = world.Sprites[spriteName];
    }
}

public class AirTile : Tile
{
    public override string Audio => "Root";
    public override TileType Type => TileType.Air;
    public override float Hardness => 1;
    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        return true;
    }

    public override void UpdateSprite()
    {
    }
    public override bool CanFluidPass(Fluid fluid, Direction moveDirection)
    {
        return true;
    }
}

public class FlowerGrassTile : Tile
{
    public override string Audio => "Root";
    public override TileType Type => TileType.FlowerGrass;
    public override float Hardness => -1;
    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        return false;
    }

    public override void UpdateSprite()
    {
    }
    public override bool CanFluidPass(Fluid fluid, Direction moveDirection)
    {
        return false;
    }
}

public class RootNotFoundException : Exception
{
}

public class RootTile : Tile
{
    public override string Audio => "Root";
    public enum RootStatus
    {
        Connected,
        Disconnected,
        Spawned,
        Initial,
    }

    private readonly Color HEALTHY_COLOR = Color.white;
    private readonly Color DAMAGED_COLOR = new Color(0.7f, 0.4f, 0.2f);
    private readonly Color SPAWNED_COLOR = new Color(0.7f, 0.7f, 0.7f);

    public float Health = 1.0f;

    private Tween _colorTween;

    private void changeColor(Color color, float time=0.5f)
    {
        if (time <= 0f)
        {
            SpriteRenderer.color = color;
        }
        else
        {
            _colorTween?.Kill();
            _colorTween = SpriteRenderer.DOColor(color, 0.5f);
        }
    }

    public override void SimulationStep()
    {
        
        if (Status == RootStatus.Disconnected)
        {
            Health -= 0.02f;
        }
        // interpolate color
        if (Status == RootStatus.Spawned)
            changeColor(Color.Lerp(DAMAGED_COLOR, SPAWNED_COLOR, Health));
        else
            changeColor(Color.Lerp(DAMAGED_COLOR, HEALTHY_COLOR, Health));
        if (Health <= 0)
        {
            World world = Util.GetWorld();
            world.ReplaceTile(X, Y, TileType.Air);
        }
    }


    private RootStatus? _status;
    public RootStatus? Status {
        get => _status;
        set
        { 
            _status = value;
            if (_status == RootStatus.Spawned)
                changeColor(Color.Lerp(DAMAGED_COLOR, SPAWNED_COLOR, Health), 0f);
            else
                changeColor(Color.Lerp(DAMAGED_COLOR, HEALTHY_COLOR, Health));
        }
    }

    public override TileType Type => TileType.Root;
    public override float Hardness => 0;
    
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

    public HashSet<RootTile> BFSApply(Action<RootTile> action, Predicate<RootTile> connPredicate = null)
    {
        World world = Util.GetWorld();
        Queue<RootTile> queue = new Queue<RootTile>();
        HashSet<RootTile> visited = new HashSet<RootTile>();
        queue.Enqueue(this);
        while (queue.Count > 0)
        {
            RootTile tile = queue.Dequeue();
            if (visited.Contains(tile))
                continue;
            visited.Add(tile);
            action(tile);
            foreach (Direction direction in Util.CARDINAL_DIRECTIONS)
            {
                if (!tile.ConnectedDirections[(int)direction])
                    continue;
                Tile neighTile = world.GetTile(tile.X + direction.X(), tile.Y + direction.Y());
                if (neighTile is not RootTile neighRoot)
                    continue;
                if (connPredicate is not null && !connPredicate(neighRoot))
                    continue;
                queue.Enqueue(neighRoot);
            }
        }

        return visited;
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

    public void GrowAnim(Direction dir)
    {
        var opdir = dir.Opposite();
        transform.position = new Vector3(X + -World.MAP_WIDTH / 2f + 0.5f + opdir.X(), Y + opdir.Y());
        transform.localScale = new Vector3(dir.X() != 0 ? 0f : 1f, dir.Y() != 0 ? 0f : 1f, 1f);
        transform.DOMove(new Vector3(X + -World.MAP_WIDTH / 2f + 0.5f, Y), 0.2f);
        transform.DOScale(new Vector3(1f, 1f, 1f), 0.2f);
    }

    public override void OnDestroy()
    {
        World world = Util.GetWorld();
        if (world is null)
            return;
        HashSet<RootTile> visited = new HashSet<RootTile>();
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            if (ConnectedDirections[(int)dir])
            {
                Tile neighTile = world.GetTile(X + dir.X(), Y + dir.Y());
                if (neighTile is not RootTile neighRoot)
                {
                    if (Status == RootStatus.Initial)
                        continue;
                    else
                        throw new System.Exception("How did we end up connected to a non-root tile?");
                }

                neighRoot.ConnectedDirections[(int)dir.Opposite()] = false;

                if (!visited.Contains(neighRoot))
                {
                    var curVisited = neighRoot.BFSApply(tile => { },
                        tile => tile.Status == RootStatus.Connected || tile.Status == RootStatus.Initial);
                    var foundInitial = curVisited.Any(tile => tile.Status == RootStatus.Initial);
                    if (!foundInitial)
                    {
                        foreach (var tile in curVisited)
                        {
                            tile.Status = RootStatus.Disconnected;
                        }
                    }
                    visited.UnionWith(curVisited);
                }
            }
        }
        base.OnDestroy();
        var player = Util.GetPlayer();
        if (player && player.X == X && player.Y == Y)
            Util.GetFlower().Nourishment = 0.0f; // kills the Player
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