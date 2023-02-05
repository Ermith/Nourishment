using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public class Slug : Enemy
{
    public Direction DownDir = Direction.Down;
    public Direction ForwardDir;
    private bool _rotatesClockwise;
    private Tween _rotateTween;
    protected Animator _animator;

    private Sequence _eatTween;

    public override void Initialize(int x, int y)
    {
        _rotatesClockwise = Random.Range(0, 2) == 0;
        ForwardDir = _rotatesClockwise ? Direction.Left : Direction.Right;
        if (_rotatesClockwise)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        base.Initialize(x, y);
    }
    
    public override bool AffectedByGravity {
        get
        {
            var squareBelow = Util.GetWorld().GetSquare(X + DownDir.X(), Y + DownDir.Y(), true);
            return !Alive || (squareBelow?.CanPass(this, DownDir) ?? false);
        }
    }

    public void Rotate(bool reverse = false)
    {
        bool cw = reverse ? !_rotatesClockwise : _rotatesClockwise;
        ForwardDir = cw ? ForwardDir.Clockwise() : ForwardDir.CounterClockwise();
        DownDir = cw ? DownDir.Clockwise() : DownDir.CounterClockwise();

        _rotateTween?.Kill();
        var visAngle = DownDir.CounterClockwise().Angle();
        _rotateTween = transform.DORotate(new Vector3(0, 0, visAngle), 0.2f);
    }
    public override void SetDeathSprite()
    {
        base.SetDeathSprite();
        if (_animator == null)
            _animator = GetComponent<Animator>();
        _animator.SetTrigger("StopMoving");
    }

    public override void AIStep()
    {
        if (AffectedByGravity) // we fell but are still directed weirdly!
        {
            Rotate();
            return;
        }
        var tileBelow = Util.GetWorld().GetTile(X + DownDir.X(), Y + DownDir.Y());
        if (tileBelow is RootTile rootBelow)
        {
            rootBelow.Health -= 0.05f;
            _eatTween?.Kill();
            _eatTween = DOTween.Sequence();
            var downVec = DownDir.ToVector();
            var baseScale = transform.localScale;
            _eatTween.Append(transform.DOScale(new Vector3(baseScale.x, baseScale.y / 2, baseScale.z), 0.1f));
            _eatTween.Join(transform.DOMove(transform.position + new Vector3(0.5f * downVec.x, 0.5f * downVec.y, 0), 0.1f));
            _eatTween.Append(transform.DOScale(baseScale, 0.1f));
            _eatTween.Join(transform.DOMove(transform.position, 0.1f));
            return;
        }
        var squareForward = Util.GetWorld().GetSquare(X + ForwardDir.X(), Y + ForwardDir.Y());
        if (squareForward?.CanPass(this, ForwardDir) ?? false)
        {
            var belowForward = Util.GetWorld().GetSquare(X + ForwardDir.X() + DownDir.X(), Y + ForwardDir.Y() + DownDir.Y());
            if (belowForward is null || !belowForward.CanPass(this, DownDir))
            {
                Move(ForwardDir);
            }
            else
            {
                Move(ForwardDir);
                Rotate(reverse: true);
                Move(ForwardDir);
            }
        }
        else
        {
            Rotate();
        }
    }

    public override bool Move(Direction direction, bool tween = true)
    {
        bool success = base.Move(direction, tween);
        if (success && Alive)
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            _animator.SetTrigger("Move");
            moveTween.OnKill(() => _animator.SetTrigger("StopMoving"));
        }
        return success;
    }
}
