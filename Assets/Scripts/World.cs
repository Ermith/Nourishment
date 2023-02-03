using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Camera _camera;
    private List<Tile> _tiles;
    private float _offsetX; // based on camera witdth
    private float _offsetY; // camera position

    // Start is called before the first frame update
    void Start()
    {
        _offsetX = _camera.orthographicSize - Mathf.Floor(_camera.orthographicSize);
        _offsetY = 0;

        var sprites = LoadSprites();

        var groundTile = new GroundTile(sprites);
        var groundObject = groundTile.UpdateSprite(_tiles);
        groundObject.transform.parent = this.transform;
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
