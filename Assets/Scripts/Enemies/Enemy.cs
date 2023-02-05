
using System.Linq;
using DG.Tweening;
using UnityEngine;

public abstract class Enemy : Entity
{
    public bool Alive = true;
    public virtual float NourishmentFromCorpse => 40;
    public float Breath = 40.0f;
    public float MaxBreath = 40.0f;

    private Tween _colorTween;


    public override bool CanSpread(Player player, Direction spreadDirection)
    {
        return !Alive;
    }

    public override void OnSpread(Player player, Direction spreadDirection)
    {
        // TODO sound + visual effect?
        Util.GetFlower().Nourishment += NourishmentFromCorpse;
        Util.GetAudioManager().Play("Corpse");
        Destroy(gameObject);
    }

    public override bool CanFluidPass(Fluid fluid, Direction moveDirection)
    {
        return true;
    }

    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        if (!Alive)
            return true;

        if (entity is Rock rock)
        {
            if (moveDirection == Direction.Down)
            {
                return true;
            }
            else
            {
                return CanMove(moveDirection);
            }
        }

        return false;
    }

    public override void OnPass(Entity entity, Direction moveDirection)
    {
        if (entity is Rock rock && moveDirection == Direction.Down && !CanMove(moveDirection))
        {
            Kill();
            var squareBelow = Util.GetWorld().GetSquare(X, Y - 1, true);
            while (squareBelow is null || squareBelow.Entities.Any(e => e is Rock))
            {
                Y--;
                squareBelow = Util.GetWorld().GetSquare(X, squareBelow.Y - 1, true);
            }
            fallTween = transform.DOMove(_localShift + new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f);
            return;
        }
        Move(moveDirection);
    }

    public abstract void AIStep();

    public override void SimulationStep()
    {
        base.SimulationStep();

        if (Alive)
        {
            var square = Util.GetWorld().GetSquare(X, Y);
            if (square.Water.Amount >= 0.5f)
            {
                Breath -= square.Water.Amount * 2f;
                if (Breath <= 0f)
                {
                    Kill();
                }
            }
            else
            {
                Breath += 2f;
                if (Breath > MaxBreath)
                    Breath = MaxBreath;
            }
            _colorTween?.Kill();
            var sprite = GetComponent<SpriteRenderer>();
            _colorTween = sprite.DOColor(new Color(0.5f * Breath / MaxBreath + 0.5f, 0.5f * Breath / MaxBreath + 0.5f, 1), 0.2f);
            
            AIStep();
        }
    }

    public virtual void SetDeathSprite()
    {
        var sprite = GetComponent<SpriteRenderer>();
        _colorTween?.Kill();
        _colorTween = sprite.DOColor(new Color(1f, 1f, 1f), 0.2f);
        // rotate 180 degrees as default
        transform.DORotate(new Vector3(0, 0, 180), 0.2f);
        _localShift = new Vector3(0, -0.8f, 0);
        transform.DOMoveY(transform.localPosition.y - 0.8f, 0.2f);
    }

    public void Kill()
    {
        Alive = false;
        SetDeathSprite();
    }
}