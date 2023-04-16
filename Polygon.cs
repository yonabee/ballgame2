using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Polygon
{
    public List<int> verts;
    public List<Vector2> uvs;
    public List<Polygon> neighbors;
    public Color32 color;
    public bool smoothNormals;
    public Polygon(int a, int b, int c)
    {
        verts = new List<int>() { a, b, c };
        neighbors = new List<Polygon>();
        uvs = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero };
        smoothNormals = true;
        color = new Color32(255, 0, 255, 255);
    }

    public bool IsNeighborOf(Polygon otherPoly)
    {
        int sharedVertices = 0;
        foreach (int vertex in verts)
        {
        if (otherPoly.verts.Contains(vertex))
            sharedVertices++;
        }

        return sharedVertices == 2;
    }

    public void ReplaceNeighbor(Polygon oldNeighbor, Polygon newNeighbor)
    {
        for (int i = 0; i < neighbors.Count; i++)
        {
            if (oldNeighbor == neighbors[i])
            {
                neighbors[i] = newNeighbor;
                return;
            }
        }
    }
}

public class PolySet : HashSet<Polygon>
{
    public PolySet() {}
    public PolySet(PolySet source) : base(source) {}

    // Store the index of the last original vertex before we did the stitching.
    public int stitchedVertexThreshold = -1;

    public EdgeSet CreateEdgeSet()
    {
        EdgeSet edgeSet = new EdgeSet();
        foreach (Polygon poly in this)
        {
        foreach (Polygon neighbor in poly.neighbors)
        {
            if (this.Contains(neighbor)) 
            {
                continue;
            }
            
            Edge edge = new Edge(poly, neighbor);
            edgeSet.Add(edge);
        }
        }
        return edgeSet;
    }

    // RemoveEdges - Remove any poly from this set that borders the edge of the set, including those that just
    // touch the edge with a single vertex. The PolySet could be empty after this operation.

    public PolySet RemoveEdges()
    {
        var newSet = new PolySet();

        var edgeSet = CreateEdgeSet();

        var edgeVertices = edgeSet.GetUniqueVertices();

        foreach(Polygon poly in this)
        {
            bool polyTouchesEdge = false;

            for(int i = 0; i < 3; i++)
            {
                if(edgeVertices.Contains(poly.verts[i]))
                {
                    polyTouchesEdge = true;
                    break;
                }
            }

            if (polyTouchesEdge)
                continue;

            newSet.Add(poly);
        }

        return newSet;
    }

    public List<int> GetUniqueVertices()
    {
        List<int> verts = new List<int>();
        foreach (Polygon poly in this)
        {
        foreach (int vert in poly.verts)
        {
            if (!verts.Contains(vert))
            verts.Add(vert);
        }
        }
        return verts;
    }

    public void ApplyAmbientOcclusionTerm(float AOForOriginalVerts, float AOForNewVerts)
    {
        foreach(Polygon poly in this)
        {
            for (int i = 0; i < 3; i++)
            {
                float ambientOcclusionTerm = (poly.verts[i] > stitchedVertexThreshold) ? AOForNewVerts : AOForOriginalVerts;

                Vector2 uv = poly.uvs[i];
                uv.y = ambientOcclusionTerm;
                poly.uvs[i] = uv;
            }
        }
    }

    public void ApplyColor(Color32 c)
    {
        foreach (Polygon poly in this)
        {
            poly.color = c;
        }
            
    }

    public void ApplyColor(Color32 c1, Color32 c2)
    {
        foreach (Polygon poly in this)
        {
            poly.color = Color32.Lerp(c1, c2, Random.value);
        }     
    }
}
