using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class procMeshLoader : MonoBehaviour
{
    public int renderDst;
    private int renderIndex; // chunk indexes go from 0 to renderIndex
    public int[] storedChunk;     // last updated chunk
    public float chunkSizeUnits;

    public GameObject chunkCopy;
    public GameObject chunkParent;
    private List<GameObject[]> chunkPool; // [x][z]

    void Start() {
        renderIndex = (2 * renderDst) + 1;
        chunkSizeUnits = chunkCopy.transform.localScale.x * 10;

        // setup chunkpool
        chunkPool = new List<GameObject[]>();
        for (int c = 0; c < renderIndex; c++) {
            chunkPool.Insert(c, new GameObject[renderIndex]);
        }

        // populate chunkpool
        chunkCopy.SetActive(true);
        int ind = 0;
        for (int i = 0; i < renderIndex; i++) {
            for (int j = 0; j < renderIndex; j++) {
                Vector3 newPos = transform.position + new Vector3((i - renderDst)*chunkSizeUnits, 0, (j - renderDst)*chunkSizeUnits);
                newPos.y = 0;
                chunkPool[i][j] = Object.Instantiate(chunkCopy, newPos, chunkCopy.transform.rotation, chunkParent.transform);
                //chunkPool[i][j].transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
                chunkPool[i][j].name = ind.ToString();
                setChunkTerrain(chunkPool[i][j]);
                ind += 1;
            }
        }
        chunkCopy.SetActive(false);

        // prep storedchunk + player pos
        storedChunk = getCurrentChunk();
        transform.position += new Vector3(chunkSizeUnits/2, 0, chunkSizeUnits/2);
    }

    // the rotates for the arrays are pretty bad but given the (relatively) small size it should be fine as a temp solution
    void Update() {
        int[] cur = getCurrentChunk();

        // move nx row 
        if (cur[0] < storedChunk[0]) {
            storedChunk[0] = cur[0];
            for (int i = 0; i < renderIndex; i++) {
                setNewChunkPosition(chunkPool[renderIndex-1][i], new Vector3(-1, 0, 0));
            }
            // update chunk repr
            GameObject[] toMove = chunkPool[renderIndex - 1];
            for (int j = renderIndex - 1; j > 0; j--)
            {
                chunkPool[j] = chunkPool[j - 1];
            }
            chunkPool[0] = toMove;
        }

        // move nz row
        if (cur[1] < storedChunk[1]) {
            storedChunk[1] = cur[1];
            for (int i = 0; i < renderIndex; i++) {
                setNewChunkPosition(chunkPool[i][renderIndex - 1], new Vector3(0, 0, -1));
                // update chunk repr
                GameObject toMove = chunkPool[i][renderIndex - 1];
                for (int j = renderIndex - 1; j > 0; j--) {
                    chunkPool[i][j] = chunkPool[i][j - 1];
                }
                chunkPool[i][0] = toMove;
            }
        }

        // move x row 
        if (cur[0] > storedChunk[0]) {
            storedChunk[0] = cur[0];
            for (int i = 0; i < renderIndex; i++) {
                setNewChunkPosition(chunkPool[0][i], new Vector3(1, 0, 0));
            }
            // update chunk repr
            GameObject[] toMove = chunkPool[0];
            for (int j = 0; j < renderIndex - 1; j++)
            {
                chunkPool[j] = chunkPool[j + 1];
            }
            chunkPool[renderIndex - 1] = toMove;
        }


        // move z row
        if (cur[1] > storedChunk[1]) {
            storedChunk[1] = cur[1];
            for (int i = 0; i < renderIndex; i++) {
                setNewChunkPosition(chunkPool[i][0], new Vector3(0, 0, 1));
                // update chunk repr
                GameObject toMove = chunkPool[i][0];
                for (int j = 0; j < renderIndex - 1; j++) {
                    chunkPool[i][j] = chunkPool[i][j + 1];
                }
                chunkPool[i][renderIndex - 1] = toMove;
            }
        }
    }

    private void setChunkTerrain(GameObject chunk) {
    }

    private void setNewChunkPosition(GameObject chunk, Vector3 axis) {
        Vector3 newPos = chunk.transform.position + Vector3.Scale(axis, new Vector3( 
            renderIndex * chunkSizeUnits,
            0,
            renderIndex * chunkSizeUnits
        ));
        setChunkTerrain(chunk);
        chunk.transform.position = newPos;
    }

    private void printRepr() {
        string f = "";
        for (int i = 0; i < renderIndex; i++) {
            string s = "list "+i+": ";
            for (int j = 0; j < renderIndex; j++) {
                s += chunkPool[j][renderIndex-i-1].name + "  ";
            }
            f += s + "\n";
        }
        Debug.Log(f);
    }

    // get the chunk coords where player currently is
    private int[] getCurrentChunk()
    {
        return new int[2] {
            (int)transform.position.x / (int)chunkSizeUnits,
            (int)transform.position.z / (int)chunkSizeUnits
        };
    }
}
