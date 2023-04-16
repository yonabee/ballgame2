using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise
{
    public Noise()
    {
        Randomize();
    }

    public Noise(int seed)
    {
        Randomize(seed);
    }

    public virtual float Evaluate(Vector3 point) 
    {
        return 0;
    }
    protected virtual void Randomize() {}
    protected virtual void Randomize(int seed) {}
}
