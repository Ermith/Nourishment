using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GridSquare
{
    public int X;
    public int Y;
    public Tile Tile;
    public List<Entity> Entities = new List<Entity>();
    public Water Water = new Water();
    public bool Active = true;
    
    public GridSquare(int x, int y, Tile tile = null)
    {
        X = x;
        Y = y;
        Tile = tile;
    }

    public bool CanSpread(Player player, Direction spreadDirection)
    {
        if (Tile != null && !Tile.CanSpread(player, spreadDirection))
            return false;

        foreach (var entity in Entities)
        {
            if (!entity.CanSpread(player, spreadDirection))
                return false;
        }

        return true;
    }

    public void SimulationStep()
    {
        Tile.SimulationStep();
    }

    public void OnSpread(Player player, Direction spreadDirection)
    {
        foreach (var entity in new List<Entity>(Entities))
        {
            entity.OnSpread(player, spreadDirection);
        }
    }

    public bool CanPass(Entity entity, Direction moveDirection)
    {
        if (Tile != null && !Tile.CanPass(entity, moveDirection))
            return false;
        foreach (var e in new List<Entity>(Entities))
        {
            if (e != entity && !e.CanPass(entity, moveDirection))
                return false;
        }

        return true;
    }

    public void OnPass(Entity entity, Direction moveDirection)
    {
        foreach (var e in new List<Entity>(Entities))
        {
            if (e != entity)
                e.OnPass(entity, moveDirection);
        }
    }

    public void SetActive(bool active)
    {
        if (active == this.Active)
            return;
        this.Active = active;
        Tile?.gameObject.SetActive(active);
        /*
        foreach (var entity in Entities)
            entity.ActivityChange(active);
        */
    }

    public void SimulationStepFluid()
    {
        if (Water.Amount <= 0f)
            return;
        float absorbAmount = 0f;
        (int, int)[] neighTileCoords = new (int, int)[]
        {
            (X, Y),
            (X, Y - 1),
            (X + 1, Y),
            (X, Y + 1),
            (X - 1, Y),
        };
        foreach (var (x, y) in neighTileCoords)
        {
            if (Util.GetWorld().GetTile(x, y) is RootTile { Status: RootTile.RootStatus.Connected } root)
            {
                absorbAmount += World.WaterConversionSpeed;
                if (root.Health < 1f)
                {
                    root.Health += World.WaterHealingRatio;
                }
            }
        }

        // hack to clean up ugly worldgen water in root tiles
        if (Util.GetWorld().GetTile(X, Y) is RootTile { Status: RootTile.RootStatus.Spawned })
        {
            Water.Amount = 0f;
            return;
        }

        absorbAmount = Mathf.Min(Water.Amount, absorbAmount);
        Water.Amount -= absorbAmount;
        Util.GetFlower().ModifyNourishmentWithSource(absorbAmount * World.WaterConversionRatio, Tile.gameObject);

        // TODO visual / sound effect?
    }

    public void SimulationSubStepFluid()
    {
        Water.Flow(this.X, this.Y);
    }
    public void SimulationSubStepFluidFinish()
    {
        Water.SimSwap();
    }
    
    public bool CanFluidPass(Fluid fluid, Direction moveDirection)
    {
        if (Tile != null && !Tile.CanFluidPass(fluid, moveDirection))
            return false;
        foreach (var entity in Entities)
        {
            if (!entity.CanFluidPass(fluid, moveDirection))
                return false;
        }

        return true;
    }

    public bool PushesOutFluid(Fluid fluid, Direction moveDirection)
    {
        if (Tile != null && Tile.PushesOutFluid(fluid, moveDirection))
            return true;
        foreach (var entity in Entities)
        {
            if (!entity.CanFluidPass(fluid, moveDirection))
                return true;
        }

        return false;
    }
}

public class World : MonoBehaviour
{
    public UnityEvent<bool> WorldSimulationStepEvent;

    public static int MinTileEntityY = -3;
    public static int MinEntityY = -5;
    public const int TileSize = 32;
    public static int MapWidth = 23;
    public const int ChunkSize = 20;
    public const int FluidSubsteps = 10;
    public const float WaterConversionRatio = 25f; //! how much nourishment you get per 1 tile of water
    public const float WaterConversionSpeed = 0.015f; //! how much water do you absorb per 1 tick per water/root boundary
    public const float WaterHealingRatio = 0.03f; //! how much health do you get by being next to water
    public Camera Camera;
    public Dictionary<string, Sprite> Sprites;
    public Player Player;
    public int ExtraSimulatedRows = 10;
    public bool CheatsEnabled = false;

