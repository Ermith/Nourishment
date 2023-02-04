
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
    SmallRock,
}

public class EntityFactory
{
    public static Entity PlaceEntity(EntityType type, int x, int y)
    {
        Entity entity = null;
        switch (type)
        {
            case EntityType.SmallRock:
                entity = new SmallRock(x, y);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        
        foreach (var location in entity.Locations)
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2);
            square.Entities.Add(entity);
        }

        return entity;
    }
}

public abstract class Entity : MonoBehaviour
{
    public List<(int, int)> Locations;
    public int X;
    public int Y;

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

        return true;
    }
}


public class SmallRock : Entity
{
    public SmallRock(int x, int y)
    {
        X = x;
        Y = y;
        Locations = new List<(int, int)>();
        Locations.Add((x, y));
    }

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

    public override void SimulationStep()
    {
        // TODO
    }
}