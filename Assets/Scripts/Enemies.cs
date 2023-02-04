
using UnityEngine;

public class Enemy : Entity
{
    public bool Alive = true;
}

public class Slug : Enemy
{
    public override void Initialize(int x, int y)
    {
        base.Initialize(x, y);
        gameObject.AddComponent<SpriteRenderer>().sortingLayerName = "Entity";
        gameObject.AddComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/Slug");
    }
}