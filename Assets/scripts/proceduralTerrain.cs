// heavily inspired by https://www.youtube.com/watch?v=4RpVBYW1r5M

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class proceduralTerrainSampler {
    int seed = 9001;

    FastNoiseLite noise1;
    float amp1;
    FastNoiseLite noise2;
    float amp2;
    FastNoiseLite noise3;
    float amp3;

    public proceduralTerrainSampler()
    {
        noise1 = new FastNoiseLite();
        amp1 = 250;
        noise1.SetSeed(seed);
        noise1.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        noise1.SetFrequency(0.0015f);
        noise1.SetFractalType(FastNoiseLite.FractalType.Ridged);
        noise1.SetFractalOctaves(5);
        noise1.SetFractalLacunarity(2.0f);
        noise1.SetFractalGain(0.5f);

        noise2 = new FastNoiseLite();
        amp2 = 0.5f;
        noise2.SetSeed(seed);
        noise2.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise2.SetFrequency(0.6f);

        noise3 = new FastNoiseLite();
        amp3 = 30;
        noise3.SetSeed(seed);
        noise3.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise3.SetFrequency(0.001f);
    }

    public float sampleHeight(int x, int z) {
        float s1 = amp1 * noise1.GetNoise(x, z);
        float s2 = amp2 * noise2.GetNoise(x, z);
        float s3 = amp3 * noise3.GetNoise(x, z);
        return s1 + s2 + s3;
    }
}

public class Chunk {
    public Vector2Int index;
    int chunkSize;

    Vector3[] verts;
    Vector2[] uvs;
    int[] tris;

    int triIndex;

    public Chunk(int x, int z, int size) {
        index = new Vector2Int(x, z);
        chunkSize = size + 2; // account for seams
        verts = new Vector3[chunkSize * chunkSize];
        uvs = new Vector2[chunkSize * chunkSize];
        tris = new int[(chunkSize-1) * (chunkSize-1) * 6];
    }

    public void addTri(int a, int b, int c) {
        tris[triIndex] = c;
        tris[triIndex + 1] = b;
        tris[triIndex + 2] = a;
        triIndex += 3;
    }

    public Mesh createMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }

    public void generateHeightData(proceduralTerrainSampler sampler) {
        int vertIndex = 0;
        for (int z = 0; z < chunkSize; z++) {
            for (int x = 0; x < chunkSize; x++) {
                int cox = (chunkSize * index[0]) + x;
                int coz = (chunkSize * index[1]) + z;

                verts[vertIndex] = new Vector3(x, sampler.sampleHeight(cox, coz), z);
                uvs[vertIndex] = new Vector2(x / (float)chunkSize, z / (float)chunkSize);

                if (x < chunkSize - 1 && z < chunkSize - 1) {
                    addTri(vertIndex, vertIndex+chunkSize+1, vertIndex+chunkSize);
                    addTri(vertIndex+chunkSize+1, vertIndex, vertIndex+1);
                }

                vertIndex++;
            }
        }
    }
}

public class proceduralTerrain : MonoBehaviour
{
    proceduralTerrainSampler sampler;

    public GameObject chunkMeshSample;
    List<GameObject> chunkObjects = new List<GameObject>();
    List<Chunk> chunks = new List<Chunk>();

    public int renderDst;
    public int chunkSize;
    public bool colorChunks = false;

    private void Start() {
        sampler = new proceduralTerrainSampler();

        int d = (2 * renderDst) + 1;
        int s = - d/2;
        int e = d/2;
        for (int w = s; w <= e; w++) {
            for (int h = s; h <= e; h++) {
                generateChunk(w, h);
            }
        }

        chunkMeshSample.SetActive(false);
    }

    public void generateChunk(int x, int z) {
        Chunk chunk = new Chunk(x, z, chunkSize);
        chunks.Add(chunk);
        chunk.generateHeightData(sampler);
        Mesh mesh = chunk.createMesh();

        // rendering
        GameObject chunkObject = Instantiate(chunkMeshSample);
        chunkObject.SetActive(true);
        chunkObject.transform.position = new Vector3(chunkSize * x, 0, chunkSize * z);
        chunkObjects.Add(chunkObject);
        chunkObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        if (colorChunks) { chunkObject.GetComponent<MeshRenderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f); }
    }
}