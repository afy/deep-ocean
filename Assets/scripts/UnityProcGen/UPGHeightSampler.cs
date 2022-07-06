using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightSamplerExample {
    static int seed = 9001;
    static FastNoiseLite noise1;
    static FastNoiseLite noise2;
    static FastNoiseLite noise3;

    public static void setup() {
        noise1 = new FastNoiseLite();
        noise1.SetSeed(seed);
        noise1.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        noise1.SetFrequency(0.0015f);
        noise1.SetFractalType(FastNoiseLite.FractalType.Ridged);
        noise1.SetFractalOctaves(5);
        noise1.SetFractalLacunarity(2.0f);
        noise1.SetFractalGain(0.5f);

        noise2 = new FastNoiseLite();
        noise2.SetSeed(seed);
        noise2.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise2.SetFrequency(0.6f);

        noise3 = new FastNoiseLite();
        noise3.SetSeed(seed);
        noise3.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise3.SetFrequency(0.001f);
    }

    public static float sample(float x, float z) {
        float s1 = 200  * noise1.GetNoise(x, z);
        float s2 = 0.5f * noise2.GetNoise(x, z);
        float s3 = 30   * noise3.GetNoise(x, z);
        return 0; // return s1 + s2 + s3;
    }
}