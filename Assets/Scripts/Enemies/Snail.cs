using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snail : Slug
{
    public override void SetDeathSprite()
    {
        _animator.SetTrigger("Death");
    }
}
