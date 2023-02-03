using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public const int TILE_SIZE = 32;
    public const int MAP_WIDTH = 21;
    public Camera _camera;

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

        var sprites = LoadSprites();

        // Generation of world
        _tiles = new List<Tile[]>();
        for (int i = 0; i < 30; i++)
        {
            _tiles.Add(new Tile[TILE_SIZE]);
            for (int j = 0; j < MAP_WIDTH; j++)
            {
                _tiles[i][j] = new GroundTile(sprites);
            }
        }

        //var groundTile = new GroundTile(sprites);
        //var groundObject = groundTile.UpdateSprite(_tiles);
        //groundObject.transform.parent = this.transform;

        int xStart = 0;
        int yStart = (int)(_offsetY / TILE_SIZE);
        int xCount = MAP_WIDTH;
        int yCount = (int)Mathf.Ceil(_camera.orthographicSize * 2);
        //float cameraHeight = _camera.orthographicSize;

        for (int y = 0; y < yCount; y++)
        {
            for (int x = 0; x < xCount; x++)
            {
                int xIndex = xStart + x;
                int yIndex = yStart + y;

                var tile = _tiles[yIndex][xIndex];
                var spriteObject = tile.UpdateSprite(this);
                spriteObject.transform.position =
                    new Vector2(
                        x: _offsetX + xIndex,
                        y: _camera.orthographicSize -yIndex - 0.5f
                        );

                spriteObject.transform.parent = this.transform;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private Dictionary<string, Sprite> LoadSprites()
    {
        var sprites = new Dictionary<string, Sprite>();
        var groundSprites = Resources.LoadAll<Sprite>("Sprites/Ground");
        foreach (var sprite in groundSprites)
        {
            sprites.Add(sprite.name, sprite);
        }

        return sprites;
    }
}
