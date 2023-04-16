using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePlanet : Planetoid
{
    [SerializeField, Range(2, 256)]
    int resolution = 10;
    int numFaces = 6;
    public enum FaceRenderMask { All, Top, Bottom, Left, Right, Front, Back };
    public FaceRenderMask faceRenderMask;

    ShapeGenerator shapeGenerator = new ShapeGenerator();
    ColorGenerator colorGenerator = new ColorGenerator();

    [SerializeField, HideInInspector]
    GameObject[] landObjects;

    [SerializeField, HideInInspector]
    GameObject[] oceanObjects;
    MeshFilter[] landMeshes;
    MeshFilter[] oceanMeshes;
    MeshRenderer[] landRenderers;
    MeshRenderer[] oceanRenderers;
    MeshCollider[] landColliders;
    MeshCollider[] oceanColliders;
    TerrainFace[] terrainFaces;
    Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

    void Initialize()
    {
        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);

        if (landObjects == null || landObjects.Length == 0)
        {
            landObjects = new GameObject[numFaces];
        }

        if (oceanObjects == null || oceanObjects.Length == 0)
        {
            oceanObjects = new GameObject[numFaces];
        }

        if (landColliders == null || landColliders.Length == 0)
        {
            landColliders = new MeshCollider[numFaces];
        }

        if (oceanColliders == null || oceanColliders.Length == 0)
        {
            oceanColliders = new MeshCollider[numFaces];
        }

        if (landMeshes == null || landMeshes.Length == 0)
        {
            landMeshes = new MeshFilter[numFaces];
        }

        if (oceanMeshes == null || oceanMeshes.Length == 0)
        {
            oceanMeshes = new MeshFilter[numFaces];
        }

        if (landRenderers == null || landRenderers.Length == 0)
        {
            landRenderers = new MeshRenderer[numFaces];
        }

        if (oceanRenderers == null || oceanRenderers.Length == 0)
        {
            oceanRenderers = new MeshRenderer[numFaces];
        }

        terrainFaces = new TerrainFace[6];

        for (int i = 0; i < 6; i++)
        {
            if (landObjects[i] == null)
            {
                landObjects[i] = new GameObject("Land" + i.ToString());
                landObjects[i].transform.parent = transform;
                landObjects[i].transform.localScale = Vector3.one;
                landObjects[i].transform.localPosition = Vector3.zero;
            }

            if (oceanObjects[i] == null)
            {
                oceanObjects[i] = new GameObject("Ocean" + i.ToString());
                oceanObjects[i].transform.parent = transform;
                oceanObjects[i].transform.localScale = Vector3.one;
                oceanObjects[i].transform.localPosition = Vector3.zero;
                oceanObjects[i].layer = LayerMask.NameToLayer("Water");
            }

            if (landRenderers[i] == null)
            {
                landRenderers[i] = landObjects[i].GetComponent<MeshRenderer>();

                if (landRenderers[i] == null)
                {
                    landRenderers[i] = landObjects[i].AddComponent<MeshRenderer>();
                    landRenderers[i].sharedMaterial = colorSettings.planetMaterial;
                }
            }

            if (oceanRenderers[i] == null)
            {
                oceanRenderers[i] = oceanObjects[i].GetComponent<MeshRenderer>();

                if (oceanRenderers[i] == null)
                {
                    oceanRenderers[i] = oceanObjects[i].AddComponent<MeshRenderer>();
                    oceanRenderers[i].sharedMaterial = colorSettings.oceanMaterial;
                }
            }

            if (landMeshes[i] == null)
            {
                landMeshes[i] = landObjects[i].GetComponent<MeshFilter>();

                if (landMeshes[i] == null) 
                {
                    landMeshes[i] = landObjects[i].AddComponent<MeshFilter>();
                    landMeshes[i].sharedMesh = new Mesh();
                }
            }

            if (oceanMeshes[i] == null)
            {
                oceanMeshes[i] = oceanObjects[i].GetComponent<MeshFilter>();

                if (oceanMeshes[i] == null) 
                {
                    oceanMeshes[i] = oceanObjects[i].AddComponent<MeshFilter>();
                    oceanMeshes[i].sharedMesh = new Mesh();
                }
            }

            if (landColliders[i] == null) 
            {
                landColliders[i] = landObjects[i].GetComponent<MeshCollider>();

                if (landColliders[i] == null) 
                {
                    landColliders[i] = landObjects[i].AddComponent<MeshCollider>();
                    landColliders[i].sharedMesh = landMeshes[i].sharedMesh;
                }
            }

            if (oceanColliders[i] == null) 
            {
                oceanColliders[i] = oceanObjects[i].GetComponent<MeshCollider>();

                if (oceanColliders[i] == null) 
                {
                    oceanColliders[i] = oceanObjects[i].AddComponent<MeshCollider>();
                    oceanColliders[i].sharedMesh = oceanMeshes[i].sharedMesh;
                    oceanColliders[i].convex = true;
                    oceanColliders[i].isTrigger = true;
                }
            }

            terrainFaces[i] = new TerrainFace(shapeGenerator, shapeSettings, landMeshes[i].sharedMesh, oceanMeshes[i].sharedMesh, resolution, directions[i]);

            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            landObjects[i].SetActive(renderFace);
            oceanObjects[i].SetActive(renderFace);
        }
    }

    public override void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
        GenerateColors();
    }

    public override void OnShapeSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateMesh();
        }
    }

    public override void OnColorSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateColors();
        }
    }

    public override void GenerateMesh()
    {
        for (int i = 0; i < numFaces; i++) 
        {
            if (landObjects[i].activeSelf)
            {
                terrainFaces[i].ConstructMesh();
            }
        }

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }

    public override void GenerateColors()
    {
        colorGenerator.UpdateColors();
        for (int i = 0; i < numFaces; i++)
        {
            if (landMeshes[i].gameObject.activeSelf)
            {
                terrainFaces[i].UpdateUVs(colorGenerator);
            }
        }
    }
}
