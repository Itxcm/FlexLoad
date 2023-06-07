using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataAsset", menuName = "CreateAsset", order = 0)]
public class AsssetData : ScriptableObject
{
    public string id;
    public new string name;
    public List<int> list;
}

