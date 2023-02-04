using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class GridSquare
{
    public int X;
    public int Y;
    public Tile Tile;
    public List<Entity> Entities = new List<Entity>();
    public float WaterLevel = 0; // TODO maybe generalize to multiple fluids?

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
}

public class World : MonoBehaviour
{
    public const int TILE_SIZE = 32;
    public const int MAP_WIDTH = 21;
    public const int CHUNK_SIZE = 14;
    public Camera _camera;
    public Dictionary<string, Sprite> Sprites;
    public Player player;
    public int ExtraSimulatedRows = 10;

    private List<GridSquare[]> _tiles = new List<GridSquare[]>();

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

    /// <summary>
    /// groundTreshold (default) < rockTrshold < rootTreshold
    /// </summary>
    /// <param name="x">Tile position</param>
    /// <param name="y">Tile position</param>
    /// <param name="rockTreshold">[0-99] number</param>
    /// <param name="rootTreshold">[0-99] number</param>
    /// <returns></returns>
    private Tile RandomTile(int x, int y, int rockTreshold, int rootTreshold)
    {
        int prob = UnityEngine.Random.Range(0, 100);

        if (prob < rootTreshold)
            return TileFactory.RootTile(this.gameObject, x, y);

        if (prob < rockTreshold)
        {
            var tile = TileFactory.CreateTile(this.gameObject, x, y, TileType.Air);
            EntityFactory.PlaceEntity(this.gameObject, EntityType.SmallRock, x, y);
            return tile;
        }

        return TileFactory.GroundTile(this.gameObject, x, y);
    }

    private void CellularAutomaton(int yStart)
    {
        int[,] niehgbours = new int[MAP_WIDTH, CHUNK_SIZE];
        int neighborhood = 1;

        Func<Func<GridSquare, bool>, int, int, int> countNeighbors = (Func<GridSquare, bool> check, int x, int y) =>
        {
            int count = 0;
            for (int i = -neighborhood; i <= neighborhood; i++)
                for (int j = -neighborhood; j <= neighborhood; j++)
                {
                    var square = GetSquare(x + i, y + j);
                    if (square != null && check(square))
                        count++;
                }

            return count;
        };


        var toChange = new Dictionary<(int, int), Action<GridSquare>>();

        // Transform roots
        int rootTreshold = 2;
        Func<GridSquare, bool> isRoot = (GridSquare square) => square.Tile is RootTile;
        for (int i = 0; i < CHUNK_SIZE; i++)
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                if (countNeighbors(isRoot, j, -yStart - i) > rootTreshold)
                    toChange.Add((j, -yStart - i), (GridSquare square) =>
                        ReplaceTile(square.X, square.Y, TileType.Root));
                else
                    toChange.Add((j, -yStart - i), (GridSquare square) =>
                    {
                        if (square.Tile is RootTile)
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
        int rockTreshold = 10;
        int rootTreshold = 10 - yStart / CHUNK_SIZE * 4;

        for (int i = 0; i < CHUNK_SIZE; i++)
        {
            _tiles.Add(new GridSquare[MAP_WIDTH]);
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                int x = j;
                int y = -yStart - i;

                GridSquare square = AddSquare(x, y);
                square.Tile = RandomTile(x, y, rockTreshold, rootTreshold);
                square.Tile.gameObject.SetActive(IsTileOnCamera(square.Tile));
            }
        }

        CellularAutomaton(yStart);

        // Player starts on root
        int xSpawn = MAP_WIDTH / 2;
        int ySpawn = 0;
        RootTile spawnTile = ReplaceTile(xSpawn, ySpawn, TileType.Root) as RootTile;
        spawnTile.ForceConnect(Direction.Up);
        spawnTile.Protected = true;

        for (int i = 0; i < 3; i++)
        {
            int tries = 10;
            int x, y;
            Entity rock;
            do
            {
                x = UnityEngine.Random.Range(0, MAP_WIDTH - 1);
                y = -UnityEngine.Random.Range(yStart + 1, _tiles.Count);
                rock = EntityFactory.PlaceEntity(this.gameObject, EntityType.SquareRock, x, y);
                if(rock.IsPlacementValid())
                    break;
                Destroy(rock.gameObject);
                rock = null;
            } while (tries-- > 0);
            
            if(rock)
                foreach (var location in rock.GetLocations())
                {
                    ReplaceTile(location.Item1, location.Item2, TileType.Air);
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
        foreach (GridSquare[] row in _tiles)
            foreach (GridSquare square in row)
            {
                var tile = square.Tile;
                tile.gameObject.SetActive(IsTileOnCamera(tile));
            }
    }

    public void SimulationStep()
    {
        // TODO maybe optimize (hashtable)
        List<Entity> simulatedEntities = new List<Entity>();
        for (int rowId = SimulatedRowsStart; rowId < SimulatedRowsEnd; rowId++)
        {
            var row = _tiles[rowId];
            foreach (var square in row)
            {
                foreach (var entity in square.Entities)
                {
                    if(!simulatedEntities.Contains(entity))
                        simulatedEntities.Add(entity);
                }
                square.SimulationStep();
            }
        }

        simulatedEntities.Reverse(); // it'll look better if falling is simulated bottom to top
        foreach (var entity in simulatedEntities)
            entity.SimulationStep();
    }

    public bool IsTileOnCamera(Tile tile)
    {
        return Mathf.Abs(tile.transform.position.x - _camera.transform.position.x) < _camera.orthographicSize * _camera.aspect + 0.5f
               && Mathf.Abs(tile.transform.position.y - _camera.transform.position.y) < _camera.orthographicSize + 0.5f;
    }

    private Dictionary<string, Sprite> LoadSprites()
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (var subdir in new string[] { "Ground", "Root", "Stone" })
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
