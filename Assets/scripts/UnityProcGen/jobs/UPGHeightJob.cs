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

    public void Execute(int index) {
        var x = index / settings.chunkSize;
        var z = index % settings.chunkSize;
        var pos = new float2((settings.chunkSize * cx) + x, (settings.chunkSize * cz) + z);
        float y = noise.snoise(pos / 16);
        result[index] = new Vector3(x, y, z);
    }
}