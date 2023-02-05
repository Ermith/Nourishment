using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;

public class GridSquare
{
    public int X;
    public int Y;
    public Tile Tile;
    public List<Entity> Entities = new List<Entity>();
    public Water Water = new Water();
    public bool active = true;
    
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
        if (active == this.active)
            return;
        this.active = active;
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
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            if (Util.GetWorld().GetTile(X + dir.X(), Y + dir.Y()) is RootTile)
                absorbAmount += World.WATER_CONVERSION_SPEED;
        }

        absorbAmount = Mathf.Min(Water.Amount, absorbAmount);
        Water.Amount -= absorbAmount;
        Util.GetFlower().Nourishment += absorbAmount * World.WATER_CONVERSION_RATIO;
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
}

public class World : MonoBehaviour
{
    public static int MIN_TILE_ENTITY_Y = -3;
    public static int MIN_ENTITY_Y = -5;
    public const int TILE_SIZE = 32;
    public static int MAP_WIDTH = 23;
    public const int CHUNK_SIZE = 14;
    public const int FLUID_SUBSTEPS = 5;
    public const float WATER_CONVERSION_RATIO = 15f; //! how much nourishment you get per 1 tile of water
    public const float WATER_CONVERSION_SPEED = 0.03f; //! how much water do you absorb per 1 tick per water/root boundary
    public Camera _camera;
    public Dictionary<string, Sprite> Sprites;
    public Player player;
    public int ExtraSimulatedRows = 10;

    public SpriteRenderer Background1;
    public SpriteRenderer Background2;
    public SpriteRenderer Background3;
    public SpriteRenderer Background4;
    private float start1;
    private float start2;
    private float start3;
    private float start4;
    //private SpriteRenderer CurrentBackground;


    private List<GridSquare[]> _tiles = new List<GridSquare[]>();

    private Vector3 ComputeParallax(ref float start)
    {
        float length = Background1.size.y / 2f;
        float parallax = 0.5f;

        float temp = _camera.transform.position.y * (1 - parallax);
        float dist = _camera.transform.position.y * parallax;

        float oldStart = start;

        if (temp > start1 + length)
            start += length;

        if (temp < start1 - length)
            start -= length;

        return Vector3.up * (oldStart + dist) + Vector3.forward * 100;
    }

    public void MoveBackground()
    {
        Background1.transform.position = ComputeParallax(ref start1);
        Background2.transform.position = ComputeParallax(ref start2);
        Background3.transform.position = ComputeParallax(ref start3);
        Background4.transform.position = ComputeParallax(ref start4);
    }

    public static bool InBounds((int, int) coords)
    {
        return coords.Item1 >= 0 && coords.Item1 < MAP_WIDTH && coords.Item2 <= 0;
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
        Sprites = LoadSprites();

        // Generation of world
        GenerateMoreMap();
        start1 = Background1.transform.position.y;
        start2 = Background2.transform.position.y;
        start3 = Background3.transform.position.y;
        start4 = Background3.transform.position.y;
    }

    public Tile ReplaceTile(int x, int y, TileType type)
    {
        var square = GetSquare(x, y);

        var oldTile = square.Tile;

        if (oldTile != null)
            Destroy(oldTile.gameObject);

        var newTile = TileFactory.CreateTile(gameObject, x, y, type);
        newTile.UpdateSprite();
        square.Tile = newTile;

        return newTile;
    }

    private Tile RandomTile(int x, int y)
    {
        // First row
        if (y == 0 && x == MAP_WIDTH / 2)
            return TileFactory.CreateTile(gameObject, x, y, TileType.Air);

        if (y == 0)
            return TileFactory.CreateTile(gameObject, x, y, TileType.Grass);

        if (y == -1 && x == MAP_WIDTH / 2)
        {
            RootTile tile = TileFactory.CreateTile(gameObject, x, y, TileType.Root) as RootTile;
            tile.ForceConnect(Direction.Up);
            tile.Protected = true;
            return tile;
        }

        if (x == 0 || x == MAP_WIDTH - 1)
            return TileFactory.CreateTile(gameObject, x, y, TileType.SuperGround);

        int prob = UnityEngine.Random.Range(0, 100);

        int air = 30;
        int root = 15;

        if (y < MIN_TILE_ENTITY_Y)
        {
            if (prob < root)
                return TileFactory.CreateTile(gameObject, x, y, TileType.Root);

            if (prob < air)
                return TileFactory.CreateTile(gameObject, x, y, TileType.Air);
        }

        return TileFactory.CreateTile(gameObject, x, y, TileType.Ground);
    }

