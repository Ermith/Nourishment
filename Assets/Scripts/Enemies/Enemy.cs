
using UnityEngine;

public class Enemy : Entity
{
    public bool Alive = true;

    public override bool CanFluidPass(Fluid fluid, Direction moveDirection)
    {
        return true;
    }
}