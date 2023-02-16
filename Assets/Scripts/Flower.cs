using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Flower : MonoBehaviour
{
    public GameObject VictoryScreenPrompt;
    public TMP_Text VictoryProgress;
    public List<Sprite> GrowthSprites;
    public int RequiredLevelAmberBreak = 6;
    public int QueenLevel = 0;
    public float AmberBreakCost = 250;
    public bool HasHatchedBee = false;
    public GameObject BeeQueenPrefab = null;
    public const int GameOverNourishment = 5;
    public TMP_Text NourishmentText;
    public TMP_Text HintsText;
    public TMP_Text HintForHintsText;
    public float BeeBonus = 0.5f;
    private bool _nourishmentDecreased = false;
    private float _nourishmentOld = 0;
    private bool _victoryAchieved = false;
    private SpriteRenderer _spriteRenderer;
    public GameObject NourishmentGainIndicator;
    private GameObject _canvas;
    private int _maxLevelReached = 0;

    public float[] NourishmentForLevel = new float[] { };

    private int _level;
    public int Level
    {
        get { return _level; }
        set
        {
            // this checks only sprites
            _level = Mathf.Clamp(value, 0, GrowthSprites.Count - 1);
            _maxLevelReached = Mathf.Max(_maxLevelReached, _level);
        }
    }

    [SerializeField]
    private float _startingNourishment = 400;

    private bool _indicatedChange = true; // should skip the initial set to 400
    private float _nourishment;

    public float Nourishment
    {
        get => _nourishment;
        set
        {
            var delta = value - _nourishment;
            if (delta == 0)
            {
                _indicatedChange = false;
                return;
            }

            _nourishment = value;
            Level = ApplyLevelRestriction(NourishmentToLevel(_nourishment));
            if (_nourishment < GameOverNourishment)
                SceneManager.LoadScene("GameOverScene");

            CheckVictory();

            if (!_indicatedChange)
                IndicateNourishmentChange(delta);

            NourishmentText.text = $"Nourishment: {_nourishment:.##}";

            _indicatedChange = false;
        }
    }

    public void ModifyNourishmentWithSource(float delta, GameObject source)
    {
        IndicateNourishmentChange(delta, source);
        _indicatedChange = true; // makes the setter skips its thing
        Nourishment += delta;
    }

    private void IndicateNourishmentChange(float delta, GameObject centerHere=null)
    {
        if (delta == 0f)
            return;
        centerHere ??= Util.GetWorld().Player.gameObject;
        Vector2 viewportPos = Util.GetWorld().Camera.WorldToViewportPoint(centerHere.transform.position);
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.02f, 0.98f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        var cam = Util.GetWorld().Camera;
        Vector2 canvasPos = new Vector2(
            (viewportPos.x - 0.5f) * cam.pixelWidth + canvasRect.position.x,
            (viewportPos.y - 0.5f) * cam.pixelHeight + canvasRect.position.y);
        canvasPos.x += 2 * (Random.value - 0.5f) * 0.08f * cam.pixelWidth;
        canvasPos.y += 2 * (Random.value - 0.5f) * 0.1f * cam.pixelHeight;
        var indicator = Instantiate(NourishmentGainIndicator, canvasPos, Quaternion.identity);
        indicator.transform.SetParent(_canvas.transform, true);
        var text = indicator.GetComponentInChildren<TMP_Text>();
        text.text = $"{delta:+0.##;-0.##}";
        text.color = delta > 0 ? Color.green : Color.red;
        text.DOColor(Color.clear, 3f).OnKill(() => Destroy(indicator));
        float floatShift = viewportPos.y > 0.8f ? -0.3f : 0.3f;
        indicator.transform.DOLocalMoveY(indicator.transform.localPosition.y + floatShift * cam.pixelHeight, 3f);
        indicator.transform.localScale = Mathf.Clamp(0.4f + Mathf.Log10(Mathf.Abs(delta)), 0.5f, 2f) * Vector3.one;
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
        _canvas = GameObject.FindGameObjectWithTag("Canvas");
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = GrowthSprites[Level];
        Nourishment = _startingNourishment;
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
        Instantiate(BeeQueenPrefab, transform.position, Quaternion.identity);
    }
    private bool HasCompletedNourishmentCondition => _nourishment >= NourishmentForLevel.Last();
    private bool HasCompletedBeeCondition => QueenLevel >= NourishmentForLevel.Length - 1;
    private void UpdateProgress()
    {
        if (_maxLevelReached >= RequiredLevelAmberBreak && !_victoryAchieved)
        {
            VictoryProgress.text = $"Bees saved: {QueenLevel} / {NourishmentForLevel.Length - 1}  ";
        }
        else if (QueenLevel > 0)
        {
            VictoryProgress.text = $"Bees saved: {QueenLevel}  ";
        }
        else
        {
            VictoryProgress.text = "";
        }
    }

    public void CheckVictory()
    {
        if (!_victoryAchieved && HasCompletedBeeCondition && HasCompletedNourishmentCondition)
        {
            _victoryAchieved = true;
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
        _nourishmentDecreased = (_nourishment < _nourishmentOld);
        var decreasedByHowMuch = _nourishmentOld - _nourishment;
        _nourishmentOld = _nourishment;
        if (passed)
            return;

        if (HasCompletedBeeCondition && _nourishmentDecreased)
        {
            var bonusGain = BeeBonus * (QueenLevel - NourishmentForLevel.Length + 1);
            bonusGain = Math.Min(decreasedByHowMuch, bonusGain); // don't gain more than was spent
            bonusGain = Math.Min(4f, bonusGain); // digging regular tile shouldn't cost less than 1
            ModifyNourishmentWithSource(bonusGain, gameObject);
        }
    }
}
