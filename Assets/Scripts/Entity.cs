
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
}

public class EntityFactory
{
    public static Entity PlaceEntity(GameObject parent, EntityType type, int x, int y)
    {
        Entity entity = null;
        World world = Util.GetWorld();

        var go = new GameObject();
        go.SetActive(true);
        go.transform.parent = parent.transform;
        
        switch (type)
        {
            case EntityType.SmallRock:
                entity = go.AddComponent<SmallRock>();
                break;
            case EntityType.SquareRock:
                entity = go.AddComponent<SquareRock>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        
        entity.name = type.ToString();
        entity.Initialize(x, y);
        
        entity.UpdateSprite();
        go.transform.position =
        new Vector2(
                x: -(World.MAP_WIDTH / 2) + x,
                y: y
            );

        foreach (var location in entity.GetLocations())
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2, true);
            square.Entities.Add(entity);
        }

        return entity;
    }
}

public abstract class Entity : MonoBehaviour
{
    protected List<(int, int)> Locations;
    public int X;
    public int Y;
    private Tween moveTween;
    private Tween fallTween;

    public virtual bool AffectedByGravity => false;

    public List<(int, int)> GetLocations()
    {
        return Locations;
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
                moveTween = gameObject.transform.DOMove(new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f * fallenHeight);
                moveTween.SetEase(Ease.OutBounce);
            }

            if ((moveTween?.IsActive() ?? false) && moveTween.IsPlaying())
                moveTween.OnComplete(FallAnim);
            else
                FallAnim();
        }
    }

    public virtual bool CanSpread(Player player, Direction spreadDirection)
    {
        return CanPass(null, spreadDirection);
    }

    public virtual void OnSpread(Player player, Direction spreadDirection)
    {
    }

    // entity can also be null for a general purpose check
    public virtual bool CanPass(Entity entity, Direction moveDirection)
    {
        return true;
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

    public bool Move(Direction direction, bool tween = true)
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
            moveTween = gameObject.transform.DOMove(new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f);
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

    protected abstract bool[,] GetShape();

    public override bool CanSpread(Player player, Direction spreadDirection)
    {
        return CanPass(null, spreadDirection);
    }

    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        return CanMove(moveDirection);
    }

    public override void OnPass(Entity entity, Direction moveDirection)
    {
        Move(moveDirection);
    }

    public override void OnSpread(Player player, Direction spreadDirection)
    {
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

        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Util.GetWorld().Sprites["stone-00-corner"];
        spriteRenderer.sortingLayerName = "Entity";
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