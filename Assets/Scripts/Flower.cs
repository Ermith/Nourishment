using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flower : MonoBehaviour
{
    public List<Sprite> GrowthSprites;
    private SpriteRenderer _spriteRenderer;

    private int _level;
    public int Level
    {
        get { return _level; }
        set
        {
            _level = Mathf.Clamp(value, 0, GrowthSprites.Count - 1);
        }
    }

    [SerializeField]
    private float StartingNourishment = 200;

    private float _nourishment;

    public float Nourishment
    {
        get
        {
            return _nourishment;
        }

        set
        {
            _nourishment = value;
            Level = (int)(_nourishment / 100);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = GrowthSprites[Level];
        Nourishment = StartingNourishment;
    }

    // Update is called once per frame
    void Update()
    {
        _spriteRenderer.sprite = GrowthSprites[Level];
    }

    private void OnGUI()
    {
        if (GUILayout.Button("+ 100"))
            Nourishment += 100;
        if (GUILayout.Button("- 100"))
            Nourishment -= 100;

        GUILayout.Label($"Nourishment: {Nourishment}");
    }
}
