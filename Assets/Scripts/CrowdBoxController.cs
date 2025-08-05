using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class CrowdBoxController : MonoBehaviour
{
    [SerializeField] private HoleColor crowdBoxControllerColor;

    private List<CrowdBox> chainedCrowdBoxes;
    private CrowdBox currentSelectedBox;

    private LevelRuleTileManager levelRuleTileManager;

    [Header("Animation Settings")]
    [SerializeField] private float movementDuration = 0.3f;
    [SerializeField] private Ease movementEase = Ease.OutQuad;

    public List<CrowdBox> ChainedCrowdBoxes { get => chainedCrowdBoxes; }
    public CrowdBox CurrentSelectedBox { get => currentSelectedBox; set => currentSelectedBox = value; }
    public HoleColor CrowdBoxControllerColor { get => crowdBoxControllerColor; set => crowdBoxControllerColor = value; }

    private void Start()
    {
        chainedCrowdBoxes = new List<CrowdBox>(GetComponentsInChildren<CrowdBox>());
        levelRuleTileManager = GameManager.Instance.LevelRuleTileManger;
    }

    public void HandleSelectedBoxMovement(Vector2Int nextGridPos)
    {
        Vector2Int currentSelectedPos = currentSelectedBox.CurrentGridPos;
        bool isHead = IsHead(currentSelectedBox);
        bool isTail = IsTail(currentSelectedBox);

        // Check if trying to move into adjacent box (inward movement)
        if (IsMovingIntoAdjacentBox(nextGridPos, isHead, isTail))
        {
            // Handle backward movement
            HandleBackwardMovement(nextGridPos, isHead);
        }
        else
        {
            // Normal forward movement
            HandleForwardMovement(nextGridPos, currentSelectedPos);
        }
    }

    private bool IsMovingIntoAdjacentBox(Vector2Int nextGridPos, bool isHead, bool isTail)
    {
        if (!isHead && !isTail) return false;

        CrowdBox adjacentBox = null;
        
        if (isHead && chainedCrowdBoxes.Count > 1)
        {
            adjacentBox = chainedCrowdBoxes[1]; // Next box after head
        }
        else if (isTail && chainedCrowdBoxes.Count > 1)
        {
            adjacentBox = chainedCrowdBoxes[^2]; // Box before tail
        }

        return adjacentBox != null && adjacentBox.CurrentGridPos == nextGridPos;
    }

    private void HandleForwardMovement(Vector2Int nextGridPos, Vector2Int currentSelectedPos)
    {
        // Move the selected box to its new position
        MoveBoxToPosition(currentSelectedBox, nextGridPos, currentSelectedPos);

        // Move rest of the boxes in the chain - pass the original position of selected box
        HandleChainMovement(currentSelectedPos);
    }

    private void HandleBackwardMovement(Vector2Int attemptedPos, bool isHead)
    {
        // Find the opposite end of the chain and move it in that direction
        CrowdBox oppositeEnd = isHead ? GetTail() : GetHead();
        Vector2Int moveDirection = Utils.GetDirection(oppositeEnd.BoxGridDirection);
        Vector2Int newOppositeEndPos = oppositeEnd.CurrentGridPos + moveDirection;

        // Check if the new position is valid
        if (levelRuleTileManager.IsBlocked(newOppositeEndPos) ||
            levelRuleTileManager.IsInBounds(newOppositeEndPos) == false) return;

        // Store original position of the opposite end
        Vector2Int oppositeEndOriginalPos = oppositeEnd.CurrentGridPos;

        // Move the opposite end to the new position
        MoveBoxToPosition(oppositeEnd, newOppositeEndPos, oppositeEndOriginalPos);

        // Handle reverse chain movement
        HandleReverseChainMovement(oppositeEndOriginalPos, !isHead);
    }

    private void HandleChainMovement(Vector2Int selectedBoxOriginalPos)
    {
        bool isHead = IsHead(currentSelectedBox);
        HandleChainMovementInternal(selectedBoxOriginalPos, isHead);
    }

    private void HandleReverseChainMovement(Vector2Int oppositeEndOriginalPos, bool movingFromHead)
    {
        HandleChainMovementInternal(oppositeEndOriginalPos, movingFromHead);
    }

    private void HandleChainMovementInternal(Vector2Int startingPos, bool movingFromHead)
    {
        int selectedIndex = movingFromHead ? 0 : chainedCrowdBoxes.Count - 1;
        int direction = movingFromHead ? 1 : -1;
        int startIndex = movingFromHead ? selectedIndex + 1 : selectedIndex - 1;
        int endIndex = movingFromHead ? chainedCrowdBoxes.Count : -1;

        bool allowRotation = true;
        Vector2Int previousPos = startingPos;

        for (int i = startIndex; i != endIndex; i += direction)
        {
            CrowdBox box = chainedCrowdBoxes[i];

            if (movingFromHead)
            {
                if (box == GetTail()) allowRotation = false;
            }
            else
            {
                if (box == GetHead()) allowRotation = false;
            }

            Vector2Int currentPos = box.CurrentGridPos;
            
            // Move each box to the previous box's position
            MoveBoxToPosition(box, previousPos, currentPos, allowRotation);
            
            // Update previousPos for next iteration
            previousPos = currentPos;
        }

        SetEndBoxRotation(movingFromHead);
    }

    private void MoveBoxToPosition(CrowdBox box, Vector2Int targetGridPos, Vector2Int fromGridPos, bool allowRotation = true)
    {
        Vector3 targetWorldPos = levelRuleTileManager.GetWorldPosition(targetGridPos.x, targetGridPos.y);
        GridDirection moveDirection = Utils.GetDirectionFromPos(fromGridPos, targetGridPos);
        float targetRotation = Utils.GetWorldDirFromGridDir(moveDirection);

        if (allowRotation) box.transform.DORotateQuaternion(Quaternion.Euler(0, targetRotation, 0), 0.15f).SetEase(Ease.OutQuad);
        box.transform.DOMove(targetWorldPos, movementDuration).SetEase(movementEase);
        box.CurrentGridPos = targetGridPos;
        
        // Update the BoxGridDirection to keep it in sync
        box.BoxGridDirection = moveDirection;
    }

    private void SetEndBoxRotation(bool isHead)
    {
        if (chainedCrowdBoxes.Count < 2) return; // Need at least 2 boxes for this logic

        var adjacentBox = isHead ? chainedCrowdBoxes[^2] : chainedCrowdBoxes[1];
        var endBox = isHead ? GetTail() : GetHead();

        float adjacentRotationOpp = Utils.GetWorldDirFromGridDir(Utils.GetOppositeGridDirection(adjacentBox.BoxGridDirection));

        endBox.transform.DORotateQuaternion(Quaternion.Euler(0, adjacentRotationOpp, 0), 0.2f);

        endBox.BoxGridDirection = Utils.GetGridDirFromWorldDir(adjacentRotationOpp);
    }

    public void JumpIntoHole(Vector2Int holePos)
    {
        var holeWorldPos = levelRuleTileManager.GetWorldPosition(holePos.x, holePos.y);
        holeWorldPos.y = -2;

        // Create a sequence for the entire chain animation
        Sequence jumpSequence = DOTween.Sequence();

        // Find the index of the current selected box
        bool movingFromHead = IsHead(currentSelectedBox);
        int selectedIndex = movingFromHead ? 0 : chainedCrowdBoxes.Count - 1;

        // Create lists for boxes before and after the selected box
        List<CrowdBox> boxesToAnimate = new List<CrowdBox>();

        // Add selected box first
        boxesToAnimate.Add(currentSelectedBox);

        int direction = movingFromHead ? 1 : -1;
        int startIndex = movingFromHead ? selectedIndex + 1 : selectedIndex - 1;
        int endIndex = movingFromHead ? chainedCrowdBoxes.Count : -1;

        for (int i = startIndex; i != endIndex; i += direction)
            boxesToAnimate.Add(chainedCrowdBoxes[i]);

        float delayBetweenBoxes = 0.25f;
        float suckDuration = 1f;

        for (int i = 0; i < boxesToAnimate.Count; i++)
        {
            CrowdBox box = boxesToAnimate[i];
            float delay = i * delayBetweenBoxes;
            Vector3 startPos = box.transform.position;

            // Calculate control point for curved path (above and toward the hole)
            Vector3 controlPoint = Vector3.Lerp(startPos, holeWorldPos, 0.5f);
            controlPoint.y += 3f; // Height of the curve

            // Create a curved path animation using DOPath
            Vector3[] pathPoints = { startPos, controlPoint, holeWorldPos };
            
            jumpSequence.Insert(delay, box.transform.DOPath(pathPoints, suckDuration, PathType.CatmullRom)
                .SetEase(Ease.InQuart)); // InQuart gives acceleration toward the end

            // Add scaling effect to simulate being sucked in
            jumpSequence.Insert(delay, box.transform.DOScale(0.1f, suckDuration)
                .SetEase(Ease.InQuart));

            // Optional: Add rotation for more dynamic effect
            jumpSequence.Insert(delay, box.transform.DORotate(new Vector3(0, 720f, 0), suckDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InQuart));

            // Disable the box after animation
            //jumpSequence.InsertCallback(delay + suckDuration, () => box.gameObject.SetActive(false));
        }

        // Clear the chained boxes list after all animations complete
        jumpSequence.OnComplete(() => {
            chainedCrowdBoxes.Clear();
            currentSelectedBox = null;
        });
    }

    public bool IsHead(CrowdBox crowdBox)
    {
        return chainedCrowdBoxes[0] == crowdBox;
    }

    public bool IsTail(CrowdBox crowdBox)
    {
        return chainedCrowdBoxes[^1] == crowdBox;
    }

    public CrowdBox GetHead()
    {
        return chainedCrowdBoxes[0];
    }

    public CrowdBox GetTail()
    {
        return chainedCrowdBoxes[^1];
    }

    public bool IsHeadOrTail(CrowdBox crowdBox)
    {
        if (chainedCrowdBoxes[0] == crowdBox ||
            chainedCrowdBoxes[^1] == crowdBox)
        {
            currentSelectedBox = crowdBox;
            return true;
        }
        return false;
    }

    public void Released()
    {
        currentSelectedBox = null;
    }
}
