using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planetoid : MonoBehaviour
{
    public bool autoUpdate = false;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public Vector3 initialVelocity;
    Vector3 currentVelocity;

    [HideInInspector]
    public bool shapeSettingsFoldout;
    [HideInInspector]
    public bool colourSettingsFoldout;

    void Awake()
    {
        currentVelocity = initialVelocity;
    }

    void Start()
    {
        GeneratePlanet();
    }

    public virtual void GeneratePlanet() {}

    public virtual void OnShapeSettingsUpdated() {}

    public virtual void OnColorSettingsUpdated() {}

    public virtual void GenerateMesh() {}

    public virtual void GenerateColors() {}

    public void UpdateVelocity(Planetoid[] allBodies, float timeStep) 
    {
        foreach(var otherBody in allBodies) 
        {
            if (otherBody != this)
            {
                Vector3 distance = otherBody.transform.position - transform.position;
                float sqrDst = distance.sqrMagnitude;
                Vector3 forceDir = distance.normalized;
                Vector3 force = forceDir * Physics.gravity.y * shapeSettings.mass * otherBody.shapeSettings.mass / sqrDst;
                Vector3 acceleration = force / shapeSettings.mass;
                currentVelocity += acceleration * timeStep;
            }
        }
    }

    public void UpdatePosition(float timeStep) 
    {
        transform.position += currentVelocity * timeStep;
    }
}
