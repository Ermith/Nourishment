using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class Bee : Enemy
{
    public bool IsOverworldBee = false;
    public float StepLength = 0.01f;
    public float MinMove = -6;
    public float MaxMove = 6;
    public Direction ForwardDir = Direction.Right;
    public Direction BackwardDir = Direction.Left;
    private Direction _currentDir = Direction.Right;
    private Vector3 _toLeft = new Vector3(-1, 1, 1);
    private Vector3 _toRight = new Vector3(1, 1, 1);

    public override bool AffectedByGravity => !Alive;

    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        if (entity is Enemy)
            return true;
        return base.CanPass(entity, moveDirection);
    }

    private void Flip()
    {
        if (_currentDir == Direction.Left)
        {
            transform.localScale = _toRight;
            _currentDir = Direction.Right;
        }
        else if (_currentDir == Direction.Right)
        {
            transform.localScale = _toLeft;
            _currentDir = Direction.Left;
        }
    }

    public void Start()
    {
        if (IsOverworldBee)
        {
            MinMove += 2 * 2 * (Random.value - 0.5f);
            MaxMove += 2 * 2 * (Random.value - 0.5f);
            StepLength += 0.005f * 2 * (Random.value - 0.5f);
            transform.position = new Vector2(
                2 * (Random.value - 0.5f) * (MaxMove - MinMove) + MinMove,
                transform.position.y + 2 * (Random.value - 0.5f));
            float scale = 0.5f + 0.3f * 2 * (Random.value - 0.5f);
            transform.localScale = new Vector3(scale, scale, 1);
        }
    }

    public void Update()
    {
        if (IsOverworldBee)
        {
            OverworldFly();
        }
    }

    private void OverworldFly()
    {
        // swap direction
        if (_currentDir == Direction.Left && transform.position.x <= MinMove)
        {
            _currentDir = Direction.Right;
            StepLength = -StepLength;
            transform.localScale = new Vector3(0.5f, 0.5f, 1);

        }

        if (_currentDir == Direction.Right && transform.position.x >= MaxMove)
        {
            _currentDir = Direction.Left;
            StepLength = -StepLength;
            transform.localScale = new Vector3(-0.5f, 0.5f, 1);
        }

        // move
        transform.position = new Vector3(transform.position.x + StepLength, 
            transform.position.y,
            transform.position.z);
    }

    public override bool Move(Direction direction, bool tween = true)
    {
        var square = Util.GetWorld().GetSquare(X + direction.X(), Y + direction.Y());
        if (square is null)
            return false;
        foreach (var entity in square.Entities)
        {
            if (entity is Enemy enemy && enemy is not Bee)
            {
                if (enemy.Alive)
                {
                    enemy.Kill();
                    if (Random.value < 0.2f)
                        Kill();
                    return false;
                }
            }
        }
        return base.Move(direction, tween);
    }

    public override void AiStep()
    {
        if (IsOverworldBee)
        {
            OverworldFly();
            return;
        }

        if (!Move(_currentDir))
            Flip();
    }

    public override bool CanSpread(Player player, Direction spreadDirection)
    {
        return CanMove(spreadDirection) || base.CanSpread(player, spreadDirection);
    }

    public override void OnSpread(Player player, Direction spreadDirection)
    {
        if (!Alive)
            base.OnSpread(player, spreadDirection);
        else if (CanMove(spreadDirection))
            Move(spreadDirection);
    }
}
