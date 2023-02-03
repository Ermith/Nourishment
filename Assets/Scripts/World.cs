using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    private const int TILE_SIZE = 32;
    public Camera _camera;
    private List<Tile> _tiles;
    private float _offsetX; // based on camera witdth
    private float _offsetY; // camera position

    // Start is called before the first frame update
    void Start()
    {
        _offsetX =  Mathf.Floor(_camera.orthographicSize * _camera.aspect) - _camera.orthographicSize * _camera.aspect;
        _offsetY = 0;

        var sprites = LoadSprites();

        // Generation of world
        _tiles = new List<Tile>();
        for (int i = 0; i < 500; i++) _tiles.Add(new GroundTile(sprites));

        //var groundTile = new GroundTile(sprites);
        //var groundObject = groundTile.UpdateSprite(_tiles);
        //groundObject.transform.parent = this.transform;

        int xStart = 0;
        int yStart = (int)(_offsetY / TILE_SIZE);
        int xCount = (int)Mathf.Ceil(_camera.aspect * _camera.orthographicSize * 2) + 1;
        int yCount = (int)Mathf.Ceil(_camera.orthographicSize * 2);

        for (int y = 0; y < yCount; y++)
        {
            for (int x = 0; x < xCount; x++)
            {
                int xIndex = xStart + x;
                int yIndex = yStart + y;
                int index = yIndex * yCount + xIndex;

                if (index > _tiles.Count) return;

                var tile = _tiles[index];
                var spriteObject = tile.UpdateSprite(_tiles);
                spriteObject.transform.position =
                    new Vector2(
                        x: _offsetX + xIndex - _camera.orthographicSize * _camera.aspect + 1,
                        y: _offsetY + yIndex - _camera.orthographicSize
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
