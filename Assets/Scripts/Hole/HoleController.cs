using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HoleController : MonoBehaviour
{
    [Header("Default Assigns")]
    public MeshRenderer[] MeshRenderers;

    public ColorEnum holeColor;
    public Vector2Int holePos;

    public GameManager gameManager;
    
    public void UpdateHoleMaterials()
    {
        if (gameManager == null)
        {
            gameManager = GameObject.FindObjectOfType<GameManager>();
        }

        if (gameManager != null)
        {
            var holeMaterial = gameManager.colorManager.ColorEnum_Material[(int)holeColor].HoleMaterial;
            foreach (var _mesh in MeshRenderers)
            {
                _mesh.sharedMaterial = holeMaterial;
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private void OnValidate()
    {
        UpdateHoleMaterials();
    }
}