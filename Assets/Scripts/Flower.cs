using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flower : MonoBehaviour
{
    public GameObject VictoryScreenPrompt;
    public TMP_Text VictoryProgress;
    public List<Sprite> GrowthSprites;
    public int RequiredLevelAmberBreak = 6;
    public int QueenLevel = 0;
    public float AmberBreakCost = 250;
    public bool HasHatchedBee = false;
    public GameObject HatchedBeeQueen = null;
    public const int GAME_OVER_NOURISHMENT = 5;
    public TMP_Text NourishmentText;
    public TMP_Text HintsText;
    public TMP_Text HintForHintsText;
    public float BeeBonus = 0.5f;
    private bool NourishmentChanged = false;
    private float NourishmentOld = 0;

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
    private float StartingNourishment = 400;

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
            Level = NourishmentToLevel(_nourishment);
            if (_nourishment < GAME_OVER_NOURISHMENT)
                SceneManager.LoadScene("GameOverScene");

            NourishmentText.text = $"Nourishment: {String.Format("{0:.##}", _nourishment)}";
        }
    }

    private int NourishmentToLevel(float expectedNourishment)
    {
        int currentLevel = -1;
        foreach (float nextLevel in NourishmentForLevel)
        {
            if (nextLevel <= expectedNourishment)
                currentLevel++;
            else
                break;
        }
        // bees to level
        if (currentLevel < QueenLevel)
        {
            currentLevel = QueenLevel;
        }
        if (NourishmentForLevel.Length <= QueenLevel && Nourishment < NourishmentForLevel[NourishmentForLevel.Length])
        {
            // final can be achieved only if victory condition is met
            currentLevel -= 1;
        }

        return currentLevel;
    }

    // Start is called before the first frame update
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = GrowthSprites[Level];
        Nourishment = StartingNourishment;
        UpdateProgress();
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
        if (Util.GetWorld().CheatsEnabled)
        {
            if (GUILayout.Button("+ 100"))
                Nourishment += 100;
            if (GUILayout.Button("- 100"))
                Nourishment -= 100;

            GUILayout.Label($"Nourishment: {Nourishment}");
        }
    }

    public void ObtainBeeQueen()
    {
        HasHatchedBee = true;
        PowerUpQueen();
        if (HatchedBeeQueen != null)
            HatchedBeeQueen.SetActive(true);
    }

    public bool IsAbleToBreakAmber()
    {
        // current level is higher or equal
        // changed level is still higher or equal to break level
        return Level >= RequiredLevelAmberBreak &&
               NourishmentToLevel(_nourishment - AmberBreakCost) >= RequiredLevelAmberBreak;
    }
    public void BreakAmber()
    {
        Util.GetFlower().Nourishment -= AmberBreakCost;
    }

    public bool CanObtainQueen()
    {
        return !HasHatchedBee;
    }

    public void PowerUpQueen()
    {
        QueenLevel += 1;
        UpdateProgress();
        CheckVictory();
    }

    private void UpdateProgress()
    {
        if (QueenLevel < NourishmentForLevel.Length)
        {
            VictoryProgress.text = $"Bees saved: {QueenLevel} / {NourishmentForLevel.Length}  ";
        }
        else
        {
            VictoryProgress.text = $"Bees saved: {QueenLevel}  ";
        }
    }

    public void CheckVictory()
    {
        if (QueenLevel == NourishmentForLevel.Length && Nourishment >= NourishmentForLevel[NourishmentForLevel.Length])
        {
            VictoryScreenPrompt.SetActive(true);
        }
        else
        {
            return;
        }
    }

    public void HideVictory()
    {
        VictoryScreenPrompt.SetActive(false);
    }

    public void OnWorldSimulationStep(bool passed)
    {
        NourishmentChanged = MathF.Abs(NourishmentOld - Nourishment) > 0;
        NourishmentOld = Nourishment;
        if (passed)
            return;

        if (HasHatchedBee && QueenLevel > NourishmentForLevel.Length && NourishmentChanged)
        {
            Nourishment += BeeBonus * (QueenLevel - NourishmentForLevel.Length);
        }
    }
}
