
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public List<(int, int)> Locations;

    public abstract void OnTick();

    public virtual bool CanSpread(Player player)
    {
        return true;
    }
}
