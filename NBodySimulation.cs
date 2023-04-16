using UnityEngine;

public class NBodySimulation : MonoBehaviour 
{
    Planetoid[] bodies;

    void Awake() 
    {
        bodies = FindObjectsOfType<Planetoid>();
    }

    void FixedUpdate() 
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].UpdateVelocity(bodies, Time.fixedDeltaTime);
        }

        for (int i = 0; i < bodies.Length; i++) 
        {
            bodies[i].UpdatePosition(Time.fixedDeltaTime);
        }
    }
}