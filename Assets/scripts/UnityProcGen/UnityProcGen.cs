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
-Add support for objects in separate chunk-related storage from heightpoints (easily recalculated, but objects are persistent).
  maybe a database?
-General optimization ideas:
    -Try using "half" class for points instead of vector3 using a vertex-shader based heightmap renderer
    -Review the sketchy NativeArray -> Vector3[] in calculateChunkHeights function
    -Multithreaded loading (if not already) 
    -Review LOD levels for "Close" (possible save on verteces)
*/



public class UnityProcGen : MonoBehaviour {
    // unity
    public UPGSettings upgSettings;
    public UPGDebugSettings upgDebugSettings;
    public GameObject chunkStencil;
    public GameObject target;
    public bool debugOverrideHeight;
    public Text debugTextChunks;

    // internal
    private UPGMeshPool meshPool;
    private UPGChunkPool chunkPool;
    private (int cx, int cz) currentChunk;
    private bool initialized;

    private void Start() {
        int nochunks = (2 * upgSettings.renderDst + 1) * (2 * upgSettings.renderDst + 1);
        if (upgSettings.chunkPoolLimit < nochunks) {
            throw new Exception("chunk pool limit ("+upgSettings.chunkPoolLimit+") cannot be smaller than number of chunks ("+nochunks+")");
        }

        currentChunk = UPGTools.getChunkCoord(target.transform.position, upgSettings);
        meshPool = new UPGMeshPool() {
            settings = upgSettings,
            debugSettings = upgDebugSettings
        };
        var ret = meshPool.createInitialMeshes(chunkStencil);
        chunkPool = new UPGChunkPool() {
            settings = upgSettings
        };
        foreach (var e in ret) {
            loadChunk((e.Item1.x, e.Item1.z), e.lod);
        }
        float y = target.transform.position.y;
        target.transform.position = new Vector3((currentChunk.cx + 0.5f) * upgSettings.chunkSize, y, (currentChunk.cz + 0.5f) * upgSettings.chunkSize);
        initialized = true;
    }

    private void Update() {
        if (!initialized) { return; }
        debugTextChunks.text = "#" + chunkPool.getNumberOfChunks() + ", VL: " + chunkPool.getNumberOfVertLists() + ",  OL: " + chunkPool.getNumberOfObjLists();

        var c = UPGTools.getChunkCoord(target.transform.position, upgSettings);
        if (c == currentChunk) { return; }

        updateChunksOnMove(c);
    }

    private void loadChunk((int cx, int cz) id, UPGLOD lod) {
        if (!chunkPool.isChunkStored(id)) {
            chunkPool.addChunkToList(id);
        }
        if (!chunkPool.areVertsStored(id, lod)) {
            calculateChunkHeights(id, lod);
        }

        setChunk(id, lod);
    }

    private void calculateChunkHeights((int cx, int cz) id, UPGLOD lod) {
        if (chunkPool.areVertsStored(id, lod)) { throw new Exception("values already exist for this chunk's lod"); }

        int cinc = ((int)lod == 0) ? 1 : (int)lod * 2;
        int cverts = (upgSettings.chunkSize - 1) / cinc + 1;
        var result = new NativeArray<Vector3>(cverts * cverts, Allocator.Persistent);
        var job = new UPGHeightJob() {
            settings = upgSettings,
            debugSettings = upgDebugSettings,
            result = result,
            cx = id.cx,
            cz = id.cz,
            cinc = cinc,
            cverts = cverts
        };
        var handle = job.Schedule(cverts * cverts, cverts * cverts);
        handle.Complete();

        Mesh m = new Mesh();
        m.SetVertices(job.result);
        chunkPool.setVerts(id, lod, m.vertices);
        result.Dispose();
    }

    private void setChunk((int cx, int cz) id, UPGLOD lod) {
        if (meshPool.getFreeMeshCount(lod) <= 0) { throw new Exception("No meshes in available pool"); }
        if (!chunkPool.isChunkStored(id)) { throw new Exception("No chunk of that id has been created"); }
        if (!chunkPool.areVertsStored(id, lod)) { throw new Exception("no verts have been stored for this lod"); }

        GameObject go = meshPool.getNewMeshInstance(id, lod);
        go.name = "chunk (" + id.cx.ToString() + ", " + id.cz.ToString() + ")";
        float y = go.transform.position.y;
        go.transform.position = new Vector3(id.cx * (upgSettings.chunkSize - 1), y, id.cz * (upgSettings.chunkSize - 1));
        Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
        m.SetVertices(chunkPool.getVerts(id, lod));
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
            meshPool.freeMesh((oc.cx, oc.cz), oc.lod);
        }
        foreach (var nc in newchunks) {
            loadChunk((nc.cx, nc.cz), nc.lod);
        }
    }
}
