
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

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
            if (square == null || square.Y >= World.MIN_ENTITY_Y || square.Entities.Count > 1 || square.Entities.Count == 1 && square.Entities[0] != this || square.Tile is SuperGroundTile)
                return false;
        }

        return true;
    }

    public void Remove()
    {
        OnRemove();
        Destroy(gameObject);
    }

    public virtual void OnRemove()
    {
        World world = Util.GetWorld();
        if (world is null)
            return;
        if(Locations is not null)
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
        return Heaviness >= 0
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

public class AmberBee : Entity
{
    public override bool AffectedByGravity => true;
    public override float Heaviness => 750;

    public override bool CanSpread(Player player, Direction spreadDirection)
    {
        return CanMove(spreadDirection) || Util.GetFlower().IsAbleToBreakAmber();
    }

    public override void OnSpread(Player player, Direction spreadDirection)
    {
        if (base.CanSpread(player, spreadDirection))
        {
            // miro TODO: spawn bee above ground?
            var flower = Util.GetFlower();

            if (flower.IsAbleToBreakAmber())
            {
                if (flower.CanObtainQueen())
                    flower.ObtainBeeQueen();
                else
                    flower.PowerUpQueen();

                flower.BreakAmber();
                Remove();
                return;
            }
        }
        
        Move(spreadDirection);
    }

    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        return CanMove(moveDirection) || entity is Rock && moveDirection == Direction.Down;
    }

    public override void OnPass(Entity entity, Direction moveDirection)
    {
        if (entity is Rock && moveDirection == Direction.Down && !CanMove(moveDirection))
        {
            Remove();
            Util.GetEntityFactory().PlaceEntity(Util.GetWorld().gameObject, EntityType.Bee, X, Y);
        }
        else
            Move(moveDirection);
    }

    public override void Initialize(int x, int y)
    {
        base.Initialize(x, y);
        var renderer = this.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = Util.GetWorld().Sprites["bee_queen"];
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

    public override bool CanSpread(Player player, Direction spreadDirection)
    {
        return CanMove(spreadDirection);
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

public class RandomRock : Rock
{
    public int Width = 3;
    public int Height = 3;
    public float Chance = 0.35f;

    protected bool[,] _shape = null;

    protected override bool[,] GetShape()
    {
        if (_shape != null)
            return _shape;
        
        var shape = new bool[Width, Height];
        for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
                shape[i, j] = Random.value < Chance;
        for (int i = 0; i < Width; i++)
        {
            bool found = false;
            for (int j = 0; j < Height; j++)
            {
                if (shape[i, j])
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                int j = (int)(Random.value * Random.value * Height);
                shape[i, j] = true;
            }
        }
        for (int j = 0; j < Height; j++)
        {
            bool found = false;
            for (int i = 0; i < Width; i++)
            {
                if (shape[i, j])
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                int i = (int)(Random.value * Random.value * Width);
                shape[i, j] = true;
            }
        }

        // make it convex-ish
        for (int i = 0; i < Width; i++)
        {
            int minJ = -1;
            int maxJ = -1;
            for (int j = 0; j < Height; j++)
            {
                if (shape[i, j])
                {
                    if (minJ == -1)
                        minJ = j;
                    maxJ = j;
                }
            }
            if (minJ != -1)
            {
                for (int j = minJ; j <= maxJ; j++)
                    shape[i, j] = true;
            }
        }
        for (int j = 0; j < Height; j++)
        {
            int minI = -1;
            int maxI = -1;
            for (int i = 0; i < Width; i++)
            {
                if (shape[i, j])
                {
                    if (minI == -1)
                        minI = i;
                    maxI = i;
                }
            }
            if (minI != -1)
            {
                for (int i = minI; i <= maxI; i++)
                    shape[i, j] = true;
            }
        }

        // make it connected
        
        bool[,] visited = new bool[Width, Height];
        Queue<(int, int)> queue = new Queue<(int, int)>();
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                if (shape[i, j])
                {
                    queue.Enqueue((i, j));
                    visited[i, j] = true;
                    break;
                }
            }
            if (queue.Count > 0)
                break;
        }

        while (queue.Count > 0)
        {
            var (i, j) = queue.Dequeue();
            foreach (var dir in Util.CARDINAL_DIRECTIONS)
            {
                int i1 = i + dir.X();
                int j1 = j + dir.Y();
                if (i1 < 0 || i1 >= Width || j1 < 0 || j1 >= Height)
                    continue;
                if (visited[i1, j1])
                    continue;
                if (shape[i1, j1])
                {
                    queue.Enqueue((i1, j1));
                    visited[i1, j1] = true;
                }
            }
        }

        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                if (!visited[i, j])
                    shape[i, j] = false;
            }
        }

        _shape = shape;
        return shape;
    }
}