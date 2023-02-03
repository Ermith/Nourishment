using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tile
{
    public int X;
    public int Y;
    protected Dictionary<string, Sprite> _sprites;

    protected Tile(Dictionary<string, Sprite> sprites)
    {
        _sprites = sprites;
    }

    private GameObject CreateCorner(string type, string corner, string cornerType)
    {
        if (cornerType.Length == 0) cornerType += "-";

        GameObject spriteObject = new GameObject();

        string spriteName = $"{type}{corner}{cornerType}";
        spriteObject.AddComponent<SpriteRenderer>().sprite = _sprites[spriteName];
        spriteObject.name = spriteName;

        return spriteObject;
    }

    protected GameObject CreateSpriteObject(
        string type,
        string _00 = "",
        string _01 = "",
        string _10 = "",
        string _11 = "")
    {
        GameObject sprite00 = CreateCorner(type, "00", _00);
        GameObject sprite01 = CreateCorner(type, "01", _01);
        GameObject sprite10 = CreateCorner(type, "10", _10);
        GameObject sprite11 = CreateCorner(type, "11", _11);

        sprite01.transform.position += Vector3.up * 0.5f;
        sprite10.transform.position += Vector3.right * 0.5f;
        sprite11.transform.position += Vector3.right * 0.5f + Vector3.up * 0.5f;

        GameObject fatherObject = new GameObject();

        sprite00.transform.parent = fatherObject.transform;
        sprite01.transform.parent = fatherObject.transform;
        sprite10.transform.parent = fatherObject.transform;
        sprite11.transform.parent = fatherObject.transform;

        return fatherObject;
    }

    public abstract GameObject UpdateSprite(List<Tile> tiles);
}

public class GroundTile : Tile
{
    public GroundTile(Dictionary<string, Sprite> sprites) : base(sprites)
    {
    }

    public override GameObject UpdateSprite(List<Tile> tiles)
    {
        int left = X - 1;
        int top = Y - 1;
        int right = X + 1;
        int bottom = Y + 1;

        // TODO: CHECK NEIGHBORS

        return CreateSpriteObject("ground", _00: "-corner");
    }
}

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
