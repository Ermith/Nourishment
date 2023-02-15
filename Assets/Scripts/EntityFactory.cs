using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
    SmallRock,
    SquareRock,
    Slug,
    Snail,
    Bee,
    AmberBee,
    RandomRock3X3,
    RandomRock4X4,
    RandomRock5X5,
    RandomRock6X6,
    RandomRock6X3,
    RandomRock3X6,
}
public class EntityFactory : MonoBehaviour
{
    public GameObject Bee;
    public GameObject Slug;
    public GameObject Snail;

    private void Start()
    {
    }

    public Entity PlaceEntity(GameObject parent, EntityType type, int x, int y)
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
            case EntityType.Slug:
                go = Instantiate(Slug);
                entity = go.GetComponent<Slug>();
                break;
            case EntityType.Snail:
                go = Instantiate(Snail);
                entity = go.GetComponent<Snail>();
                break;
            case EntityType.Bee:
                go = Instantiate(Bee);
                entity = go.GetComponent<Bee>();
                break;
            case EntityType.AmberBee:
                entity = go.AddComponent<AmberBee>();
                break;
            default:
                if (type.ToString().StartsWith("RandomRock"))
                {
                    var size = type.ToString().Substring("RandomRock".Length);
                    var parts = size.Split("X");
                    var width = int.Parse(parts[0]);
                    var height = int.Parse(parts[1]);
                    entity = go.AddComponent<RandomRock>();
                    ((RandomRock)entity).Width = width;
                    ((RandomRock)entity).Height = height;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }

                break;
        }

        entity.name = type.ToString();
        entity.Initialize(x, y);
        entity.Activity = 0;

        entity.UpdateSprite();
        go.transform.position =
        new Vector2(
                x: -(World.MapWidth / 2) + x,
                y: y
            );

        foreach (var location in entity.GetLocations())
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2, true);
            if (square is null)
            {
                entity.Remove();
                return null;
            }
            square.Entities.Add(entity);
        }

        return entity;
    }
}
