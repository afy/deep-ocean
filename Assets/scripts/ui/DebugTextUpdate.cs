using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugTextUpdate : MonoBehaviour
{
    public Text t;
    public GameObject target;

    void Update()   {
        var s = target.GetComponent<UnityProcGen>().upgSettings;
        t.text = UPGTools.getChunkCoord(target.transform.position, s).ToString();
    }
}
