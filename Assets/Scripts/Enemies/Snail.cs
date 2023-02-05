using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snail : Slug
{
    public override float NourishmentFromCorpse => 60;

    public override void SetDeathSprite()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();
        _animator.SetTrigger("Die");
    }
}
