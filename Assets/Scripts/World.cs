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
        return _tiles[-y][x];
    }

    // Start is called before the first frame update
    void Start()
    {
        _offsetX = - (MAP_WIDTH / 2f);
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
                    _tiles[i][j] = TileFactory.RootTile(j, -i);
                else
                    _tiles[i][j] = TileFactory.GroundTile(j, -i);   
            }
        }

        int xStart = 0;
        int yStart = (int)(_offsetY / TILE_SIZE);
        int xCount = MAP_WIDTH;
        int yCount = (int)Mathf.Ceil(_camera.orthographicSize * 2);
        
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
                        y: _camera.orthographicSize -yIndex - 0.5f
                        );

                tile.transform.parent = this.transform;
                tile.gameObject.SetActive(true);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
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
            catch (System.Exception e)
            {
                Debug.Log(e);
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
