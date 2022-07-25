using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;



/*
    TODO

-Fix bug where moving on x axis after z-axis movement will fuck up chunk loading
  moving too fast will also cause this bug
-Make a "chunk buffer" to limit storage taken up by the chunk list in UnityProcGen (capped FIFO with overflow?)
-Add support for objects in separate chunk-related storage from heightpoints (easily recalculated, but objects are persistent).
  maybe a database?
-General optimization ideas:
    -Try using "half" class for points instead of float
    -Review the sketchy NativeArray -> Vector3[] in calculateChunkHeights function
    -Multithreaded loading (if not already) 
    -Review LOD levels for "Close" (possible save on verteces)
*/



public class UnityProcGen : MonoBehaviour {
    // unity
    public UPGSettings upgSettings;
    public UPGMeshPool meshPool;
    public GameObject chunkStencil;
    public GameObject target;
    public bool debugOverrideHeight;
    public Text debugTextChunks;

    // internal
    private Dictionary<(int cx, int cz), UPGChunkData> chunks = new Dictionary<(int cx, int cz), UPGChunkData>();
    private (int cx, int cz) currentChunk;
    private int debugChunkLoadedSize;

    private void Start() {
        currentChunk = UPGTools.getChunkCoord(target.transform.position, upgSettings);
        meshPool = new UPGMeshPool() {
            settings = upgSettings,
        };
        var ret = meshPool.createInitialMeshes(chunkStencil);
        foreach (var e in ret) {
            loadChunk((e.Item1.x, e.Item1.z), e.lod);
        }
        float y = target.transform.position.y;
        target.transform.position = new Vector3((currentChunk.cx + 0.5f) * upgSettings.chunkSize, y, (currentChunk.cz + 0.5f) * upgSettings.chunkSize);
    }

    private void Update() {
        debugTextChunks.text = "#" + chunks.Count + " - " + debugChunkLoadedSize;

        var c = UPGTools.getChunkCoord(target.transform.position, upgSettings);
        if (c == currentChunk) { return; }

        updateChunksOnMove(c);
    }

    private void loadChunk((int cx, int cz) id, UPGLOD lod) {
        if (!chunks.ContainsKey(id)) {
            createChunk(id);
        }
        if (!chunks[id].verts.ContainsKey(lod)) {
            calculateChunkHeights(id, lod);
        }

        setChunk(id, lod);
    }

    private void createChunk((int cx, int cz) id) {
        UPGChunkData chunk = new UPGChunkData();
        chunk.verts = new Dictionary<UPGLOD, Vector3[]>();
        chunk.id = id;
        chunks[id] = chunk;
    }

    private void calculateChunkHeights((int cx, int cz) id, UPGLOD lod) {
        if (chunks[id].verts.ContainsKey(lod)) { throw new Exception("values already exist for this chunk's lod"); }

        int cinc = ((int)lod == 0) ? 1 : (int)lod * 2;
        int cverts = (upgSettings.chunkSize - 1) / cinc + 1;
        var result = new NativeArray<Vector3>(cverts * cverts, Allocator.Persistent);
        var job = new UPGHeightJob() {
            settings = upgSettings,
            result = result,
            cx = id.cx,
            cz = id.cz,
            cinc = cinc,
            cverts = cverts,
            debugOH = debugOverrideHeight
        };
        var handle = job.Schedule(cverts * cverts, cverts * cverts);
        handle.Complete();

        Mesh m = new Mesh();
        m.SetVertices(job.result);
        chunks[id].verts[lod] = m.vertices;
        debugChunkLoadedSize += 1;
        result.Dispose();
    }

    private void setChunk((int cx, int cz) id, UPGLOD lod) {
        if (meshPool.getFreeMeshCount(lod) <= 0) { throw new Exception("No meshes in available pool"); }
        if (!chunks.ContainsKey(id)) { throw new Exception("No chunk of that id has been created"); }

        GameObject go = meshPool.getNewMeshInstance(id, lod);
        go.name = "chunk (" + id.cx.ToString() + ", " + id.cz.ToString() + ")";
        float y = go.transform.position.y;
        go.transform.position = new Vector3(id.cx * (upgSettings.chunkSize - 1), y, id.cz * (upgSettings.chunkSize - 1));
        Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
        m.SetVertices(chunks[id].verts[lod]);
        m.RecalculateNormals();
        go.SetActive(true);
    }

