using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private bool VictoryAchieved = false;
    private SpriteRenderer _spriteRenderer;

    public float[] NourishmentForLevel = new float[] { };

    private int _level;
    public int Level
    {
        get { return _level; }
        set
        {
            // this checks only sprites
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
            Level = ApplyLevelRestriction(NourishmentToLevel(_nourishment));
            if (_nourishment < GAME_OVER_NOURISHMENT)
                SceneManager.LoadScene("GameOverScene");

            CheckVictory();

            NourishmentText.text = $"Nourishment: {String.Format("{0:.##}", _nourishment)}";
        }
    }

    private int ApplyLevelRestriction(int desiredLevel)
    {
        // Bees defines minimum level as well
        var newLevel = Math.Max(desiredLevel, QueenLevel);
        // Check if new level is expected to be max
        bool isMaxLevel = newLevel >= NourishmentForLevel.Length - 1;
        // max level requires bee condition and nourishment condition
        bool isComplete = HasCompletedBeeCondition && HasCompletedNourishmentCondition;
        if (isMaxLevel && !isComplete)
        {
            // get level that is not last as current level
            newLevel = NourishmentForLevel.Length - 2;
        }
        // Otherwise tree can be shown as max level tree
        return newLevel;
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
    // Bee level will be changed
    public void PowerUpQueen()
    {
        QueenLevel += 1;
        UpdateProgress();
    }
    private bool HasCompletedNourishmentCondition => _nourishment >= NourishmentForLevel.Last();
    private bool HasCompletedBeeCondition => QueenLevel >= NourishmentForLevel.Length - 1;
    private void UpdateProgress()
    {
        if (!VictoryAchieved)
        {
            VictoryProgress.text = $"Bees saved: {QueenLevel} / {NourishmentForLevel.Length - 1}  ";
        }
        else
        {
            VictoryProgress.text = $"Bees saved: {QueenLevel}  ";
        }
    }

    public void CheckVictory()
    {
        if (!VictoryAchieved && HasCompletedBeeCondition && HasCompletedNourishmentCondition)
        {
            VictoryAchieved = true;
            VictoryScreenPrompt.SetActive(true);
            UpdateProgress();
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

        if (HasCompletedBeeCondition && NourishmentChanged)
        {
            Nourishment += BeeBonus * (QueenLevel - NourishmentForLevel.Length - 1);
        }
    }
}