    public SpriteRenderer Background1;
    public SpriteRenderer Background2;
    public SpriteRenderer Background3;
    public SpriteRenderer Background4;
    private float _start1;
    private float _start2;
    private float _start3;
    private float _start4;
    //private SpriteRenderer CurrentBackground;


    private List<GridSquare[]> _tiles = new List<GridSquare[]>();

    private Vector3 ComputeParallax(ref float start)
    {
        float length = Background1.size.y / 2f;
        float parallax = 0.5f;

        float temp = Camera.transform.position.y * (1 - parallax);
        float dist = Camera.transform.position.y * parallax;

        float oldStart = start;

        if (temp > _start1 + length)
            start += length;

        if (temp < _start1 - length)
            start -= length;

        return Vector3.up * (oldStart + dist) + Vector3.forward * 100;
    }

    public void MoveBackground()
    {
        Background1.transform.position = ComputeParallax(ref _start1);
        Background2.transform.position = ComputeParallax(ref _start2);
        Background3.transform.position = ComputeParallax(ref _start3);
        Background4.transform.position = ComputeParallax(ref _start4);
    }

    public static bool InBounds((int, int) coords)
    {
        return coords.Item1 >= 0 && coords.Item1 < MapWidth && coords.Item2 <= 0;
    }

    public GridSquare GetSquare(int x, int y, bool allowGeneration = false)
    {
        if (!InBounds((x, y)))
            return null;
        while (-y >= _tiles.Count && allowGeneration)
            GenerateMoreMap();
        if (-y >= _tiles.Count)
            return null;
        return _tiles[-y][x];
    }

    public GridSquare AddSquare(int x, int y)
    {
        _tiles[-y][x] = new GridSquare(x, y);
        return _tiles[-y][x];
    }

    public Tile GetTile(int x, int y, bool allowGeneration = false)
    {
        return GetSquare(x, y, allowGeneration)?.Tile;
    }

    // Start is called before the first frame update
    void Start()
    {
        DOTween.SetTweensCapacity(1500, 50);

        Sprites = LoadSprites();
        Util.GetAudioManager().Play("Music", true);

        // Generation of world
        GenerateMoreMap();
        _start1 = Background1.transform.position.y;
        _start2 = Background2.transform.position.y;
        _start3 = Background3.transform.position.y;
        _start4 = Background3.transform.position.y;
    }

    public Tile ReplaceTile(int x, int y, TileType type)
    {
        var square = GetSquare(x, y);

        var oldTile = square.Tile;

        if (oldTile != null)
        {
            Destroy(oldTile.gameObject);
            oldTile.OnRemove();
        }

        var newTile = TileFactory.CreateTile(gameObject, x, y, type);
        newTile.UpdateSprite();
        square.Tile = newTile;

        foreach (var dir in Util.CardinalDirections)
        {
            var neighbor = GetTile(x + dir.X(), y + dir.Y());
            if (neighbor != null)
                neighbor.UpdateSprite();
        }

        return newTile;
    }

    private Tile RandomTile(int x, int y)
    {
        // First row
        if (y == 0 && x == MapWidth / 2)
            return TileFactory.CreateTile(gameObject, x, y, TileType.FlowerGrass);
        
        if (y == 0)
            return TileFactory.CreateTile(gameObject, x, y, TileType.Grass);

        if (y == -1 && x == MapWidth / 2)
        {
            RootTile tile = TileFactory.CreateTile(gameObject, x, y, TileType.Root) as RootTile;
            tile.ForceConnect(Direction.Up);
            tile.Status = RootTile.RootStatus.Initial;
            return tile;
        }

        if (x == 0 || x == MapWidth - 1)
            return TileFactory.CreateTile(gameObject, x, y, TileType.SuperGround);


        var rnd = new System.Random();
        float prob = (float)rnd.NextDouble() * 100;

        float evil = 40 + Mathf.Min(20, -y / ChunkSize * 1.5f);
        float nutrition = evil + 3;
        float air = 20;
        float root = 10 - (-y / ChunkSize);
        float water = 18 - (-y / ChunkSize * 1.5f);
        water = Mathf.Max(water, 2f);

        if (y < MinTileEntityY)
        {
            if (prob < root)
                return TileFactory.CreateTile(gameObject, x, y, TileType.Root);

            if (prob < air)
            {
                if (prob < water)
                    GetSquare(x, y).Water.Amount = prob / water;

                return TileFactory.CreateTile(gameObject, x, y, TileType.Air);
            }

            if (prob < evil)
                return TileFactory.CreateTile(gameObject, x, y, TileType.EvilGround);
            if (prob < nutrition)
                return TileFactory.CreateTile(gameObject, x, y, TileType.NutritionGround);
        }

        return TileFactory.CreateTile(gameObject, x, y, TileType.Ground);
    }

