using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An Edge is a boundary between two Polygons. We're going to be working with loops of Edges, so
// each Edge will have a Polygon that's inside the loop and a Polygon that's outside the loop.
// We also want to Split apart the inner and outer Polygons so that they no longer share the same
// vertices. This means the Edge will need to keep track of what the outer Polygon's vertices are
// along its border with the inner Polygon, and what the inner Polygon's vertices are for that
// same border.

public class Edge
{
    public Polygon innerPoly; //The Poly that's inside the Edge. The one we'll be extruding or insetting.
    public Polygon outerPoly; //The Poly that's outside the Edge. We'll be leaving this one alone.
    public List<int> outerVerts; //The vertices along this edge, according to the Outer poly.
    public List<int> innerVerts; //The vertices along this edge, according to the Inner poly.
    public int inwardDirectionVertex; //The third vertex of the inner polygon. That is, the one that doesn't touch this edge.

    public Edge(Polygon innerPoly, Polygon outerPoly)
    {
        this.innerPoly  = innerPoly;
        this.outerPoly  = outerPoly;
        outerVerts = new List<int>(2);
        innerVerts = new List<int>(2);

        foreach (int vertex in innerPoly.verts)
        {
            if (outerPoly.verts.Contains(vertex))
            {
                innerVerts.Add(vertex);
            }
            else 
            {
                inwardDirectionVertex = vertex;
            }
                
        }

        // For consistency, we want the 'winding order' of the edge to be the same as that of the inner
        // polygon. So the vertices in the edge are stored in the same order that you would encounter them if
        // you were walking clockwise around the polygon. That means the pair of edge vertices will be:
        // [1st inner poly vertex, 2nd inner poly vertex] or
        // [2nd inner poly vertex, 3rd inner poly vertex] or
        // [3rd inner poly vertex, 1st inner poly vertex]
        //
        // The formula above will give us [1st inner poly vertex, 3rd inner poly vertex] though, so
        // we check for that situation and reverse the vertices.

        if(innerVerts[0] == innerPoly.verts[0] && innerVerts[1] == innerPoly.verts[2])
        {
            int temp = innerVerts[0];
            innerVerts[0] = innerVerts[1];
            innerVerts[1] = temp;
        }

        // No manipulations have happened yet, so the outer and inner Polygons still share the same vertices.
        // We can instantiate m_OuterVerts as a copy of m_InnerVerts.

        outerVerts = new List<int>(innerVerts);
    }
}

// EdgeSet is a collection of unique edges. Basically it's a HashSet, but we have
// extra convenience functions that we'd like to include in it.

public class EdgeSet : HashSet<Edge>
{
    // Split - Given a list of original vertex indices and a list of replacements,
    //         update m_InnerVerts to use the new replacement vertices.

    public void Split(List<int> oldVertices, List<int> newVertices)
    {
        foreach(Edge edge in this)
        {
            for(int i = 0; i < 2; i++)
            {
                edge.innerVerts[i] = newVertices[oldVertices.IndexOf(edge.outerVerts[i])];
            }
        }
    }

    // GetUniqueVertices - Get a list of all the vertices referenced
    // in this edge loop, with no duplicates.

    public List<int> GetUniqueVertices()
    {
        List<int> vertices = new List<int>();

        foreach (Edge edge in this)
        {
            foreach (int vert in edge.outerVerts)
            {
                if (!vertices.Contains(vert)) 
                {
                    vertices.Add(vert);
                }       
            }
        }
        return vertices;
    }

    // GetInwardDirections - For each vertex on this edge, calculate the direction that
    // points most deeply inwards. That's the average of the inward direction of each edge
    // that the vertex appears on.

    public Dictionary<int, Vector3> GetInwardDirections(List<Vector3> vertexPositions)
    {
        var inwardDirections = new Dictionary<int, Vector3>();
        var numContributions = new Dictionary<int, int>();

        foreach(Edge edge in this)
        {
            Vector3 innerVertexPosition = vertexPositions[edge.inwardDirectionVertex];

            Vector3 edgePosA   = vertexPositions[edge.innerVerts[0]];
            Vector3 edgePosB   = vertexPositions[edge.innerVerts[1]];
            Vector3 edgeCenter = Vector3.Lerp(edgePosA, edgePosB, 0.5f);

            Vector3 innerVector = (innerVertexPosition - edgeCenter).normalized;

            for(int i = 0; i < 2; i++)
            {
                int edgeVertex = edge.innerVerts[i];

                if (inwardDirections.ContainsKey(edgeVertex))
                {
                    inwardDirections[edgeVertex] += innerVector;
                    numContributions[edgeVertex]++;
                }
                else
                {
                    inwardDirections.Add(edgeVertex, innerVector);
                    numContributions.Add(edgeVertex, 1);
                }
            }
        }

        // Now we average the contributions that each vertex received, and we can return the result.

        foreach(KeyValuePair<int, int> kvp in numContributions)
        {
            int vertexIndex               = kvp.Key;
            int contributionsToThisVertex = kvp.Value;
            inwardDirections[vertexIndex] = (inwardDirections[vertexIndex] / contributionsToThisVertex).normalized;
        }

        return inwardDirections;
    }
}