using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UPGMeshPool {
    // settings
    public UPGSettings settings;

    // internal
    private Dictionary<Tuple<int, int>, Mesh> meshes = new Dictionary<Tuple<int, int>, Mesh>();

    public void generateMesh(Tuple<int, int> id) {
        int sl = settings.chunkSize;
        Vector3[] verts = new Vector3[sl * sl];
        Vector2[] uvs = new Vector2[sl * sl];
        int[] tris = new int[(sl - 1) * (sl - 1) * 6];

        int vertIndex = 0;
        int chunkSize = sl;
        int triIndex = 0;
        for (int z = 0; z < chunkSize; z++) {
            for (int x = 0; x < chunkSize; x++) {
                int cox = (chunkSize * id.Item1) + x;
                int coz = (chunkSize * id.Item2) + z;

                verts[vertIndex] = new Vector3(x, 0, z);
                uvs[vertIndex] = new Vector2(x / (float)chunkSize, z / (float)chunkSize);

                if (x < chunkSize - 1 && z < chunkSize - 1) {
                    tris[triIndex] = vertIndex + chunkSize;
                    tris[triIndex + 1] = vertIndex + chunkSize + 1;
                    tris[triIndex + 2] = vertIndex;
                    triIndex += 3;

                    tris[triIndex] = vertIndex + 1;
                    tris[triIndex + 1] = vertIndex;
                    tris[triIndex + 2] = vertIndex + chunkSize + 1;
                    triIndex += 3;
                }

                vertIndex++;
            }
        }

        Mesh m = new Mesh();
        m.vertices = verts;
        Array.Reverse(tris);
        m.triangles = tris;
        m.uv = uvs;
        m.RecalculateNormals();
        meshes[id] = m;
    }

    public Mesh getMeshInstance(Tuple<int, int> id) {
        return meshes[id];
    }
}