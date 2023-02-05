using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class Bee : Enemy
{
    public bool IsOverworldBee = false;
    public float StepLength = 0.01f;
    public float MinMove = -2;
    public float MaxMove = 6;
    public Direction ForwardDir = Direction.Right;
    public Direction BackwardDir = Direction.Left;
    private Direction _currentDir = Direction.Right;
    private Vector3 toLeft = new Vector3(-1, 1, 1);
    private Vector3 toRight = new Vector3(1, 1, 1);

    public override bool CanPass(Entity entity, Direction moveDirection)
    {
        if (entity is Enemy)
            return true;
        return base.CanPass(entity, moveDirection);
    }

    private void Flip()
    {
        if (_currentDir == Direction.Left)
            transform.localScale = toRight;
        else if (_currentDir == Direction.Right)
            transform.localScale = toLeft;
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

    public override void AIStep()
    {
        if (IsOverworldBee)
        {
            OverworldFly();
            return;
        }

        Flip();
        var squareForward = Util.GetWorld().GetSquare(X + ForwardDir.X(), Y + ForwardDir.Y());
        if (_currentDir == Direction.Left && squareForward.CanPass(this, ForwardDir))
        {
            Move(ForwardDir);
            return;
        }
        if (_currentDir == Direction.Left && !squareForward.CanPass(this, ForwardDir))
        {
            _currentDir = Direction.Right;
            Flip();
        }

        var squareBackward = Util.GetWorld().GetSquare(X + BackwardDir.X(), Y + BackwardDir.Y());
        if (_currentDir == Direction.Right && squareBackward.CanPass(this, BackwardDir))
        {
            Move(BackwardDir);
            return;
        }
        if (_currentDir == Direction.Right && !squareBackward.CanPass(this, BackwardDir))
        {
            _currentDir = Direction.Left;
            Flip();
        }
    }
}
