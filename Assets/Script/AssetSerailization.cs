using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetSerailization : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AsssetData asssetData = UnityEditor.AssetDatabase.LoadAssetAtPath<AsssetData>("Assets/Data/DataAsset.asset");
        Debug.Log(asssetData.id);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