    private Entity RandomEntity(int x, int y)
    {
        if (y >= MinEntityY)
            return null;

        if (x == 0 || x == MapWidth - 1)
            return null;

        float amberBee = 0.25f;
        float bigRock = 2;
        float smallRock = 6;
        float snail = 8;
        float slug = 15;

        var rnd = new System.Random();
        float prob = (float)rnd.NextDouble() * 100;
        Tile tile = GetTile(x, y);
        EntityType? type = null;

        if (prob < snail && tile is AirTile)
            type = EntityType.Snail;

        else if (prob < slug && tile is AirTile)
            type = EntityType.Slug;

        else if (prob < amberBee)
            type = EntityType.AmberBee;

        else if (prob < bigRock)
        {
            type = Util.WeightedPick(new Dictionary<EntityType, float>()
            {
                { EntityType.SquareRock , 2f },
                { EntityType.RandomRock3X3 , 3f },
                { EntityType.RandomRock4X4 , 2f },
                { EntityType.RandomRock5X5 , 0.5f },
                { EntityType.RandomRock6X6 , 0.05f },
                { EntityType.RandomRock6X3 , 0.1f },
                { EntityType.RandomRock3X6 , 0.1f }
            });
        }

        else if (prob < smallRock)
            type = EntityType.SmallRock;

        if (type == null)
        {
            return null;
        }

        int tries = 10;
        Entity e;
        do
        {
            e = Util.GetEntityFactory().PlaceEntity(gameObject, type.Value, x, y);
            if (e is null || !e.IsPlacementValid())
            {
                if (e is not null)
                    e.Remove();
                e = null;
            }
            else
            {
                break;
            }
        } while (tries-- > 0);

        return e;
    }

    private void CellularAutomaton(int yStart, TileType type, int treshold = 2, int neighborhood = 1)
    {
        int[,] niehgbours = new int[MapWidth, ChunkSize];

        Func<int, int, int> countNeighbors = (int x, int y) =>
        {
            int count = 0;
            for (int i = -neighborhood; i <= neighborhood; i++)
                for (int j = -neighborhood; j <= neighborhood; j++)
                {
                    var square = GetSquare(x + i, y + j);
                    if (square != null && square.Tile.Type == type)
                        count++;
                }

            return count;
        };


        var toChange = new Dictionary<(int, int), Action<GridSquare>>();

        // Transform roots
        for (int i = 0; i < ChunkSize; i++)
            for (int j = 1; j < MapWidth - 1; j++)
            {
                int x = j;
                int y = -yStart - i;

                if (y >= MinTileEntityY)
                    continue;

                if (countNeighbors(x, y) > treshold)
                    toChange.Add((x, y), (GridSquare square) =>
                        ReplaceTile(square.X, square.Y, type));
                else
                    toChange.Add((x, y), (GridSquare square) =>
                    {
                        if (square.Tile.Type == type)
                            ReplaceTile(square.X, square.Y, TileType.Ground);
                    });
            }


        foreach (((int x, int y), Action<GridSquare> change) in toChange)
            change(GetSquare(x, y));
    }


    void GenerateMoreMap()
    {
        // Genrate more map
        int yStart = _tiles.Count;

        // Create Tiles
        for (int i = 0; i < ChunkSize; i++)
        {
            _tiles.Add(new GridSquare[MapWidth]);
            for (int j = 0; j < MapWidth; j++)
            {
                int x = j;
                int y = -yStart - i;

                GridSquare square = AddSquare(x, y);
                square.Tile = RandomTile(x, y);
                square.SetActive(IsTileOnCamera(square.Tile));
            }
        }

        CellularAutomaton(yStart, TileType.EvilGround, treshold: 4);
        CellularAutomaton(yStart, TileType.Root);
        CellularAutomaton(yStart, TileType.Air);

        // Create Entities
        for (int i = 0; i < ChunkSize; i++)
        {
            for (int j = 0; j < MapWidth; j++)
            {
                int x = j;
                int y = -yStart - i;
        
                Entity e = RandomEntity(x, y);
                if (e != null)
                    foreach ((int ex, int ey) in e.GetLocations())
                    {
                        ReplaceTile(ex, ey, TileType.Air);
                        GetSquare(ex, ey).Water.Amount = 0f;
                    }
            }
        }

        for (int i = 0; i < ChunkSize; i++)
            for (int j = 0; j < MapWidth; j++)
            {
                var square = GetSquare(j, -yStart - i);
                var tile = GetTile(j, -yStart - i);
                (tile as RootTile)?.ConnectWithAllNeigh();
                tile.UpdateSprite();
                if (square.Water.Amount > 0.0f && square.PushesOutFluid(square.Water, Direction.Down))
                    square.Water.Amount = 0.0f;
            }

        // fix sprites on the border row
        if (yStart - 1 >= 0)
            foreach (GridSquare square in _tiles[yStart - 1])
            {
                square.Tile?.UpdateSprite();
            }
    }

