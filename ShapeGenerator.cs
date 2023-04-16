using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Elevation
{
    public float unscaled;
    public float scaled;
}

public class ShapeGenerator {

    ShapeSettings settings;
    INoiseFilter[] noiseFilters;
    public MinMax elevationMinMax;

    public void UpdateSettings(ShapeSettings settings)
    {
        this.settings = settings;
        noiseFilters = new INoiseFilter[settings.noiseLayers.Length];
        for (int i = 0; i < noiseFilters.Length; i++)
        {
            noiseFilters[i] = NoiseFilterFactory.CreateNoiseFilter(settings.noiseLayers[i].noiseSettings);
        }
        elevationMinMax = new MinMax();
    }

    public Elevation GetElevation(Vector3 pointOnUnitSphere) {
        Elevation result = new Elevation();
        result.unscaled = CalculateUnscaledElevation(pointOnUnitSphere);
        result.scaled = GetScaledElevation(result.unscaled);
        return result;
    }

    float CalculateUnscaledElevation(Vector3 pointOnUnitSphere)
    {
        float firstLayerValue = 0;
        float elevation = 0;

        if (noiseFilters.Length > 0)
        {
            firstLayerValue = noiseFilters[0].Evaluate(pointOnUnitSphere);
            if (settings.noiseLayers[0].enabled)
            {
                elevation = firstLayerValue;
            }
        }

        for (int i = 1; i < noiseFilters.Length; i++)
        {
            if (settings.noiseLayers[i].enabled)
            {
                float mask = (settings.noiseLayers[i].useFirstLayerAsMask) ? firstLayerValue : 1;
                elevation += noiseFilters[i].Evaluate(pointOnUnitSphere) * mask;
            }
        }

        elevationMinMax.AddValue(elevation);
        return elevation;
    }

    float GetScaledElevation(float unscaledElevation) 
    {
        float elevation = Mathf.Max(0, unscaledElevation);
        elevation = settings.radius * (1 + unscaledElevation);

        return elevation;
    }
}