using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bee : Enemy
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override bool CanPass(Entity entity, Direction moveDirection) => true;

    public override void AIStep()
    {
        Move(Direction.Left);
    }
}
