using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UPGMeshPool {
    // settings
    public UPGSettings settings;

    // internal
    public bool initialCreated = false;
    public Dictionary<UPGLOD, List<GameObject>> availableMeshes = new Dictionary<UPGLOD, List<GameObject>>();
    private Dictionary<UPGLOD, Dictionary<(int cx, int cz), GameObject>> activeMeshes = 
        new Dictionary<UPGLOD, Dictionary<(int cx, int cz), GameObject>>();

    public List<((int x, int z), UPGLOD lod)> createInitialMeshes(GameObject chunkStencil) {
        if (initialCreated) { return null; }
        if (settings.renderDst <= settings.farLimit) { throw new Exception("Render distance has to be > "+settings.farLimit.ToString()); }

        var ret = new List<((int x, int z), UPGLOD lod)>();
        for (int i = -settings.renderDst; i < settings.renderDst + 1; i++) {
            for (int j = -settings.renderDst; j < settings.renderDst + 1; j++) {

                if (i < -settings.closeLimit || i > settings.closeLimit
                    || j < -settings.closeLimit || j > settings.closeLimit) {

                    if (i < -settings.mediumLimit || i > settings.mediumLimit
                        || j < -settings.mediumLimit || j > settings.mediumLimit) {

                        if (i < -settings.farLimit || i > settings.farLimit
                            || j < -settings.farLimit || j > settings.farLimit) {
                            ret.Add(((i, j), UPGLOD.Distant));
                        }

                        else {
                            ret.Add(((i, j), UPGLOD.Far));
                        }

                    }

                    else {
                        ret.Add(((i, j), UPGLOD.Medium));
                    }
                }
                else {
                    ret.Add(((i, j), UPGLOD.Close));
                }
            }
        }
        foreach (var ch in ret) {
            generateMesh(ch.lod, chunkStencil);
        }

        initialCreated = true;
        return ret;
    }

    public void generateMesh(UPGLOD lod, GameObject chunkStencil) {
        int cinc = ((int)lod == 0) ? 1 : (int)lod * 2;
        int cverts = (settings.chunkSize - 1) / cinc + 1;

        Vector3[] verts = new Vector3[cverts * cverts];
        Vector2[] uvs = new Vector2[cverts * cverts];
        int[] tris = new int[(cverts-1) * (cverts-1) * 6];

        int vertIndex = 0;
        int triIndex = 0; 
        for (int z = 0; z < settings.chunkSize; z += cinc) {
            for (int x = 0; x < settings.chunkSize; x += cinc) {
                verts[vertIndex] = new Vector3(x, 0, z);
                uvs[vertIndex] = new Vector2(x / (float)settings.chunkSize, z / (float)settings.chunkSize);

                if (x < settings.chunkSize-cinc && z < settings.chunkSize-cinc) {
                    tris[triIndex] = vertIndex + cverts;
                    tris[triIndex + 1] = vertIndex + cverts + 1;
                    tris[triIndex + 2] = vertIndex;
                    triIndex += 3;

                    tris[triIndex] = vertIndex + 1;
                    tris[triIndex + 1] = vertIndex;
                    tris[triIndex + 2] = vertIndex + cverts + 1;
                    triIndex += 3;
                }

                vertIndex++;
            }
        }

        Mesh m = new Mesh();
        m.vertices = verts;
        m.triangles = tris;
        m.uv = uvs;
        m.RecalculateNormals();
        
        GameObject go = GameObject.Instantiate(chunkStencil);
        go.SetActive(false);
        go.transform.position = Vector3.zero;
        go.GetComponent<MeshFilter>().sharedMesh = m;
        availableMeshes[lod].Add(go);

        switch (lod) {
            case UPGLOD.Far:
                go.GetComponent<MeshRenderer>().material.color = Color.red;
                break;

            case UPGLOD.Close:
                go.GetComponent<MeshRenderer>().material.color = Color.green;
                break;

            case UPGLOD.Medium:
                go.GetComponent<MeshRenderer>().material.color = Color.yellow;
                break;

            default: break;
        }
    }

    public GameObject getNewMeshInstance((int cx, int cz) id, UPGLOD lod) {
        if (availableMeshes.Count > 0) {
            GameObject go = availableMeshes[lod][0];
            availableMeshes[lod].RemoveAt(0);
            activeMeshes[lod][id] = go;
            return go;
        }
        return null;
    }

    public void freeMesh((int cx, int cz) id, UPGLOD lod) {
        GameObject go = activeMeshes[lod][id];
        activeMeshes[lod].Remove(id);
        go.SetActive(false);
        availableMeshes[lod].Add(go);
    }
}