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

    public bool CanSpread(Player player)
    {
        if(Tile != null && !Tile.CanSpread(player))
            return false;
        
        foreach (var entity in Entities)
        {
            if (!entity.CanSpread(player))
                return false;
        }

        return true;
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

    void GenerateMoreMap(int rowsToGenerate = 10)
    {
        // rowsToGenerate might need to be fixed in the end idk

        int yStart = _tiles.Count;
        for (int i = 0; i < rowsToGenerate; i++)
        {
            _tiles.Add(new GridSquare[MAP_WIDTH]);
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                _tiles[yStart + i][j] = new GridSquare(j, -yStart - i);
                if (Random.Range(0, 100) < 50)
                    _tiles[yStart + i][j].Tile = TileFactory.RootTile(this.gameObject, j, -yStart - i);
                else
                    _tiles[yStart + i][j].Tile = TileFactory.GroundTile(this.gameObject, j, -yStart - i);
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


        if (Random.Range(0, 100) >= 0)
        {
            try
            {
                var randomTile = _tiles[Random.Range(0, _tiles.Count)][Random.Range(0, MAP_WIDTH)].Tile;
                if (randomTile is RootTile rootTile)
                {
                    rootTile.ConnectWithNeigh((Direction)Random.Range(0, 4));
                }
            }
            catch (RootNotFoundException)
            {
            }
        }
    }

    public void SimulationStep()
    {

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
