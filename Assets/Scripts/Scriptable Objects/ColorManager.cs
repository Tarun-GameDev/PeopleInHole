using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
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
    
    

    [Button("Set All Values")]
    public void SetAllColors()
    {
#if UNITY_EDITOR
        var allEnumValues = (ColorEnum[])Enum.GetValues(typeof(ColorEnum));
        Dictionary<ColorEnum, ColorMaterals> colorMap = new Dictionary<ColorEnum, ColorMaterals>();

        // Preserve existing unique entries
        foreach (var entry in ColorEnum_Material)
        {
            if (!colorMap.ContainsKey(entry.Color))
            {
                colorMap[entry.Color] = entry;
            }
        }

        // Ensure every enum has an entry, add if missing
        foreach (var enumValue in allEnumValues)
        {
            if (!colorMap.ContainsKey(enumValue))
            {
                colorMap[enumValue] = new ColorMaterals
                {
                    Color = enumValue,
                    PlayerMaterial = null,
                    HoleMaterial = null,
                    baseColor = Color.white
                };
            }
        }

        // Rebuild the list in enum order (sorted by enum)
        ColorEnum_Material = new List<ColorMaterals>();
        foreach (var enumValue in allEnumValues)
        {
            ColorEnum_Material.Add(colorMap[enumValue]);
        }

        // Save and mark the asset dirty
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("ColorEnum_Material list updated: no duplicates, sorted, and complete.");
#endif
    }

    public List<Material> materials = new List<Material>();
    
    [Button("Auto Assign Hole Materials")]
    public void AutoAssignMaterials()
    {
#if UNITY_EDITOR
        var allEnumValues = (ColorEnum[])Enum.GetValues(typeof(ColorEnum));

        if (materials.Count != allEnumValues.Length)
        {
            Debug.LogError($"Material list count ({materials.Count}) does not match ColorEnum count ({allEnumValues.Length})");
            return;
        }

        foreach (var colorMaterial in ColorEnum_Material)
        {
            int index = (int)colorMaterial.Color;
            if (index >= 0 && index < materials.Count)
            {
                colorMaterial.PlayerMaterial = materials[index];
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("HoleMaterials assigned from materials list.");
#endif
    }

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