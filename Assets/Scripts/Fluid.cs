
using System.Collections.Generic;
using UnityEngine;

public abstract class Fluid
{
    public float Amount = 0.0f;
    protected float NextAmount = 0.0f;

    public abstract float FlowRate();

    protected abstract (Fluid, GridSquare) GetFluid(int x, int y);

    public void SimSwap()
    {
        Amount = NextAmount;
        NextAmount = 0.0f;
    }

    protected void AddFluid(int x, int y, float amount)
    {
        (Fluid fluid, _) = GetFluid(x, y);
        fluid.NextAmount += amount;
    }

    public void FixDisplacement(GridSquare square)
    {
        List<GridSquare> availTargets = new List<GridSquare>();
        foreach (var dir in Util.CARDINAL_DIRECTIONS)
        {
            var neighSquare = Util.GetWorld().GetSquare(square.X + dir.X(), square.Y + dir.Y());
            if (neighSquare?.CanFluidPass(this, dir) ?? false)
                availTargets.Add(neighSquare);
        }

        if (availTargets.Count == 0)
        {
            var aboveSquare = square;
            while (aboveSquare is not null && !aboveSquare.CanFluidPass(this, Direction.Up))
            {
                aboveSquare = Util.GetWorld().GetSquare(aboveSquare.X, aboveSquare.Y + 1);
            }

            if (aboveSquare is null)
            {
                Amount = 0.0f; // rip
                return;
            }
            else
                availTargets.Add(aboveSquare);
        }

        float amountPerTarget = Amount / availTargets.Count;
        foreach (var target in availTargets)
        {
            AddFluid(target.X, target.Y, amountPerTarget);
        }
        Amount = 0.0f;
    }

    public void Flow(int x, int y)
    {
        if (Amount <= 0.0f) return;
        float amountLeft = Amount;
        
        (Fluid below, GridSquare belowSquare) = GetFluid(x, y - 1);
        bool belowPass = belowSquare != null && belowSquare.CanFluidPass(this, Direction.Down);
        if (belowPass && below.Amount < 1.0f && below.NextAmount < 1.0f)
        {
            float flowRate = FlowRate();
            float flowAmount = Mathf.Min(1.0f - below.Amount, 1.0f - below.NextAmount, flowRate * 10, amountLeft);
            AddFluid(x, y - 1, flowAmount);
            amountLeft -= flowAmount;
        }

        (Fluid left, GridSquare leftSquare) = GetFluid(x - 1, y);
        bool leftPass = leftSquare != null && leftSquare.CanFluidPass(this, Direction.Left);
        
        (Fluid leftBelow, GridSquare leftBelowSquare) = GetFluid(x - 1, y - 1);
        bool leftBelowPass = leftBelowSquare != null && leftBelowSquare.CanFluidPass(this, Direction.Down);
        if (leftPass && leftBelowPass && leftBelow.Amount < 1.0f && left.NextAmount < 1.0f)
        {
            float flowRate = FlowRate();
            float flowAmount = Mathf.Min(1.0f - leftBelow.Amount, 1.0f - leftBelow.NextAmount, flowRate * 3, amountLeft);
            AddFluid(x - 1, y - 1, flowAmount);
            amountLeft -= flowAmount;
        }

        (Fluid right, GridSquare rightSquare) = GetFluid(x + 1, y);
        bool rightPass = rightSquare != null && rightSquare.CanFluidPass(this, Direction.Right);
        
        (Fluid rightBelow, GridSquare rightBelowSquare) = GetFluid(x + 1, y - 1);
        bool rightBelowPass = rightBelowSquare != null && rightBelowSquare.CanFluidPass(this, Direction.Down);
        if (rightPass && rightBelowPass && rightBelow.Amount < 1.0f && rightBelow.NextAmount < 1.0f)
        {
            float flowRate = FlowRate();
            float flowAmount = Mathf.Min(1.0f - rightBelow.Amount, 1.0f - rightBelow.NextAmount, flowRate * 3, amountLeft);
            AddFluid(x + 1, y - 1, flowAmount);
            amountLeft -= flowAmount;
        }
        
        if (rightPass && right.Amount < Amount && right.NextAmount < 1.0f)
        {
            float flowRate = FlowRate();
            float flowAmount = Mathf.Min(Amount - right.Amount, 1.0f - right.NextAmount, flowRate, amountLeft);
            AddFluid(x + 1, y, flowAmount);
            amountLeft -= flowAmount;
        }

        if (leftPass && left.Amount < Amount && left.NextAmount < 1.0f)
        {
            float flowRate = FlowRate();
            float flowAmount = Mathf.Min(Amount - left.Amount, 1.0f - left.NextAmount, flowRate, amountLeft);
            AddFluid(x - 1, y, flowAmount);
            amountLeft -= flowAmount;
        }

        NextAmount += amountLeft;
    }
}


public class Water : Fluid
{
    public override float FlowRate() => 0.01f;

    protected override (Fluid, GridSquare) GetFluid(int x, int y)
    {
        World world = Util.GetWorld();
        var square = world.GetSquare(x, y);
        return (square?.Water, square);
    }
}



public class FluidIndicator : MonoBehaviour
{
    public GridSquare Square;
    public SpriteRenderer SpriteRenderer;
    const float BASE_ALPHA = 0.5f;
    readonly Color BASE_COLOR = new Color(0.2f, 0.2f, 1.0f, 1.0f);

    private void Start()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
        SpriteRenderer.sortingOrder = 1;
        // SpriteRenderer.sortingLayerName = "Fluid";
        SpriteRenderer.sprite = Resources.Load<Sprite>("Sprites/white");
    }

    private void Update()
    {
        SpriteRenderer.enabled = Square.Water.Amount > 0.0f;
        var squareAbove = Util.GetWorld().GetSquare(Square.X, Square.Y + 1);
        
        if (squareAbove != null && squareAbove.Water.Amount > 0.0f)
        {
            SpriteRenderer.color = new Color(BASE_COLOR.r, BASE_COLOR.g, BASE_COLOR.b, BASE_ALPHA * Square.Water.Amount);
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        }
        else
        {
            SpriteRenderer.color = new Color(BASE_COLOR.r, BASE_COLOR.g, BASE_COLOR.b, BASE_ALPHA);
            transform.localScale = new Vector3(1.0f, Square.Water.Amount, 1.0f);
            transform.localPosition = new Vector3(0.0f, -0.5f + Square.Water.Amount / 2, 0.0f);
        }
    }
}