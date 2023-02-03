using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public int X;
    public int Y;
    List<Sprite> Sprites;

    GameObject Create()
    {
        if (Sprites.Count == 1)
        {
            GameObject tile = new GameObject();
            var spriteRenderer = tile.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Sprites[0];
            return GameObject.Instantiate(tile);
        }

        // just for now
        return null;
    }
}

public class World : MonoBehaviour
{
    private Camera _camera;
    private List<Tile> _tiles;
    private float _offsetX;
    private float _offsetY;

    // Start is called before the first frame update
    void Start()
    {
        _offsetX = _camera.orthographicSize - Mathf.Floor(_camera.orthographicSize);
        _offsetY = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
