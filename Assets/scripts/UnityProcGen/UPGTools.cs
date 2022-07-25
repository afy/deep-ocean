using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NOTE:
// chunkSize must be set so that all chunks lods must be divisible by the smallest
// python script to find possible sizes iven lod (implement this into menu?)
/*
def sidelen(cs, l):
    return (l*2) * (((cs-1)//(l*2)))
def x(m, f, d, maxd=300):
    for i in range(0, maxd):
        cl = i-1
        ml = sidelen(i, m)
        fl = sidelen(i, f)
        dl = sidelen(i, d)
        #print(i, cl, ml, fl, dl)
        if cl == ml and ml == fl and fl == dl:
            print(i)
x(2,8,12)
*/
[Serializable]
public struct UPGSettings {
    public int chunkSize;
    public int renderDst;
    public int closeLimit;
    public int mediumLimit;
    public int farLimit;
    public int chunkPoolLimit;
}

[Serializable]
public struct UPGDebugSettings {
    public bool colouredMeshes;
    public bool forceFlatTerrain;
}

public enum UPGLOD {
    Close = 2,
    Medium = 8,
    Far = 12,
    Distant = 16
}

public static class UPGTools {
    public static (int cx, int cz) getChunkCoord(Vector3 p, UPGSettings s) {
        int x = Mathf.RoundToInt(p.x / s.chunkSize);
        int z = Mathf.RoundToInt(p.z / s.chunkSize);
        return (x, z);
    }

    public static int returnChunkChangeDir(int c, int cur) {
        int v = (int)Mathf.Sign(c-cur);
        return (c - cur == 0) ? 0 : v;
    }
}