    private void updateChunksOnMove((int cx, int cz) cn) {
        int sx = UPGTools.returnChunkChangeDir(cn.cx, currentChunk.cx);
        int sz = UPGTools.returnChunkChangeDir(cn.cz, currentChunk.cz);
        var oldchunks = new HashSet<(int cx, int cz, UPGLOD lod)>();
        var newchunks = new HashSet<(int cx, int cz, UPGLOD lod)>();
        var c = currentChunk;
        currentChunk = cn;

        Debug.Log(c);
        for (int i = -upgSettings.renderDst; i <= upgSettings.renderDst; i++) {
            if (sx != 0) {
                if (i >= -upgSettings.closeLimit && i <= upgSettings.closeLimit) {
                    newchunks.Add((c.cx + (sx * (upgSettings.closeLimit + 1)), c.cz + i, UPGLOD.Close));
                    oldchunks.Add((c.cx + (sx * (upgSettings.closeLimit + 1)), c.cz + i, UPGLOD.Medium));
                    newchunks.Add((c.cx - (sx * (upgSettings.closeLimit)), c.cz + i, UPGLOD.Medium));
                    oldchunks.Add((c.cx - (sx * (upgSettings.closeLimit)), c.cz + i, UPGLOD.Close));
                }
                if (i >= -upgSettings.mediumLimit && i <= upgSettings.mediumLimit) {
                    newchunks.Add((c.cx + (sx * (upgSettings.mediumLimit + 1)), c.cz + i, UPGLOD.Medium));
                    oldchunks.Add((c.cx + (sx * (upgSettings.mediumLimit + 1)), c.cz + i, UPGLOD.Far));
                    newchunks.Add((c.cx - (sx * (upgSettings.mediumLimit)), i, UPGLOD.Far));
                    oldchunks.Add((c.cx - (sx * (upgSettings.mediumLimit)), i, UPGLOD.Medium));
                }
                if (i >= -upgSettings.farLimit && i <= upgSettings.farLimit) {
                    newchunks.Add((c.cx + (sx * (upgSettings.farLimit + 1)), c.cz + i, UPGLOD.Far));
                    oldchunks.Add((c.cx + (sx * (upgSettings.farLimit + 1)), c.cz + i, UPGLOD.Distant));
                    newchunks.Add((c.cx - (sx * (upgSettings.farLimit)), c.cz + i, UPGLOD.Distant));
                    oldchunks.Add((c.cx - (sx * (upgSettings.farLimit)), c.cz + i, UPGLOD.Far));
                }
                newchunks.Add((c.cx + (sx * (upgSettings.renderDst + 1)), c.cz + i, UPGLOD.Distant));
                oldchunks.Add((c.cx - (sx * (upgSettings.renderDst)), c.cz + i, UPGLOD.Distant));
            }

            if (sz != 0) {
                if (i >= -upgSettings.closeLimit && i <= upgSettings.closeLimit) {
                    newchunks.Add((c.cx + i, c.cz + (sz * (upgSettings.closeLimit + 1)), UPGLOD.Close));
                    oldchunks.Add((c.cx + i, c.cz + (sz * (upgSettings.closeLimit + 1)), UPGLOD.Medium));
                    newchunks.Add((c.cx + i, c.cz - (sz * (upgSettings.closeLimit)), UPGLOD.Medium));
                    oldchunks.Add((c.cx + i, c.cz - (sz * (upgSettings.closeLimit)) ,UPGLOD.Close));
                }
                if (i >= -upgSettings.mediumLimit && i <= upgSettings.mediumLimit) {
                    newchunks.Add((c.cx + i, c.cz + (sz * (upgSettings.mediumLimit + 1)), UPGLOD.Medium));
                    oldchunks.Add((c.cx + i, c.cz + (sz * (upgSettings.mediumLimit + 1)), UPGLOD.Far));
                    newchunks.Add((c.cx + i, c.cz - (sz * (upgSettings.mediumLimit)), UPGLOD.Far));
                    oldchunks.Add((c.cx + i, c.cz - (sz * (upgSettings.mediumLimit)), UPGLOD.Medium));
                }
                if (i >= -upgSettings.farLimit && i <= upgSettings.farLimit) {
                    newchunks.Add((c.cx + i, c.cz + (sz * (upgSettings.farLimit + 1)), UPGLOD.Far));
                    oldchunks.Add((c.cx + i, c.cz + (sz * (upgSettings.farLimit + 1)), UPGLOD.Distant));
                    newchunks.Add((c.cx + i, c.cz - (sz * (upgSettings.farLimit)), UPGLOD.Distant));
                    oldchunks.Add((c.cx + i, c.cz - (sz * (upgSettings.farLimit)), UPGLOD.Far));
                }
                newchunks.Add((c.cx + i, c.cz + (sz * (upgSettings.renderDst + 1)), UPGLOD.Distant));
                oldchunks.Add((c.cx + i, c.cz - (sz * (upgSettings.renderDst)), UPGLOD.Distant));
            }
        }

        foreach (var oc in oldchunks) {
            //Debug.Log("unloading " + oc);
            meshPool.freeMesh((oc.cx, oc.cz), oc.lod);
        }
        foreach (var nc in newchunks) {
            //Debug.Log("loading " + nc);
            loadChunk((nc.cx, nc.cz), nc.lod);
        }
    }
}
