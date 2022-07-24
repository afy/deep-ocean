using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;



public class UnityProcGen : MonoBehaviour {
    // unity
    public UPGSettings upgSettings;
    public UPGMeshPool meshPool;
    public GameObject chunkStencil;
    public GameObject target;

    // internal
    private Dictionary<(int cx, int cz), UPGChunkData> chunks = new Dictionary<(int cx, int cz), UPGChunkData>();
    private (int cx, int cz) currentChunk;


    private void Start() {
        currentChunk = UPGTools.getChunkCoord(target.transform.position, upgSettings);
        meshPool = new UPGMeshPool() {
            settings = upgSettings,
        };
        var ret = meshPool.createInitialMeshes(chunkStencil);
        foreach (var e in ret) {
            createChunk((e.Item1.x, e.Item1.z), e.lod);
        }
    }

    private void Update() {
        var c = UPGTools.getChunkCoord(target.transform.position, upgSettings);
        if (c == currentChunk) { return; }

        updateChunksOnMove(c);
        currentChunk = c;
    }

    private void createChunk((int cx, int cz) id, UPGLOD lod) {
        UPGChunkData chunk = new UPGChunkData();
        chunk.verts = new Dictionary<UPGLOD, Vector3[]>();
        chunk.id = id;

        int cinc = ((int)lod == 0) ? 1 : (int)lod * 2;
        int cverts = (upgSettings.chunkSize - 1) / cinc + 1;
        var result = new NativeArray<Vector3>(cverts * cverts, Allocator.Persistent);
        var job = new UPGHeightJob() {
            settings = upgSettings,
            result = result,
            cx = id.cx,
            cz = id.cz,
            lod = (int)lod
        };

        var handle = job.Schedule(cverts * cverts, cverts * cverts);
        handle.Complete();

        GameObject go = meshPool.getNewMeshInstance(id, lod);
        go.name = "chunk (" + id.cx.ToString() + ", " + id.cz.ToString() + ")";
        go.transform.position = new Vector3(id.cx * (upgSettings.chunkSize-1), 0, id.cz * (upgSettings.chunkSize-1));
        Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
        m.SetVertices(job.result);
        m.RecalculateNormals();
        chunk.verts[lod] = m.vertices;
        go.SetActive(true);
        result.Dispose();
    }

    private void updateChunksOnMove((int cx, int cz) c) {
        int sx = UPGTools.returnChunkChangeDir(c.cx, currentChunk.cx);
        int sz = UPGTools.returnChunkChangeDir(c.cz, currentChunk.cz);
        var oldchunks = new List<(int cx, int cz, UPGLOD lod)>();
        var newchunks = new List<(int cx, int cz, UPGLOD lod)>();

        // ...

        foreach (var oc in oldchunks) {
            meshPool.freeMesh((oc.cx, oc.cz), oc.lod);
        }
        foreach (var nc in oldchunks) {
            // ...
        }
    }
}
