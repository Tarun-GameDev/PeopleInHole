using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class ColorMaterals
{
    public ColorEnum Color;
    public Material PlayerMaterial;
    public Material HoleMaterial;
    public Color baseColor;
}

[CreateAssetMenu(fileName = "NewColorManager", menuName = "ScriptableObjects/ColorManager", order = 1)]
public class ColorManager : ScriptableObject
{
    public List<ColorMaterals> ColorEnum_Material = new List<ColorMaterals>();
}


public enum ColorEnum
{
    Black,
    Blue,
    Brown,
    ChocolateMilk,
    Coral,
    DarkFuschia,
    Dirty,
    ForestGreen,
    Gold,
    Green,
    Grey,
    Indigo,
    Mint,
    Navy,
    Orange,
    PastelPink,
    Pink,
    Purple,
    Red,
    Silver,
    Yellow
}