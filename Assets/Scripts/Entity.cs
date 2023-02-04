
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using static UnityEditor.FilePathAttribute;
using static UnityEngine.UI.GridLayoutGroup;

public enum EntityType
{
    SmallRock,
    SquareRock,
    Slug,
    Snail
}

public abstract class Entity : MonoBehaviour
{
    protected List<(int, int)> Locations;
    public int X;
    public int Y;
    protected Tween moveTween;
    protected Tween fallTween;
    public int Activity = 0;
    public virtual float Heaviness => -1;

    protected Vector3 _localShift = new Vector3(0, 0, 0);

    public virtual bool AffectedByGravity => false;

    public List<(int, int)> GetLocations()
    {
        return Locations;
    }

    public void ActivityChange(bool active)
    {
        if(active)
            Activity++;
        else
            Activity--;
        if (Activity <= 0)
        {
            gameObject.SetActive(false);
            Activity = 0;
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public virtual bool IsPlacementValid()
    {
        foreach (var location in Locations)
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2);
            if (square == null || square.Y >= World.MIN_ENTITY_Y || square.Entities.Count > 1 || square.Entities.Count == 1 && square.Entities[0] != this)
                return false;
        }

        return true;
    }

    public virtual void OnDestroy()
    {
        World world = Util.GetWorld();
        if (world is null)
            return;
        foreach (var location in Locations)
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2);
            if (square != null)
                square.Entities.Remove(this);
        }
    }

    public virtual void SimulationStep()
    {
        if (AffectedByGravity)
            Fall();
    }

    protected void Fall()
    {
        int fallenHeight = 0;
        while (Move(Direction.Down, false))
        {
            fallenHeight++;
        }

        if (fallenHeight > 0)
        {
            fallTween?.Kill();

            void FallAnim()
            {
                moveTween = gameObject.transform.DOMove(_localShift + new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f * fallenHeight);
                moveTween.SetEase(Ease.OutBounce);
            }

            if ((moveTween?.IsActive() ?? false) && moveTween.IsPlaying())
                moveTween.OnComplete(FallAnim);
            else
                FallAnim();
        }
    }

    public virtual bool CanFluidPass(Fluid fluid, Direction moveDirection)
    {
        return false;
    }

    public virtual bool CanSpread(Player player, Direction spreadDirection)
    {
        return CanPass(null, spreadDirection)
            && Heaviness >= 0
            && Heaviness < Util.GetFlower().Nourishment;
    }

    public virtual void OnSpread(Player player, Direction spreadDirection)
    {
        Util.GetFlower().Nourishment -= Heaviness;
    }

    // entity can also be null for a general purpose check
    public virtual bool CanPass(Entity entity, Direction moveDirection)
    {
        return false;
    }

    public virtual void OnPass(Entity entity, Direction moveDirection)
    {
    }

    public bool CanMove(Direction direction)
    {
        foreach (var location in Locations)
        {
            var newLocation = (location.Item1 + direction.X(), location.Item2 + direction.Y());
            var square = Util.GetWorld().GetSquare(newLocation.Item1, newLocation.Item2);
            if (square is null)
                return false;
            if (!square.CanPass(this, direction))
                return false;
        }

        return true;
    }

    public virtual bool Move(Direction direction, bool tween = true)
    {
        if (!CanMove(direction))
            return false;
        List<(int, int)> newLocations = new List<(int, int)>();
        foreach (var location in Locations)
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2);
            square.Entities.Remove(this);
            newLocations.Add((location.Item1 + direction.X(), location.Item2 + direction.Y()));
        }

        X += direction.X();
        Y += direction.Y();

        Locations = newLocations;

        foreach (var location in Locations)
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2);
            square.OnPass(this, direction);
        }

        foreach (var location in Locations)
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2);
            square.Entities.Add(this);
        }

        if (tween)
        {
            moveTween?.Kill();
            fallTween?.Kill();
            moveTween = gameObject.transform.DOMove(_localShift + new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f);
            moveTween.SetEase(Ease.InOutCubic);
        }

        return true;
    }

    public virtual void Initialize(int x, int y)
    {
        X = x;
        Y = y;
        Locations = new List<(int, int)>();
        Locations.Add((x, y));
    }

    public virtual void UpdateSprite()
    {
    }
}


public abstract class Rock : Entity
{
    public override bool AffectedByGravity => true;
    public override float Heaviness => 10;

    protected abstract bool[,] GetShape();

    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        return CanMove(moveDirection);
    }

    public override void OnPass(Entity entity, Direction moveDirection)
    {
        base.OnPass(entity, moveDirection);
        Move(moveDirection);
    }

    public override void OnSpread(Player player, Direction spreadDirection)
    {
        base.OnSpread(player, spreadDirection);
        Move(spreadDirection);
    }

    private GameObject AddSubRock(bool[,] shape, int x, int y)
    {
        GameObject subRock = new GameObject();
        subRock.transform.parent = gameObject.transform;
        subRock.transform.position = gameObject.transform.position + new Vector3(x, y, 0);

        var subSprites = Util.GroundLikeSprite(subRock, "stone-", dir =>
        {
            int x1 = x + dir.X();
            int y1 = y + dir.Y();
            if (x1 < 0 || x1 >= shape.GetLength(0) || y1 < 0 || y1 >= shape.GetLength(1))
                return false;
            return shape[x1, y1];
        });

        foreach (var subSprite in subSprites)
            subSprite.GetComponent<SpriteRenderer>().sortingLayerName = "Entity";

        return subRock;
    }

    public override void Initialize(int x, int y)
    {
        base.Initialize(x, y);

        Locations.Clear();
        var shape = GetShape();
        for (int i = 0; i < shape.GetLength(0); i++)
        {
            for (int j = 0; j < shape.GetLength(1); j++)
            {
                if (shape[i, j])
                {
                    AddSubRock(shape, i, j);
                    Locations.Add((x + i, y + j));
                }
            }
        }
    }
}

public class SmallRock : Rock
{
    protected override bool[,] GetShape()
    {
        return new bool[,]
        {
            {true}
        };
    }
}

public class SquareRock : Rock
{
    protected override bool[,] GetShape()
    {
        return new bool[,]
        {
            {true, true},
            {true, true}
        };
    }
}