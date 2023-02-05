using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flower : MonoBehaviour
{
    public List<Sprite> GrowthSprites;
    public int RequiredLevelAmberBreak = 6;
    public bool HasHatchedBee = false;
    public GameObject HatchedBeeQueen = null;
    public const int GAME_OVER_NOURISHMENT = 5;
    public TMP_Text NourishmentText;
    public TMP_Text HintsText;
    public TMP_Text HintForHintsText;
     

    private SpriteRenderer _spriteRenderer;

    public float[] NourishmentForLevel = new float[]
    {

    };

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
            Level = NourishmentToLevel();
            if (_nourishment < GAME_OVER_NOURISHMENT)
                SceneManager.LoadScene("GameOverScene");

            NourishmentText.text = $"Nourishment: {_nourishment}";
        }
    }

    private int NourishmentToLevel()
    {
        int currentLevel = 0;
        foreach (float nextLevel in NourishmentForLevel)
        {
            if (nextLevel <= _nourishment)
                currentLevel++;
            else
                break;
        }

        return currentLevel;
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

        if (Input.GetKeyDown(KeyCode.F1))
        {
            HintForHintsText.enabled = !HintForHintsText.enabled;
            HintsText.enabled = !HintsText.enabled;
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("+ 100"))
            Nourishment += 100;
        if (GUILayout.Button("- 100"))
            Nourishment -= 100;

        GUILayout.Label($"Nourishment: {Nourishment}");
    }

    public void ObtainedBeeQueen()
    {
        HasHatchedBee = true;
        if (HatchedBeeQueen != null)
            HatchedBeeQueen.SetActive(true);
    }
}