    public int SimulatedRowsStart => Mathf.Max(0, (int)(-Camera.transform.position.y - Camera.orthographicSize - ExtraSimulatedRows));
    public int SimulatedRowsEnd => (int)(-Camera.transform.position.y + Camera.orthographicSize + ExtraSimulatedRows);

    private bool _initSimDone = false;
    // Update is called once per frame
    void Update()
    {
        while (_tiles.Count < SimulatedRowsEnd)
            GenerateMoreMap();

        if (!_initSimDone)
        {
            SimulationStep();
            _initSimDone = true;
        }

        // TODO don't iterate over all rows probably
        int i = 0;
        foreach (GridSquare[] row in _tiles)
        {
            foreach (GridSquare square in row)
            {
                square.SetActive(IsTileOnCamera(square.Tile));
                foreach (var entity in square.Entities)
                    entity.gameObject.SetActive(i >= SimulatedRowsStart && i < SimulatedRowsEnd);
            }
            i++;
        }

        MoveBackground();
    }

    public void ApplyToSimulatedTiles(Action<GridSquare> action)
    {
        for (int rowId = SimulatedRowsStart; rowId < SimulatedRowsEnd; rowId++)
        {
            var row = _tiles[rowId];
            foreach (var square in row)
            {
                action(square);
            }
        }
    }

    public void SimulationStep(bool passed = false)
    {
        // TODO maybe optimize (hashtable)
        List<Entity> simulatedEntities = new List<Entity>();
        ApplyToSimulatedTiles(square =>
        {
            foreach (var entity in square.Entities)
            {
                if (!simulatedEntities.Contains(entity))
                    simulatedEntities.Add(entity);
            }

            square.SimulationStep();
        });

        simulatedEntities.Reverse(); // it'll look better if falling is simulated bottom to top
        foreach (var entity in simulatedEntities)
            entity.SimulationStep();

        ApplyToSimulatedTiles(square => square.SimulationStepFluid());
        ApplyToSimulatedTiles(square =>
        {
            if (square.Water.Amount > 0.0f && square.PushesOutFluid(square.Water, Direction.Down))
                square.Water.FixDisplacement(square);
        });
        for (int i = 0; i < FluidSubsteps; i++)
        {
            ApplyToSimulatedTiles(square => square.SimulationSubStepFluid());
            ApplyToSimulatedTiles(square => square.SimulationSubStepFluidFinish());
        }
        ApplyToSimulatedTiles(square =>
        {
            var obj = square.Tile.gameObject;
            if (square.Water.Amount + square.Water.MaxAmountSinceLast > 0.0f && obj.GetComponentInChildren<FluidIndicator>() == null)
            {
                var indicatorObj = new GameObject();
                indicatorObj.transform.parent = obj.transform;
                indicatorObj.transform.position = obj.transform.position;
                indicatorObj.AddComponent<SpriteRenderer>().enabled = false;
                var indicatorComponent = indicatorObj.AddComponent<FluidIndicator>();
                indicatorComponent.Square = square;
            }
        });
        // event will be fired at the end (for checks)
        WorldSimulationStepEvent.Invoke(passed);
    }

    public bool IsTileOnCamera(Tile tile)
    {
        return Mathf.Abs(tile.transform.position.x - Camera.transform.position.x) < Camera.orthographicSize * Camera.aspect + 0.5f
               && Mathf.Abs(tile.transform.position.y - Camera.transform.position.y) < Camera.orthographicSize + 0.5f;
    }

    private Dictionary<string, Sprite> LoadSprites()
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (var subdir in new string[] { "Ground", "Root", "Stone", "Characters" })
        {
            var groundSprites = Resources.LoadAll<Sprite>($"Sprites/{subdir}");
            foreach (var sprite in groundSprites)
            {
                sprites.Add(sprite.name, sprite);
            }
        }

        return sprites;
    }
}
