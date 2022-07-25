using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public struct UPGHeightJob : IJobParallelFor {
    public NativeArray<Vector3> result;
    public UPGSettings settings;
    public int cx;
    public int cz;
    public int cinc;
    public int cverts;
    public bool debugOH;

    public void Execute(int index) {
        float x = cinc * (index % cverts);
        float z = cinc * (index / cverts);

        var pos = new float2(((settings.chunkSize-1) * cx) + x, ((settings.chunkSize-1) * cz) + z);
        
        float freq = 0.0007f;
        float amp = 300f;
        float y =  amp * noise.snoise(pos * freq);
        result[index] = new Vector3(x, (debugOH) ? 0 : y, z);
    }
}