using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class IcosohedralPlanet : Planetoid
{   
    public Material landMaterial;

    [SerializeField, Range(0, 4)]
    int subdivisions = 3;

    [SerializeField, Range(0, 10)]
    int numberOfContinents = 5;

    [SerializeField, Range(0, 10)]
    int numberOfHills = 5;

    [SerializeField, Range(0, 10)]
    int hillIterations = 5;

    [SerializeField, Range(0f, 2f)]
    float continentalShelfMin = 1f;

    [SerializeField, Range(0f, 2f)]
    float continentalShelfMax = 1.2f;

    [SerializeField, Range(0f, 2f)]
    float hillMin = 0.1f;

    [SerializeField, Range(0f, 2f)]
    float hillMax = 0.5f;

    [SerializeField, Range(1f, 2f)]
    float mountainFactor = 1.05f;

    [SerializeField, Range(0f, 2f)]
    float landAreaMax = 1.3f;

    [SerializeField, Range(0f, 2f)]
    float landAreaMin = 0.1f;

    GameObject oceanMesh;

    List<Polygon> polys;
    List<Vector3> verts;
    MeshFilter filter;
	MeshCollider col;
    MeshRenderer meshRenderer;

    public override void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
    }

    void Initialize()
    {
        filter = GetComponent<MeshFilter>();
        col = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material.EnableKeyword("_VERTEXCOLOR");
        colorSettings.oceanMaterial.EnableKeyword("_VERTEXCOLOR");
		filter.mesh.Clear();

        InitAsIcosohedron();
        Subdivide(subdivisions);

        CalculateNeighbors();

        Color32 colorOcean = new Color32(0,80, 220, 0);
        Color32 yellowGrass = new Color32(180, 220, 0, 0);
        Color32 colorGrass = new Color32(0, 220, 0, 0);
        Color32 colorForest = new Color32(0, 120, 50, 0);
        Color32 colorWhite = new Color32(225, 225, 225, 0);
        Color32 colorDirt = new Color32(180, 140, 20, 0);
        Color32 colorStone1 = new Color32(140, 140, 140, 0);
        Color32 colorStone2 = new Color32(200, 200, 200, 0);
        Color32 colorDeepOcean = new Color32(0, 40, 110, 0);
        Color32 colorPink = new Color32(255, 220, 220, 0);

        foreach (Polygon p in polys) 
        {
            p.color = colorOcean;
        }

        // Select land polygons

        PolySet landPolys = new PolySet();
        PolySet sides;

        for(int i = 0; i < numberOfContinents; i++)
        {
            float continentSize = Random.Range(landAreaMin, landAreaMax);
            PolySet newLand = GetPolysInSphere(Random.onUnitSphere, continentSize, polys);
            landPolys.UnionWith(newLand);
        }

        landPolys.ApplyColor(yellowGrass, colorGrass);

        // Select inverse as ocean

        var oceanPolys = new PolySet();

        foreach (Polygon poly in polys)
        {
            if (!landPolys.Contains(poly)) 
            {
                oceanPolys.Add(poly);
            }   
        }

        // Create ocean mesh
        var oceanSurface = new PolySet(oceanPolys);

        sides = Inset(oceanSurface, Mathf.Lerp(0f, 0.01f, Random.value));
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        if (oceanMesh != null)
        {
            Destroy(oceanMesh);
        }      

        oceanMesh = GenerateChildMesh("Ocean Surface", colorSettings.oceanMaterial);

        // The Extrude function will raise the land Polygons up out of the water.
        // It also generates a strip of new Polygons to connect the newly raised land
        // back down to the water level. We can color this vertical strip of land brown like dirt.

        sides = Extrude(landPolys, continentalShelfMin, continentalShelfMax);
        sides.ApplyColor(colorDirt);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        // Grab additional polygons to generate hills, but only from the set of polygons that are land.
        PolySet possibleHillPolys = landPolys.RemoveEdges();

        for (int hillCount = 0; hillCount < hillIterations; hillCount++) 
        {
            float hillSetMax = Mathf.Lerp(hillMin, hillMax, Random.value)  * (mountainFactor * hillCount);
            PolySet hillPolys = new PolySet();

            for (int i = 0; i < numberOfHills; i++)
            {
                float hillSize = Random.Range(landAreaMin, landAreaMax);
                PolySet newHills = GetPolysInSphere(Random.onUnitSphere, hillSize, possibleHillPolys);
                hillPolys.UnionWith(newHills);
            }

            hillPolys.ApplyColor(
                Color32.Lerp(colorForest, Color.white, (hillCount / hillIterations - 1) * Random.value),
                Color32.Lerp(
                    hillCount == hillIterations - 1 
                        ? colorPink
                        : colorGrass, Color.white,
                    (hillCount / hillIterations - 1) * Random.value)
                );

            sides = Inset(hillPolys, Mathf.Lerp(0f, hillSetMax, Random.value));
            sides.ApplyColor(hillIterations == 1 ? yellowGrass : colorGrass, hillIterations == 1 ? colorGrass : colorForest);
            sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

            sides = Extrude(hillPolys, hillMin, hillSetMax);
            sides.ApplyColor(colorStone1, colorStone2);
            sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

            possibleHillPolys = hillPolys.RemoveEdges();
        }

        // Time to return to the oceans.
        sides = Extrude(oceanPolys, -0.001f, -0.02f);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

        sides = Inset(oceanPolys, Mathf.Lerp(0f, 1.2f, Random.value));
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        var possibleDeepOceanPolys = oceanPolys.RemoveEdges();
        var deepOceanPolys = new PolySet();
        for (int i = 0; i < numberOfHills; i++)
        {
            float hillSize = Random.Range(landAreaMin, landAreaMax);
            PolySet newOcean = GetPolysInSphere(Random.onUnitSphere, hillSize, possibleDeepOceanPolys);
            deepOceanPolys.UnionWith(newOcean);
        }

        sides = Extrude(deepOceanPolys, -0.02f, -0.04f);
        sides.ApplyColor(colorDeepOcean);

        deepOceanPolys.ApplyColor(colorDeepOcean);
    }

    void InitAsIcosohedron()
    {
        polys = new List<Polygon>();
        verts = new List<Vector3>();

        float t = (1.0f + Mathf.Sqrt (5.0f)) / 2.0f;

        verts.Add (new Vector3 (-1, t, 0).normalized);
        verts.Add (new Vector3 (1, t, 0).normalized);
        verts.Add (new Vector3 (-1, -t, 0).normalized);
        verts.Add (new Vector3 (1, -t, 0).normalized);
        verts.Add (new Vector3 (0, -1, t).normalized);
        verts.Add (new Vector3 (0, 1, t).normalized);
        verts.Add (new Vector3 (0, -1, -t).normalized);
        verts.Add (new Vector3 (0, 1, -t).normalized);
        verts.Add (new Vector3 (t, 0, -1).normalized);
        verts.Add (new Vector3 (t, 0, 1).normalized);
        verts.Add (new Vector3 (-t, 0, -1).normalized);
        verts.Add (new Vector3 (-t, 0, 1).normalized);

        polys.Add (new Polygon(0, 11, 5));
        polys.Add (new Polygon(0, 5, 1));
        polys.Add (new Polygon(0, 1, 7));
        polys.Add (new Polygon(0, 7, 10));
        polys.Add (new Polygon(0, 10, 11));
        polys.Add (new Polygon(1, 5, 9));
        polys.Add (new Polygon(5, 11, 4));
        polys.Add (new Polygon(11, 10, 2));
        polys.Add (new Polygon(10, 7, 6));
        polys.Add (new Polygon(7, 1, 8));
        polys.Add (new Polygon(3, 9, 4));
        polys.Add (new Polygon(3, 4, 2));
        polys.Add (new Polygon(3, 2, 6));
        polys.Add (new Polygon(3, 6, 8));
        polys.Add (new Polygon(3, 8, 9));
        polys.Add (new Polygon(4, 9, 5));
        polys.Add (new Polygon(2, 4, 11));
        polys.Add (new Polygon(6, 2, 10));
        polys.Add (new Polygon(8, 6, 7));
        polys.Add (new Polygon(9, 8, 1));
    }

    public void Subdivide(int recursions)
    {
        var midPointCache = new Dictionary<int, int> ();

        for (int i = 0; i < recursions; i++)
        {
            var newPolys = new List<Polygon>();
            foreach (var poly in polys)
            {
                int a = poly.verts [0];
                int b = poly.verts [1];
                int c = poly.verts [2];

                // Use GetMidPointIndex to either create a
                // new vertex between two old vertices, or
                // find the one that was already created.
                int ab = GetMidPointIndex(midPointCache, a, b);
                int bc = GetMidPointIndex(midPointCache, b, c);
                int ca = GetMidPointIndex(midPointCache, c, a);

                // Create the four new polygons using our original
                // three vertices, and the three new midpoints.
                newPolys.Add(new Polygon(a, ab, ca));
                newPolys.Add(new Polygon(b, bc, ab));
                newPolys.Add(new Polygon(c, ca, bc));
                newPolys.Add(new Polygon(ab, bc, ca));
            }

            // Replace all our old polygons with the new set of
            // subdivided ones.
            polys = newPolys;
        }
    }

    public int GetMidPointIndex (Dictionary<int, int> cache, int indexA, int indexB)
    {
        // We create a key out of the two original indices
        // by storing the smaller index in the upper two bytes
        // of an integer, and the larger index in the lower two
        // bytes. By sorting them according to whichever is smaller
        // we ensure that this function returns the same result
        // whether you call
        // GetMidPointIndex(cache, 5, 9)
        // or...
        // GetMidPointIndex(cache, 9, 5)
        int smallerIndex = Mathf.Min (indexA, indexB);
        int greaterIndex = Mathf.Max (indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;
    
        // If a midpoint is already defined, just return it.
        if (cache.TryGetValue(key, out int ret))
        {
            return ret;
        }
            
        // If we're here, it's because a midpoint for these two
        // vertices hasn't been created yet. Let's do that now!
        Vector3 p1 = verts[indexA];
        Vector3 p2 = verts[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = verts.Count;
        verts.Add(middle);

        cache.Add(key, ret);
        return ret;
    }

    public void CalculateNeighbors()
    {
         foreach (Polygon poly in polys)
         { 
             foreach (Polygon other_poly in polys)
             { 
                 if (poly == other_poly)
                     continue;
 
                 if (poly.IsNeighborOf (other_poly))
                     poly.neighbors.Add(other_poly);
             }
         }
    }

    public PolySet GetPolysInSphere(Vector3 center, float radius, IEnumerable<Polygon> source)
    {
        PolySet newSet = new PolySet();
        foreach(Polygon p in source)
        {
            foreach(int vertexIndex in p.verts)
            {
                float distanceToSphere = Vector3.Distance(center, verts[vertexIndex]);
        
                if (distanceToSphere <= radius)
                {
                    newSet.Add(p);
                    break;
                }
            }
        }
        return newSet;
    }

    public PolySet Extrude(PolySet polyset, float minHeight, float maxHeight)
    {
        PolySet stitchedPolys = StitchPolys(polyset);
        List<int> uniqueVerts = polyset.GetUniqueVertices();

        // Take each vertex in this list of polys, and push it
        // away from the center of the Planet by the height
        // parameter.
        foreach(int vert in uniqueVerts)
        {
            Vector3 v = verts[vert];
            v = v.normalized * (v.magnitude + Mathf.Lerp(minHeight, maxHeight, Random.value));
            verts[vert] = v;
        }
        return stitchedPolys;
    }

    public PolySet Inset(PolySet polys, float interpolation)
    {
        PolySet stitchedPolys = StitchPolys(polys);
        List<int> uniqueVerts = polys.GetUniqueVertices();
    
        // Calculate the average center of all the vertices in these Polygons.
        Vector3 center = Vector3.zero;
        foreach (int vert in uniqueVerts)
        {
            center += verts[vert];
        }
        center /= uniqueVerts.Count;
    
        // Pull each vertex towards the center, then correct
        // it's height so that it's as far from the center of
        // the planet as it was before.
        foreach (int vert in uniqueVerts)
        {
            Vector3 v = verts[vert];
            float height = v.magnitude;
            v = Vector3.Lerp(v, center, interpolation);
            v = v.normalized * height;
            verts[vert] = v;
        }

        return stitchedPolys;
    }

    public List<int> CloneVertices(List<int> oldVerts)
    {
        List<int> newVerts = new List<int>();
        foreach(int oldVert in oldVerts) 
        {
            Vector3 cloned_vert = verts[oldVert];
            newVerts.Add(verts.Count);
            verts.Add(cloned_vert);
        }
        return newVerts;
    }

    public PolySet StitchPolys(PolySet polyset)
    {
        PolySet stichedPolys = new PolySet();
        var edgeSet = polyset.CreateEdgeSet();
        var originalVerts = edgeSet.GetUniqueVertices();
        var newVerts = CloneVertices(originalVerts);
        edgeSet.Split(originalVerts, newVerts);

        foreach (Edge edge in edgeSet)
        {
            // Create new polys along the stitched edge
            var stitch_poly1 = new Polygon(
                edge.outerVerts[0],
                edge.outerVerts[1],
                edge.innerVerts[0]
            );
            var stitch_poly2 = new Polygon(
                edge.outerVerts[1],
                edge.innerVerts[1],
                edge.innerVerts[0]
            );

            // Add the new stitched faces as neighbors to the originals
            edge.innerPoly.ReplaceNeighbor(edge.outerPoly, stitch_poly2);
            edge.outerPoly.ReplaceNeighbor(edge.innerPoly, stitch_poly1);

            polys.Add(stitch_poly1);
            polys.Add(stitch_poly2);
        
            stichedPolys.Add(stitch_poly1);
            stichedPolys.Add(stitch_poly2);
        }
    
        // Swap to the new vertices on the inner polys
        foreach (Polygon poly in polyset)
        {
            for (int i = 0; i < 3; i++)
            {
                int vert_id = poly.verts[i];
                if (!originalVerts.Contains(vert_id))
                {
                    continue;
                }
                    
                int vert_index = originalVerts.IndexOf(vert_id);
                poly.verts[i] = newVerts[vert_index];
            }
        }

        return stichedPolys;
    }

    public GameObject GenerateChildMesh(string name, Material material)
    {
        GameObject meshObject = new GameObject(name);
        meshObject.transform.parent = transform;
        meshObject.layer = LayerMask.NameToLayer("Water");

        MeshRenderer surfaceRenderer = meshObject.AddComponent<MeshRenderer>();
        surfaceRenderer.material = material;

        MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        meshCollider.isTrigger = true;

        Mesh terrainMesh = CreateMesh();

        MeshFilter terrainFilter = meshObject.AddComponent<MeshFilter>();
        terrainFilter.mesh = terrainMesh;

        meshCollider.sharedMesh = terrainMesh;

        meshObject.transform.localScale = new Vector3(1f, 1f, 1f);
        meshObject.transform.localPosition = Vector3.zero;

        return meshObject;
    }

    public override void GenerateMesh()
    {
        Mesh terrainMesh = CreateMesh();
        filter.mesh = terrainMesh;
        col.sharedMesh = terrainMesh;
    }

    Mesh CreateMesh()
    {
        Mesh terrainMesh = new Mesh();

        int vertexCount = polys.Count * 3;

        int[] indices = new int[vertexCount];

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals  = new Vector3[vertexCount];
        Color32[] colors   = new Color32[vertexCount];
        Vector2[] uvs      = new Vector2[vertexCount];

        for (int i = 0; i < polys.Count; i++)
        {
            var poly = polys[i];

            indices[i * 3 + 0] = i * 3 + 0;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;

            vertices[i * 3 + 0] = verts[poly.verts[0]];
            vertices[i * 3 + 1] = verts[poly.verts[1]];
            vertices[i * 3 + 2] = verts[poly.verts[2]];

            uvs[i * 3 + 0] = poly.uvs[0];
            uvs[i * 3 + 1] = poly.uvs[1];
            uvs[i * 3 + 2] = poly.uvs[2];

            colors[i * 3 + 0] = poly.color;
            colors[i * 3 + 1] = poly.color;
            colors[i * 3 + 2] = poly.color;

            if (poly.smoothNormals)
            {
                normals[i * 3 + 0] = verts[poly.verts[0]].normalized;
                normals[i * 3 + 1] = verts[poly.verts[1]].normalized;
                normals[i * 3 + 2] = verts[poly.verts[2]].normalized;
            }
            else
            {
                Vector3 ab = verts[poly.verts[1]] - verts[poly.verts[0]];
                Vector3 ac = verts[poly.verts[2]] - verts[poly.verts[0]];

                Vector3 normal = Vector3.Cross(ab, ac).normalized;

                normals[i * 3 + 0] = normal;
                normals[i * 3 + 1] = normal;
                normals[i * 3 + 2] = normal;
            }
        }

        terrainMesh.vertices = vertices;
        terrainMesh.normals  = normals;
        terrainMesh.colors32 = colors;
        terrainMesh.uv = uvs;

        terrainMesh.SetTriangles(indices, 0);

        return terrainMesh;
    }
}



