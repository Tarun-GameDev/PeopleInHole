using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    PlayerInput playerInput;

    // Actions
    InputAction touchPositionAction;
    InputAction touchPressAction;
    InputAction touchDeltaAction;

    Vector3 touchPosition;

    [SerializeField] LayerMask crowdBoxLayerMask;
    [SerializeField] LayerMask gridDetectionPlane;

    public CrowdBoxController currentCrowdBoxController;

    [Header("Drag Controls")]
    [SerializeField] private float dragThreshold = 20f; // Minimum drag distance to trigger
    [SerializeField] private float deltaThreshold = 5f; // Minimum delta magnitude to process
    [SerializeField] private float directionCooldown = 0.1f; // Prevent rapid direction changes

    public Vector2 startPos;
    public bool isDragging = false;

    private UnityEvent<Vector2Int> OnDragDirectionDetected = new();

    // Movement tracking
    private Vector2Int lastValidGridPos;
    
    // Delta accumulation for better direction detection
    private Vector2 accumulatedDelta = Vector2.zero;
    private Vector2Int lastDetectedDirection;
    private float lastDirectionTime;

    LevelRuleTileManager levelRuleTileManager;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        touchPositionAction = playerInput.actions["TouchPosition"];
        touchPressAction = playerInput.actions["TouchPress"];
        touchDeltaAction = playerInput.actions["TouchDelta"];
    }

    private void Start()
    {
        levelRuleTileManager = GameManager.Instance.LevelRuleTileManger;

        touchPressAction.started += HandleTouchPress;
        touchPressAction.canceled += HandleTouchReleased;

        touchPositionAction.performed += HandleTouchPosition;
        
        // Use touchDelta for drag direction detection
        touchDeltaAction.performed += HandleTouchDelta;

        OnDragDirectionDetected.AddListener(HandleDragMovement);
    }

    private void HandleTouchPress(InputAction.CallbackContext context)
    {
        touchPosition = touchPositionAction.ReadValue<Vector2>();

        Ray ray = Camera.main.ScreenPointToRay(touchPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, crowdBoxLayerMask))
        {
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.TryGetComponent<CrowdBox>(out CrowdBox crowdBox))
                {
                    // Get the CrowdBoxController from the parent GameObject
                    CrowdBoxController crowdBoxController = crowdBox.GetComponentInParent<CrowdBoxController>();

                    if (crowdBoxController != null)
                    {
                        if (crowdBoxController.IsHeadOrTail(crowdBox))
                        {
                            currentCrowdBoxController = crowdBoxController;
                            lastValidGridPos = currentCrowdBoxController.CurrentSelectedBox.CurrentGridPos;

                            startPos = touchPosition;
                            isDragging = true;
                            accumulatedDelta = Vector2.zero; // Reset accumulated delta
                            lastDirectionTime = Time.unscaledTime; // Use unscaledTime for frame-rate independence
                        }
                    }
                    else
                    {
                        Debug.LogWarning("CrowdBoxController not found on parent GameObject.");
                    }
                }
                else
                {
                    Debug.LogWarning("Hit object does not have a CrowdBox component.");
                }
            }
        }
    }

    private void HandleTouchPosition(InputAction.CallbackContext ctx)
    {
        touchPosition = ctx.ReadValue<Vector2>();
    }

    private void HandleTouchDelta(InputAction.CallbackContext ctx)
    {
        if (!isDragging) return;

        Vector2 delta = ctx.ReadValue<Vector2>();
        
        // Scale delta threshold by time to make it frame-rate independent
        float scaledDeltaThreshold = deltaThreshold * Time.unscaledDeltaTime * 60f; // Normalized to 60fps
        
        // Only process if delta is significant enough
        if (delta.magnitude < scaledDeltaThreshold) return;

        // Accumulate delta for more stable direction detection
        accumulatedDelta += delta;

        // Use unscaledTime for frame-rate independent timing
        float timeSinceLastDirection = Time.unscaledTime - lastDirectionTime;
        
        // Check if we have enough accumulated movement and enough time has passed
        if (accumulatedDelta.magnitude >= dragThreshold && 
            timeSinceLastDirection >= directionCooldown)
        {
            Vector2Int direction = Utils.GetCardinalDirection(accumulatedDelta);
            
            // Only trigger if direction has changed or it's been a while since last direction
            if (direction != lastDetectedDirection || timeSinceLastDirection > directionCooldown * 2f)
            {
                OnDragDirectionDetected?.Invoke(direction);
                lastDetectedDirection = direction;
                lastDirectionTime = Time.unscaledTime; // Use unscaledTime
                
                // Reset accumulated delta after successful direction detection
                accumulatedDelta = Vector2.zero;
            }
        }
    }

    private void HandleDragMovement(Vector2Int dir)
    {
        Vector2Int currentGridPos = currentCrowdBoxController.CurrentSelectedBox.CurrentGridPos;
        Vector2Int nextGridPos = currentGridPos + dir;

        // Only move if we're targeting a different grid position than our last valid position
        if (nextGridPos != lastValidGridPos)
        {
            if (IsValidMove(currentGridPos, nextGridPos))
            {
                currentCrowdBoxController.HandleSelectedBoxMovement(nextGridPos);
                lastValidGridPos = nextGridPos;
            }
        }

        //Debug.Log($"Cell Grid: {nextGridPos}");
    }

    private void HandleTouchReleased(InputAction.CallbackContext context)
    {
        isDragging = false;
        accumulatedDelta = Vector2.zero;
    }

    /// <summary>
    /// Validates if the movement is allowed
    /// </summary>
    private bool IsValidMove(Vector2Int currentGridPos, Vector2Int targetGridPos)
    {
        // Check if target is blocked
        if (levelRuleTileManager.IsBlocked(targetGridPos))
        {
            return false;
        }

        // Check if the target is a hole
        if (levelRuleTileManager.IsHole(targetGridPos))
        {
            if (levelRuleTileManager.IsOurHole(targetGridPos, currentCrowdBoxController))
            {
                //jump into hole
                currentCrowdBoxController.JumpIntoHole(targetGridPos);
                return false;
            }

            return false;
        }

        return true;
    }

    //void OnGUI()
    //{
    //    // Create a custom style for larger, black text
    //    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
    //    labelStyle.fontSize = 50; // Increase font size (default is usually around 12-14)
    //    labelStyle.normal.textColor = Color.black; // Set text color to black
        
    //    GUI.Label(new Rect(100, 150, 1000, 1000), touchPosition.ToString(), labelStyle);
    //}
}
