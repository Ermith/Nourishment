using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public const int TILE_SIZE = 32;
    public const int MAP_WIDTH = 21;
    public Camera _camera;
    public Dictionary<string, Sprite> Sprites;

    private List<Tile[]> _tiles;
    private float _offsetX; // based on camera witdth
    private float _offsetY; // camera position

    public Tile GetTile(int x, int y)
    {
        // TODO if y below current row count generate more rows
        if (x < 0 || x >= MAP_WIDTH || y > 0 || y <= -_tiles.Count)
            return null;
        return _tiles[-y][x];
    }

    // Start is called before the first frame update
    void Start()
    {
        _offsetX = -(MAP_WIDTH / 2f);
        _offsetY = 0;

        Sprites = LoadSprites();

        // Generation of world
        _tiles = new List<Tile[]>();
        for (int i = 0; i < 30; i++)
        {
            _tiles.Add(new Tile[MAP_WIDTH]);
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                if (Random.Range(0, 100) < 50)
                    _tiles[i][j] = TileFactory.RootTile(this.gameObject, j, -i);
                else
                    _tiles[i][j] = TileFactory.GroundTile(this.gameObject, j, -i);
            }
        }

        int xStart = 0;
        int yStart = (int)(_offsetY / TILE_SIZE);
        int xCount = MAP_WIDTH;
        int yCount = _tiles.Count;

        for (int y = 0; y < yCount; y++)
        {
            for (int x = 0; x < xCount; x++)
            {
                int xIndex = xStart + x;
                int yIndex = yStart + y;

                var tile = _tiles[yIndex][xIndex];
                tile.UpdateSprite();
                tile.transform.position =
                    new Vector2(
                        x: _offsetX + xIndex,
                        y: _camera.orthographicSize - yIndex - 0.5f
                        );

                tile.gameObject.SetActive(true);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Tile[] row in _tiles)
            foreach (Tile tile in row)
            {
                tile.gameObject.SetActive(
                    Mathf.Abs(tile.transform.position.x - _camera.transform.position.x) < _camera.orthographicSize * _camera.aspect + TILE_SIZE / 2f
                    && Mathf.Abs(tile.transform.position.y - _camera.transform.position.y) < _camera.orthographicSize + 0.5f
                    );
            }


        if (Random.Range(0, 100) >= 0)
        {
            try
            {
                var randomTile = _tiles[Random.Range(0, _tiles.Count)][Random.Range(0, MAP_WIDTH)];
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
