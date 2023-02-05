using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snail : Slug
{
    public override float NourishmentFromCorpse => 60;

    public override void SetDeathSprite()
    {
        _eatTween.Kill();
        _rotateTween.Kill();
        moveTween.Kill();
        fallTween.Kill();
        if (_animator == null)
            _animator = GetComponent<Animator>();
        _animator.SetTrigger("Die");
        var sprite = GetComponent<SpriteRenderer>();
        _colorTween?.Kill();
        _colorTween = sprite.DOColor(new Color(1f, 1f, 1f), 0.2f);
        transform.DORotate(new Vector3(0, 0, 0), 0.2f);
        transform.DOMove(new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f);
    }
}
