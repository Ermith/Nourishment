
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.FilePathAttribute;

public enum EntityType
{
    SmallRock,
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
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        
        entity.name = type.ToString();
        entity.Initialize(x, y);
        
        // entity.UpdateSprite();
        go.transform.position =
        new Vector2(
                x: -(World.MAP_WIDTH / 2) + x,
                y: y
            );

        foreach (var location in entity.GetLocations())
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2);
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

    public List<(int, int)> GetLocations()
    {
        return Locations;
    }

    public abstract void SimulationStep();

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

    public bool Move(Direction direction)
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
        
        moveTween?.Kill();
        moveTween = gameObject.transform.DOMove(new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f);

        return true;
    }

    public virtual void Initialize(int x, int y)
    {
        X = x;
        Y = y;
        Locations = new List<(int, int)>();
        Locations.Add((x, y));
    }
}


public class SmallRock : Entity
{
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

    public override void SimulationStep()
    {
        // TODO
    }

    public override void Initialize(int x, int y)
    {
        base.Initialize(x, y);
        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Util.GetWorld().Sprites["stone-00-corner"];
        spriteRenderer.sortingLayerName = "Entity";
    }
}