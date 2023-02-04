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
        if(Tile != null && !Tile.CanSpread(player, spreadDirection))
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
        foreach (var entity in Entities)
        {
            entity.SimulationStep();
        }
    }

    public void OnSpread(Player player, Direction spreadDirection)
    {
        foreach (var entity in Entities)
        {
            entity.OnSpread(player, spreadDirection);
        }
    }

    public bool CanPass(Entity entity, Direction moveDirection)
    {
        if (Tile != null && !Tile.CanPass(entity, moveDirection))
            return false;
        foreach (var e in Entities)
        {
            if (e != entity && !e.CanPass(entity, moveDirection))
                return false;
        }

        return true;
    }

    public void OnPass(Entity entity, Direction moveDirection)
    {
        foreach (var e in Entities)
        {
            if(e != entity)
                e.OnPass(entity, moveDirection);
        }
    }
}

public class World : MonoBehaviour
{
    public const int TILE_SIZE = 32;
    public const int MAP_WIDTH = 21;
    public Camera _camera;
    public Dictionary<string, Sprite> Sprites;
    public Player player;
    public int ExtraSimulatedRows = 10;

    private List<GridSquare[]> _tiles = new List<GridSquare[]>();
    private float _offsetX; // based on camera witdth

    public static bool InBounds((int, int) coords)
    {
        return coords.Item1 >= 0 && coords.Item1 < MAP_WIDTH && coords.Item2 <= 0;
    }

    public GridSquare GetSquare(int x, int y)
    {
        if (!InBounds((x, y)))
            return null;
        while (-y >= _tiles.Count && -y < SimulatedRowsEnd)
            GenerateMoreMap();
        if (-y >= _tiles.Count)
            return null;
        return _tiles[-y][x];
    }

    public Tile GetTile(int x, int y)
    {
        return GetSquare(x, y)?.Tile;
    }

    // Start is called before the first frame update
    void Start()
    {
        _offsetX = -(MAP_WIDTH / 2f);

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
        newTile.transform.position =
            new Vector2(
                x: _offsetX + x,
                y: y - 0.5f
            );
        square.Tile = newTile;
        return newTile;
    }

    void GenerateMoreMap(int rowsToGenerate = 10)
    {
        // rowsToGenerate might need to be fixed in the end idk

        int yStart = _tiles.Count;
        for (int i = 0; i < rowsToGenerate; i++)
        {
            _tiles.Add(new GridSquare[MAP_WIDTH]);
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                int x = j;
                int y = -yStart - i;
                _tiles[yStart + i][j] = new GridSquare(x, y);
                if (x == player.X && y == player.Y)
                {
                    RootTile rootTile = (RootTile)TileFactory.RootTile(this.gameObject, x, y);
                    rootTile.ForceConnect(Direction.Up);
                    _tiles[yStart + i][j].Tile = rootTile;
                }
                else if (Random.Range(0, 100) < 5)
                    _tiles[yStart + i][j].Tile = TileFactory.RootTile(this.gameObject, x, y);
                else
                    _tiles[yStart + i][j].Tile = TileFactory.GroundTile(this.gameObject, x, y);
            }
        }

        int xStart = 0;
        // int yStart = (int)(_offsetY / TILE_SIZE);
        int xCount = MAP_WIDTH;
        int yCount = _tiles.Count - yStart;

        for (int y = 0; y < yCount; y++)
        {
            for (int x = 0; x < xCount; x++)
            {
                int xIndex = xStart + x;
                int yIndex = yStart + y;

                var tile = _tiles[yIndex][xIndex].Tile;
                tile.UpdateSprite();
                tile.transform.position =
                    new Vector2(
                        x: _offsetX + xIndex,
                        y: -yIndex - 0.5f
                    );

                // TODO check if in camera bounds before setting active
                tile.gameObject.SetActive(true);
            }
        }

        // fix sprites on the border row
        if(yStart - 1 >= 0)
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
                tile.gameObject.SetActive(
                    Mathf.Abs(tile.transform.position.x - _camera.transform.position.x) < _camera.orthographicSize * _camera.aspect + TILE_SIZE / 2f
                    && Mathf.Abs(tile.transform.position.y - _camera.transform.position.y) < _camera.orthographicSize + 0.5f
                    );
            }
    }

    public void SimulationStep()
    {
        for (int rowId = SimulatedRowsStart; rowId < SimulatedRowsEnd; rowId++)
        {
            var row = _tiles[rowId];
            foreach (var square in row)
            {
                square.SimulationStep();
            }
        }
    }

    private Dictionary<string, Sprite> LoadSprites()
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (var subdir in new string[] { "Ground", "Root" })
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
