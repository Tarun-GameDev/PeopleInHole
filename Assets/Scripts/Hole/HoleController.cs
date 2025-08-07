using System;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HoleController : MonoBehaviour
{
    [Header("Default Assigns")] public MeshRenderer[] MeshRenderers;

    public ColorEnum holeColor;
    public Vector2Int holePos;

    public GameManager gameManager;
    public bool isMoving = false;
    public bool holeDead =  false;
    public void UpdateHoleMaterials()
    {
        if (gameManager == null)
            gameManager = GameObject.FindObjectOfType<GameManager>();

        if (gameManager != null)
        {
            var holeMaterial = gameManager.colorManager.ColorEnum_Material[(int)holeColor].HoleMaterial;
            foreach (var _mesh in MeshRenderers)
                _mesh.sharedMaterial = holeMaterial;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
#endif
    }

    private void OnValidate()
    {
        UpdateHoleMaterials();
    }

    // --- Move and slide logic encapsulated here ---
    public void SlideUntilBlocked(Vector2Int direction, LevelRuleTileManager mgr, Action<Vector2Int> onComplete,
        float moveSpeed)
    {
        if (isMoving || holeDead || mgr == null) return;

        Vector2Int currentPos = holePos;
        Vector2Int targetPos = currentPos;

        bool playerGroupFoundInTargetPos = false;

        // Slide until blocked
        while (true)
        {
            Vector2Int nextPos = targetPos + direction;
            bool canMove =
                mgr.innerTilePositionsCache.Contains(nextPos) &&
                !mgr.IsBlocked(nextPos) &&
                !mgr.IsHole(nextPos) && !mgr.IsPlayerGroup(nextPos);

            if (canMove)
                targetPos = nextPos;
            else
            {
                if (mgr.IsPlayerGroup(nextPos))
                {
                    if (holeColor == mgr.GetPlayerGroupColor(nextPos))
                    {
                        playerGroupFoundInTargetPos = true;
                        holeDead = true;
                        targetPos = nextPos;
                    }
                }

                break;
            }
        }

        if (targetPos != currentPos)
        {
            float moveDistance = (targetPos - currentPos).magnitude;
            isMoving = true;
            transform.DOMove(
                mgr.GetWorldPosition(targetPos.x, targetPos.y, true),
                moveSpeed * moveDistance
            ).OnComplete(() =>
            {
                if (playerGroupFoundInTargetPos)
                {
                     mgr.GetPlayerGroup(targetPos).DestroyPlayerGroup();
                     mgr.RemovePlayerGroup(targetPos);
                     mgr.RemoveHole(targetPos);
                     MakeHoleDeadEffect();
                }
                else
                {
                    mgr.UpdateHolePos(this, targetPos);
                }
                isMoving = false;
                onComplete?.Invoke(targetPos);
                
            });
        }
        else
        {
            onComplete?.Invoke(currentPos);
        }
    }

    public void MakeHoleDeadEffect()
    {
        gameObject.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).SetDelay(0.4f).OnComplete(() => gameObject.SetActive(false));
    }
}