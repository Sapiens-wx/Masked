using UnityEngine;
using System.Collections.Generic;

public static class MeshUtils
{
    const float EPS = 1e-10f;

    public static void BuildConnectedMesh(
        Mesh mesh,
        out List<Vector3> vertices,
        out List<int[]> triangles,
        out Dictionary<int, List<int>> logicalToMeshMap)
    {
        vertices = new();
        triangles = new();
        logicalToMeshMap = new();

        Vector3[] meshVerts = mesh.vertices;
        int[] meshTris = mesh.triangles;

        int[] meshToLogical = new int[meshVerts.Length];

        for (int i = 0; i < meshVerts.Length; i++)
        {
            int logical = FindOrAdd(meshVerts[i], vertices);
            meshToLogical[i] = logical;

            if (!logicalToMeshMap.ContainsKey(logical))
                logicalToMeshMap[logical] = new List<int>();

            logicalToMeshMap[logical].Add(i);
        }

        for (int i = 0; i < meshTris.Length; i += 3)
        {
            triangles.Add(new int[]
            {
                meshToLogical[meshTris[i]],
                meshToLogical[meshTris[i+1]],
                meshToLogical[meshTris[i+2]]
            });
        }
    }

    static int FindOrAdd(Vector3 v, List<Vector3> verts)
    {
        for (int i = 0; i < verts.Count; i++)
        {
            if ((verts[i] - v).sqrMagnitude < EPS)
                return i;
        }

        verts.Add(v);
        return verts.Count - 1;
    }
}