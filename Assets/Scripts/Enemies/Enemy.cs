
using System.Linq;
using DG.Tweening;
using UnityEngine;

public abstract class Enemy : Entity
{
    public bool Alive = true;
    public virtual float NourishmentFromCorpse => 50;
    public float Breath = 40.0f;
    public float MaxBreath = 40.0f;

    protected Tween ColorTween;


    public override bool CanSpread(Player player, Direction spreadDirection)
    {
        return !Alive;
    }

    public override void OnSpread(Player player, Direction spreadDirection)
    {
        // TODO sound + visual effect?
        Util.GetFlower().Nourishment += NourishmentFromCorpse;
        Util.GetAudioManager().Play("Corpse");
        Remove();
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
        if (!Alive)
            return;

        if (entity is Rock rock && moveDirection == Direction.Down && !CanMove(moveDirection))
        {
            Util.GetAudioManager().Play("hit");
            Kill();
            return;
        }
        Move(moveDirection);
    }

    public abstract void AiStep();

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
        }

        // we might have died in the above so need to re-check!
        if (Alive)
        {
            ColorTween?.Kill();
            var sprite = GetComponent<SpriteRenderer>();
            ColorTween =
                sprite.DOColor(new Color(0.5f * Breath / MaxBreath + 0.5f, 0.5f * Breath / MaxBreath + 0.5f, 1), 0.2f);
            AiStep();
        }

        if (AffectedByGravity)
            Fall();
    }

    public virtual void SetDeathSprite()
    {
        MoveTween?.Kill();
        FallTween?.Kill();
        var sprite = GetComponent<SpriteRenderer>();
        ColorTween?.Complete();
        ColorTween = sprite.DOColor(new Color(1f, 1f, 1f), 0.2f);
        // rotate 180 degrees as default
        transform.DORotate(new Vector3(0, 0, 180), 0.2f);
        LocalShift = new Vector3(0, -0.8f, 0);
        transform.DOMove(LocalShift  + new Vector3(X - World.MapWidth / 2, Y, 0), 0.2f);
    }

    public void Kill()
    {
        if (!Alive)
            return;
        Util.GetAudioManager().Play("Death");
        Alive = false;
        SetDeathSprite();
    }
}