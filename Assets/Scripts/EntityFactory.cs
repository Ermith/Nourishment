using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityFactory : MonoBehaviour
{

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
            case EntityType.AmberBee:
                entity = go.AddComponent<AmberBee>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        entity.name = type.ToString();
        entity.Initialize(x, y);
        entity.Activity = 0;

        entity.UpdateSprite();
        go.transform.position =
        new Vector2(
                x: -(World.MAP_WIDTH / 2) + x,
                y: y
            );

        foreach (var location in entity.GetLocations())
        {
            var square = Util.GetWorld().GetSquare(location.Item1, location.Item2, true);
            if (square is null)
            {
                Destroy(entity);
                return null;
            }
            square.Entities.Add(entity);
        }

        return entity;
    }
}
