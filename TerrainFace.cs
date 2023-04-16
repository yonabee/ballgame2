using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace 
{
    ShapeGenerator shapeGenerator;
    ShapeSettings shapeSettings;
    Mesh landMesh;
    Mesh oceanMesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    Vector3[] verts;
    Vector3[] oceanVerts;
    Vector2[] uv;
    int[] tris;

    public TerrainFace(ShapeGenerator shapeGenerator, ShapeSettings settings, Mesh landMesh, Mesh oceanMesh, int resolution, Vector3 localUp)
    {
        this.shapeSettings = settings;
        this.shapeGenerator = shapeGenerator;
        this.landMesh = landMesh;
        this.oceanMesh = oceanMesh;
        this.resolution = resolution;
        this.localUp = localUp;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);

        verts = new Vector3[resolution * resolution];
        oceanVerts = new Vector3[resolution * resolution];
        uv = new Vector2[resolution * resolution];
        tris = new int[(resolution - 1) * (resolution - 1) * 6];
    }

    public void ConstructMesh()
    {
        int triIndex = 0;
        uv = (landMesh.uv.Length == verts.Length) ? landMesh.uv : new Vector2[verts.Length];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                Elevation elevation = shapeGenerator.GetElevation(pointOnUnitSphere);

                verts[i] = pointOnUnitSphere * elevation.scaled;
                oceanVerts[i] = pointOnUnitSphere * shapeSettings.radius;
                uv[i].y = elevation.unscaled;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    tris[triIndex] = i;
                    tris[triIndex + 1] = i + resolution + 1; 
                    tris[triIndex + 2] = i + resolution;

                    tris[triIndex + 3] = i;
                    tris[triIndex + 4] = i + 1;
                    tris[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
        landMesh.Clear();
        landMesh.vertices = verts;
        landMesh.SetTriangles(tris, 0);
        landMesh.RecalculateNormals();
        landMesh.uv = uv;

        oceanMesh.Clear();
        oceanMesh.vertices = oceanVerts;
        oceanMesh.SetTriangles(tris, 0);
        oceanMesh.RecalculateNormals();
    }

    public void UpdateUVs(ColorGenerator colourGenerator)
    {
        Vector2[] uv = landMesh.uv;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                uv[i].x = colourGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }
        landMesh.uv = uv;
    }
}