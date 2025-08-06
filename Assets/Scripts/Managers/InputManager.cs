using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    public LayerMask ignoreLayers;
    public GameObject lastTappedObject;
    public LevelRuleTileManager levelRuleTileManager;

    [Header("Swipe Settings")]
    private Vector2 swipeStartScreenPos;
    private bool isSwiping = false;
    private float minSwipeDist = 50f;
    private Vector2Int lastDirection = Vector2Int.zero;

    [Header("Fade Settings")]
    [Tooltip("Seconds to wait between fading each tile")]
    public float moveSpeed = 0.1f;

    // Runtime state
    private bool pointerDown = false;
    private Vector2 pointerDownPos;
    private HoleController currentSwipedHole;
    private Coroutine _fadeTilesCoroutine;

    private void Start()
    {
        if (levelRuleTileManager == null)
            levelRuleTileManager = FindObjectOfType<LevelRuleTileManager>();
    }

    /// <summary>
    /// Predicts where the hole will stop sliding along a straight line.
    /// </summary>
    private Vector2Int ComputeEndPosition(Vector2Int start, Vector2Int direction)
    {
        var next = start + direction;
        while (!levelRuleTileManager.IsBlocked(next) && !levelRuleTileManager.IsHole(next))
            next += direction;
        return next - direction;
    }

    /// <summary>
    /// Yields every cell from start to end, inclusive, along a straight cardinal line.
    /// </summary>
    private IEnumerable<Vector2Int> GetPathInclusive(Vector2Int start, Vector2Int end)
    {
        var delta   = end - start;
        var step    = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
        var current = start;
        while (true)
        {
            yield return current;
            if (current == end) yield break;
            current += step;
        }
    }

    void Update()
    {
        Vector2 pointerPos = Vector2.zero;
        bool pointerPressed = false, pointerReleased = false, pointerIsHeld = false;

        // Mouse input
        if (Mouse.current != null)
        {
            pointerPos      = Mouse.current.position.ReadValue();
            pointerPressed  = Mouse.current.leftButton.wasPressedThisFrame;
            pointerReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            pointerIsHeld   = Mouse.current.leftButton.isPressed;
        }

        // Touch input
        if (Touchscreen.current != null)
        {
            pointerPos      = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerPressed |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            pointerReleased|= Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            pointerIsHeld   |= Touchscreen.current.primaryTouch.press.isPressed;
        }

        // On pointer down: try to pick up a hole
        if (pointerPressed)
        {
            pointerDown       = true;
            pointerDownPos    = pointerPos;
            Ray ray           = Camera.main.ScreenPointToRay(pointerPos);
            if (Physics.Raycast(ray, out RaycastHit hit, ignoreLayers) &&
                hit.collider.gameObject.CompareTag("Hole"))
            {
                var holeCtrl = hit.collider.gameObject.GetComponent<HoleController>();
                if (holeCtrl != null)
                {
                    lastTappedObject   = holeCtrl.gameObject;
                    currentSwipedHole  = holeCtrl;
                    swipeStartScreenPos = pointerDownPos;
                    isSwiping          = true;
                    lastDirection      = Vector2Int.zero;
                }
            }
        }

        // Continuous swipe: while holding and hole is stationary
        if (isSwiping && currentSwipedHole != null && pointerIsHeld && !currentSwipedHole.isMoving)
        {
            Vector2 swipeDelta = pointerPos - swipeStartScreenPos;
            if (swipeDelta.magnitude >= minSwipeDist)
            {
                Vector2 absDelta = new Vector2(Mathf.Abs(swipeDelta.x), Mathf.Abs(swipeDelta.y));
                Vector2Int direction = absDelta.x > absDelta.y
                    ? (swipeDelta.x > 0 ? Vector2Int.right : Vector2Int.left)
                    : (swipeDelta.y > 0 ? Vector2Int.up    : Vector2Int.down);

                if (direction != lastDirection || lastDirection == Vector2Int.zero)
                {
                    lastDirection = direction;

                    Vector2Int startGridPos = currentSwipedHole.holePos;
                    Vector2Int endGridPos   = ComputeEndPosition(startGridPos, direction);

                    // Cancel any in-progress fade loop
                    if (_fadeTilesCoroutine != null)
                        StopCoroutine(_fadeTilesCoroutine);

                    // Build and start the staggered fade coroutine
                    var path = new List<Vector2Int>(GetPathInclusive(startGridPos, endGridPos));
                    _fadeTilesCoroutine = StartCoroutine(FadeInnerTilesWithDelay(path));

                    // Now slide the hole; reset origin when done
                    currentSwipedHole.SlideUntilBlocked(direction, levelRuleTileManager, actualEnd =>
                    {
                        swipeStartScreenPos = pointerPos;
                    },moveSpeed);
                }
            }
        }

        // On pointer up: reset swipe state
        if (pointerDown && pointerReleased)
        {
            isSwiping         = false;
            pointerDown       = false;
            currentSwipedHole = null;
            lastDirection     = Vector2Int.zero;
        }
    }

    /// <summary>
    /// Fades each inner-tile in turn, waiting fadeDelay between each.
    /// </summary>
    private IEnumerator FadeInnerTilesWithDelay(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            var inner = levelRuleTileManager.GetInnerTile(cell);
            if (inner != null)
                inner.FadeFromHalfToZero();

            // for the first two cells, don’t wait at all;
            // after that, pause between each fade
            if (i >= 1)
                yield return new WaitForSeconds(moveSpeed);
        }
        _fadeTilesCoroutine = null;
    }


    /// <summary>
    /// Allows other scripts to query what was last tapped.
    /// </summary>
    public GameObject GetLastTappedObject() => lastTappedObject;
}
