using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerGroup : MonoBehaviour
{
    public ColorEnum playerColor;
    public Vector2Int playerPos;
    public SkinnedMeshRenderer[] playerMeshes;

    public GameManager gameManager;

    public void UpdatePlayerMaterials()
    {
        if (gameManager == null)
            gameManager = GameObject.FindObjectOfType<GameManager>();

        if (gameManager != null)
        {
            var holeMaterial = gameManager.colorManager.ColorEnum_Material[(int)playerColor].PlayerMaterial;
            foreach (var _mesh in playerMeshes)
                _mesh.sharedMaterial = holeMaterial;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
#endif
    }

    private void OnValidate()
    {
        UpdatePlayerMaterials();
    }

    public void DestroyPlayerGroup()
    {
        gameObject.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() => gameObject.SetActive(false));
    }
}