    private Entity RandomEntity(int x, int y)
    {
        if (y >= MIN_ENTITY_Y)
            return null;

        int amberBee = 2;
        int squareRock = 4;
        int smallRock = 7;
        int snail = 17;
        int slug = 35;

        int prob = UnityEngine.Random.Range(0, 100);
        Tile tile = GetTile(x, y);
        EntityType? type = null;

        if (prob < snail && tile is AirTile)
            type = EntityType.Snail;

        if (prob < slug && tile is AirTile)
            type = EntityType.Slug;

        if (prob < amberBee)
            type = EntityType.AmberBee;

        if (prob < squareRock)
            type = EntityType.SquareRock;

        if (prob < smallRock)
            type = EntityType.SmallRock;

        if (type == null)
            return null;
        
        Entity e = Util.GetEntityFactory().PlaceEntity(gameObject, type.Value, x, y);
        if (e is null || !e.IsPlacementValid())
        {
            Destroy(e);
            return null;
        }

        return e;
    }

    private void CellularAutomaton(int yStart, TileType type, int treshold = 2, int neighborhood = 1)
    {
        int[,] niehgbours = new int[MAP_WIDTH, CHUNK_SIZE];

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
        for (int i = 0; i < CHUNK_SIZE; i++)
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                int x = j;
                int y = -yStart - i;

                if (y >= MIN_TILE_ENTITY_Y)
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
        for (int i = 0; i < CHUNK_SIZE; i++)
        {
            _tiles.Add(new GridSquare[MAP_WIDTH]);
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                int x = j;
                int y = -yStart - i;

                GridSquare square = AddSquare(x, y);
                square.Tile = RandomTile(x, y);
                square.SetActive(IsTileOnCamera(square.Tile));
            }
        }
        
        CellularAutomaton(yStart, TileType.Root);
        CellularAutomaton(yStart, TileType.Air);


        // Create Entities
        for (int i = 0; i < CHUNK_SIZE; i++)
        {
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                int x = j;
                int y = -yStart - i;
        
                Entity e = RandomEntity(x, y);
                if (e != null)
                    foreach ((int ex, int ey) in e.GetLocations())
                        ReplaceTile(ex, ey, TileType.Air);
            }
        }

        for (int i = 0; i < CHUNK_SIZE; i++)
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                var tile = GetTile(j, -yStart - i);
                (tile as RootTile)?.ConnectWithAllNeigh();
                tile.UpdateSprite();
            }

        // fix sprites on the border row
        if (yStart - 1 >= 0)
            foreach (GridSquare square in _tiles[yStart - 1])
            {
                square.Tile?.UpdateSprite();
            }
    }

    public int SimulatedRowsStart => Mathf.Max(0, (int)(-_camera.transform.position.y - _camera.orthographicSize - ExtraSimulatedRows));
    public int SimulatedRowsEnd => (int)(-_camera.transform.position.y + _camera.orthographicSize + ExtraSimulatedRows);

    // Update is called once per frame
    void Update()
    {
        while (_tiles.Count < SimulatedRowsEnd)
            GenerateMoreMap();

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

    public void SimulationStep()
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
            if (square.Water.Amount > 0.0f && !square.CanFluidPass(square.Water, Direction.Down))
                square.Water.FixDisplacement(square);
        });
        for (int i = 0; i < FLUID_SUBSTEPS; i++)
        {
            ApplyToSimulatedTiles(square => square.SimulationSubStepFluid());
            ApplyToSimulatedTiles(square => square.SimulationSubStepFluidFinish());
        }
        ApplyToSimulatedTiles(square =>
        {
            var obj = square.Tile.gameObject;
            if (square.Water.Amount > 0.0f && obj.GetComponentInChildren<FluidIndicator>() == null)
            {
                var indicatorObj = new GameObject();
                indicatorObj.transform.parent = obj.transform;
                indicatorObj.transform.position = obj.transform.position;
                indicatorObj.AddComponent<SpriteRenderer>();
                var indicatorComponent = indicatorObj.AddComponent<FluidIndicator>();
                indicatorComponent.Square = square;
            }
        });
    }

    public bool IsTileOnCamera(Tile tile)
    {
        return Mathf.Abs(tile.transform.position.x - _camera.transform.position.x) < _camera.orthographicSize * _camera.aspect + 0.5f
               && Mathf.Abs(tile.transform.position.y - _camera.transform.position.y) < _camera.orthographicSize + 0.5f;
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
