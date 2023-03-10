using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snail : Slug
{
    public override float NourishmentFromCorpse => 65;

    public override void SetDeathSprite()
    {
        EatTween.Complete();
        RotateTween.Complete();
        MoveTween.Complete();
        FallTween.Complete();
        if (Animator == null)
            Animator = GetComponent<Animator>();
        Animator.SetTrigger("Die");
        var sprite = GetComponent<SpriteRenderer>();
        
        // shouldn't be necessary but somehow is in the edge case where the snail dies immediately after spawning
        sprite.sprite = Util.GetWorld().Sprites["snail_dead"];
        
        ColorTween?.Complete();
        ColorTween = sprite.DOColor(new Color(1f, 1f, 1f), 0.2f);
        transform.DORotate(new Vector3(0, 0, 0), 0.2f);
        transform.DOMove(new Vector3(X - World.MapWidth / 2, Y, 0), 0.2f);
    }
}
