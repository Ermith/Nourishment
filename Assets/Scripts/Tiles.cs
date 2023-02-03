using UnityEngine;
using System.Collections.Generic;

public class TileComponent : MonoBehaviour
{
    public Tile Tile;
}

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
        if (cornerType.Length != 0) corner += "-";

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

    public abstract GameObject UpdateSprite(World world);
}

public class GroundTile : Tile
{
    public GroundTile(Dictionary<string, Sprite> sprites) : base(sprites)
    {
    }

    public override GameObject UpdateSprite(World world)
    {
        int left = X - 1;
        int top = Y - 1;
        int right = X + 1;
        int bottom = Y + 1;

        // TODO: CHECK NEIGHBORS

        return CreateSpriteObject("ground");
    }
}

