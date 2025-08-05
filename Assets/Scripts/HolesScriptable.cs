using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HoleType
{
    public HoleColor holeColor;
    public GameObject holePrefab;
    
    public Hole GetHoleComponent()
    {
        return holePrefab != null ? holePrefab.GetComponent<Hole>() : null;
    }
}

[CreateAssetMenu]
public class HolesScriptable : ScriptableObject
{
    public List<HoleType> holeTypes;
}
