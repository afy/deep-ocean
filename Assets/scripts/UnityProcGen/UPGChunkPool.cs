using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class UPGChunkPool {

    public UPGSettings settings;

    private Queue<(int cx, int cz)> chunks = new Queue<(int cx, int cz)>();
    private Dictionary<UPGLOD, Dictionary<(int cx, int cz), Vector3[]>> verts = new Dictionary<UPGLOD, Dictionary<(int cx, int cz), Vector3[]>>();
    private Dictionary<(int cx, int cz), List<GameObject>> objs = new Dictionary<(int cx, int cz), List<GameObject>>();

    private int vertslists;
    private int objlists;

    public UPGChunkPool() {
        foreach (UPGLOD l in Enum.GetValues(typeof(UPGLOD))) {
            verts[l] = new Dictionary<(int cx, int cz), Vector3[]>();
        }
    }

    public void setVerts((int cx, int cz) id, UPGLOD lod, Vector3[] v) {
        if (!chunks.Contains(id)) { addChunkToList(id); }
        if (!areVertsStored(id, lod)) {
            vertslists += 1;
        }
        verts[lod][id] = v;
    }

    public void addObjs((int cx, int cz) id, UPGLOD lod, GameObject go) {
        if (!chunks.Contains(id)) { addChunkToList(id); }
        if (!objs.ContainsKey(id)) {
            objs[id] = new List<GameObject>();
            objlists += 1;
        }

        objs[id].Add(go);
    }

    public void addChunkToList((int cx, int cz) id) {
        if (chunks.Count >= settings.chunkPoolLimit) {
            var ch = chunks.Dequeue();
            foreach (UPGLOD l in Enum.GetValues(typeof(UPGLOD))) {
                if (verts[l].ContainsKey(ch)) {
                    verts[l].Remove(ch);
                    vertslists -= 1;
                }
            }
            if (objs.ContainsKey(ch)) {
                objs.Remove(ch);
                objlists -= 1;
            }
        }

        chunks.Enqueue(id);
    }

    public bool isChunkStored((int cx, int cz) id) {
        return chunks.Contains(id);
    }

    public bool areVertsStored((int cx, int cz) id, UPGLOD lod) {
        return verts[lod].ContainsKey(id);
    }

    public Vector3[] getVerts((int cx, int cz) id, UPGLOD lod) {
        if (!areVertsStored(id, lod)) { throw new Exception("no verts have been generated for this lod"); }

        return verts[lod][id];
    }

    public int getNumberOfChunks() {
        return chunks.Count;
    }

    public int getNumberOfVertLists() {
        return vertslists;
    }

    public int getNumberOfObjLists() {
        return objlists;
    }
}