using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

[Serializable]
public struct UPGSettings {
    public int chunkSize; // for "distant" lod size
    public int renderDst;
}

[Serializable]
public class UPGChunkData {
    public Tuple<int, int> id;
    public Vector3[] verts;
}

// essentially a multiplier for (default) chunk size
/*
public enum UPGLOD {
    Close = 8,
    Mid = 4,
    Far = 2,
    Distant = 1
}*/

public class UnityProcGen : MonoBehaviour {
    // unity
    public UPGSettings upgSettings;
    public UPGMeshPool meshPool;
    public GameObject physChunk;

    public Dictionary<Tuple<int, int>, UPGChunkData> chunks = new Dictionary<Tuple<int, int>, UPGChunkData>();


    private void Start() {
        HeightSamplerExample.setup();
        meshPool = new UPGMeshPool() {
            settings = upgSettings,
        };

        List<Tuple<int, int>> cs = new List<Tuple<int, int>>();
        int d = (2 * upgSettings.renderDst) + 1;
        int s = -d / 2;
        int e = d / 2;
        for (int w = s; w <= e; w++) {
            for (int h = s; h <= e; h++) {
                cs.Add(new Tuple<int, int>(w, h));
            }
        }

        var ts = Time.time * 1000;
        foreach (var ch in cs) {
            createChunk(ch);
        }
        var te = (Time.time * 1000) - ts;
        Debug.Log("Total time: " + te + "ms");
    }

    private void createChunk(Tuple<int,int> id) {
        int sl = upgSettings.chunkSize;
        UPGChunkData chunk = new UPGChunkData();
        chunk.id = id;
        meshPool.generateMesh(id);
        Mesh mesh = meshPool.getMeshInstance(id);

        float tstart = Time.time;
        var result = new NativeArray<Vector3>(sl * sl, Allocator.Persistent);
        var job = new UPGHeightJob() {
            settings = upgSettings,
            result = result,
            cx = id.Item1,
            cz = id.Item2
        };

        var handle = job.Schedule(sl * sl, 1);
        handle.Complete();

        mesh.SetVertices(job.result);
        mesh.RecalculateNormals();

        GameObject go = Instantiate(physChunk);
        go.SetActive(true);
        go.transform.position = new Vector3((upgSettings.chunkSize-1) * id.Item1, 0, (upgSettings.chunkSize-1) * id.Item2);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;

        float t = (Time.time) - tstart;
        Debug.Log("Generated chunk, time: " + t + "ms");

        result.Dispose();
        
    }
}
