using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("Event/Multi Window Input Module")]
/// <summary>
/// A BaseInputModule designed for mouse / keyboard / controller input.
/// </summary>
/// <remarks>
/// Input module for working with, mouse, keyboard, or controller.
/// </remarks>
public class MultiWindowInputModule : PointerInputModule
{
    private float _prevActionTime;
    private Vector2 _lastMoveVector;
    private int _consecutiveMoveCount;
    private Vector2 _lastMousePosition;
    private Vector2 _mousePosition;

    private GameObject _currentFocusedGameObject;
    private PointerEventData _inputPointerEvent;
    private readonly bool[] _mouseButtonPressed;
    private readonly MouseState _mouseState;
    private ExternalWindow _activeWindow;

    public static MultiWindowInputModule Instance { get; private set; }

    public ExternalWindow ActiveWindow
    {
        get { return _activeWindow; }
        set
        {
            if (_activeWindow == value)
            {
                return;
            }

            _activeWindow = value;

            HashSet<Canvas> canvases = new HashSet<Canvas>(FindObjectsOfType<Canvas>());
            ExternalWindow[] windows = WindowManager.Instance.GetAllWindows();
            for (int i = 0; i < windows.Length; ++i)
            {
                ExternalWindow window = windows[i];
                bool activeWindow = window == value;

                HashSet<Canvas> windowCanvases = window.GetAssociatedCanvases();
                foreach (Canvas windowCanvas in windowCanvases)
                {
                    if (canvases.Contains(windowCanvas))
                    {
                        GraphicRaycaster raycaster = windowCanvas.GetComponent<GraphicRaycaster>();
                        if (raycaster != null)
                        {
                            raycaster.enabled = activeWindow;
                        }

                        canvases.Remove(windowCanvas);
                        break;
                    }
                }
            }

            // Iterate over canvases not associated to external windows.
            bool mainWindow = value == null;
            foreach (Canvas windowCanvas in canvases)
            {
                GraphicRaycaster raycaster = windowCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = mainWindow;
                }
            }
        }
    }

    protected MultiWindowInputModule()
    {
        Instance = this;
        _mouseButtonPressed = new bool[3];
        _mouseState = new MouseState();
    }

    [SerializeField]
    private string _horizontalAxis = "Horizontal";

    /// <summary>
    /// Name of the vertical axis for movement (if axis events are used).
    /// </summary>
    [SerializeField]
    private string _verticalAxis = "Vertical";

    /// <summary>
    /// Name of the submit button.
    /// </summary>
    [SerializeField]
    private string _submitButton = "Submit";

    /// <summary>
    /// Name of the submit button.
    /// </summary>
    [SerializeField]
    private string _cancelButton = "Cancel";

    [SerializeField]
    private float _inputActionsPerSecond = 10;

    [SerializeField]
    private float _repeatDelay = 0.5f;

    [SerializeField]
    private bool _forceModuleActive;

    /// <summary>
    /// Force this module to be active.
    /// </summary>
    /// <remarks>
    /// If there is no module active with higher priority (ordered in the inspector) this module will be forced active even if valid enabling conditions are not met.
    /// </remarks>
    public bool ForceModuleActive
    {
        get { return _forceModuleActive; }
        set { _forceModuleActive = value; }
    }

    /// <summary>
    /// Number of keyboard / controller inputs allowed per second.
    /// </summary>
    public float InputActionsPerSecond
    {
        get { return _inputActionsPerSecond; }
        set { _inputActionsPerSecond = value; }
    }

    /// <summary>
    /// Delay in seconds before the input actions per second repeat rate takes effect.
    /// </summary>
    /// <remarks>
    /// If the same direction is sustained, the InputActionsPerSecond property can be used to control the rate at which events are fired. However, it can be desirable that the first repetition is delayed, so the user doesn't get repeated actions by accident.
    /// </remarks>
    public float RepeatDelay
    {
        get { return _repeatDelay; }
        set { _repeatDelay = value; }
    }

    /// <summary>
    /// Name of the horizontal axis for movement (if axis events are used).
    /// </summary>
    public string HorizontalAxis
    {
        get { return _horizontalAxis; }
        set { _horizontalAxis = value; }
    }

    /// <summary>
    /// Name of the vertical axis for movement (if axis events are used).
    /// </summary>
    public string VerticalAxis
    {
        get { return _verticalAxis; }
        set { _verticalAxis = value; }
    }

    /// <summary>
    /// Maximum number of input events handled per second.
    /// </summary>
    public string SubmitButton
    {
        get { return _submitButton; }
        set { _submitButton = value; }
    }

    /// <summary>
    /// Input manager name for the 'cancel' button.
    /// </summary>
    public string CancelButton
    {
        get { return _cancelButton; }
        set { _cancelButton = value; }
    }

    private bool ShouldIgnoreEventsOnNoFocus()
    {
        switch (SystemInfo.operatingSystemFamily)
        {
            case OperatingSystemFamily.Windows:
            case OperatingSystemFamily.Linux:
            case OperatingSystemFamily.MacOSX:
#if UNITY_EDITOR
                if (EditorApplication.isRemoteConnected)
                    return false;
#endif
                return true;
            default:
                return false;
        }
    }

    public override void UpdateModule()
    {
        if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus() && ActiveWindow == null)
        {
            if (_inputPointerEvent != null && _inputPointerEvent.pointerDrag != null && _inputPointerEvent.dragging)
            {
                ReleaseMouse(_inputPointerEvent, _inputPointerEvent.pointerCurrentRaycast.gameObject);
            }

            _inputPointerEvent = null;

            return;
        }

        _lastMousePosition = _mousePosition;
        _mousePosition = GetMousePos();
    }

    private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
    {
        ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

        GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

        // PointerClick and Drop events
        if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
        }
        else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
        {
            ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
        }

        pointerEvent.eligibleForClick = false;
        pointerEvent.pointerPress = null;
        pointerEvent.rawPointerPress = null;

        if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

        pointerEvent.dragging = false;
        pointerEvent.pointerDrag = null;

        // redo pointer enter / exit to refresh state
        // so that if we moused over something that ignored it before
        // due to having pressed on something else
        // it now gets it.
        if (currentOverGo != pointerEvent.pointerEnter)
        {
            HandlePointerExitAndEnter(pointerEvent, null);
            HandlePointerExitAndEnter(pointerEvent, currentOverGo);
        }

        _inputPointerEvent = pointerEvent;
    }

    public override bool IsModuleSupported()
    {
        return _forceModuleActive || input.mousePresent || input.touchSupported;
    }

    public override bool ShouldActivateModule()
    {
        if (!base.ShouldActivateModule())
            return false;

        bool shouldActivate = _forceModuleActive;
        shouldActivate |= input.GetButtonDown(_submitButton);
        shouldActivate |= input.GetButtonDown(_cancelButton);
        shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(_horizontalAxis), 0.0f);
        shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(_verticalAxis), 0.0f);
        shouldActivate |= (_mousePosition - _lastMousePosition).sqrMagnitude > 0.0f;
        shouldActivate |= input.GetMouseButtonDown(0);

        if (input.touchCount > 0)
            shouldActivate = true;

        return shouldActivate;
    }

    private Vector2 GetMousePos()
    {
        return ActiveWindow == null ? (Vector2)Input.mousePosition : ActiveWindow.MousePosition;
    }

    /// <summary>
    /// See BaseInputModule.
    /// </summary>
    public override void ActivateModule()
    {
        if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus() && ActiveWindow == null)
            return;

        base.ActivateModule();
        _mousePosition = GetMousePos();
        _lastMousePosition = GetMousePos();

        GameObject toSelect = eventSystem.currentSelectedGameObject;
        if (toSelect == null)
            toSelect = eventSystem.firstSelectedGameObject;

        eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
    }

    /// <summary>
    /// See BaseInputModule.
    /// </summary>
    public override void DeactivateModule()
    {
        base.DeactivateModule();
        ClearSelection();
    }

    public override void Process()
    {
        if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus() && ActiveWindow == null)
            return;

        bool usedEvent = SendUpdateEventToSelectedObject();

        // case 1004066 - touch / mouse events should be processed before navigation events in case
        // they change the current selected gameobject and the submit button is a touch / mouse button.

        // touch needs to take precedence because of the mouse emulation layer
        if (!ProcessTouchEvents() && input.mousePresent)
            ProcessMouseEvent();

        if (eventSystem.sendNavigationEvents)
        {
            if (!usedEvent)
                usedEvent |= SendMoveEventToSelectedObject();

            if (!usedEvent)
                SendSubmitEventToSelectedObject();
        }
    }

    private bool ProcessTouchEvents()
    {
        for (int i = 0; i < input.touchCount; ++i)
        {
            Touch touch = input.GetTouch(i);

            if (touch.type == TouchType.Indirect)
                continue;

            bool released;
            bool pressed;
            PointerEventData pointer = GetTouchPointerEventData(touch, out pressed, out released);

            ProcessTouchPress(pointer, pressed, released);

            if (!released)
            {
                ProcessMove(pointer);
                ProcessDrag(pointer);
            }
            else
                RemovePointerData(pointer);
        }
        return input.touchCount > 0;
    }

    /// <summary>
    /// This method is called by Unity whenever a touch event is processed. Override this method with a custom implementation to process touch events yourself.
    /// </summary>
    /// <param name="pointerEvent">Event data relating to the touch event, such as position and ID to be passed to the touch event destination object.</param>
    /// <param name="pressed">This is true for the first frame of a touch event, and false thereafter. This can therefore be used to determine the instant a touch event occurred.</param>
    /// <param name="released">This is true only for the last frame of a touch event.</param>
    /// <remarks>
    /// This method can be overridden in derived classes to change how touch press events are handled.
    /// </remarks>
    protected void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
    {
        GameObject currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

        // PointerDown notification
        if (pressed)
        {
            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, pointerEvent);

            if (pointerEvent.pointerEnter != currentOverGo)
            {
                // send a pointer enter to the touched element if it isn't the one to select...
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                pointerEvent.pointerEnter = currentOverGo;
            }

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            GameObject newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // Debug.Log("Pressed: " + newPressed);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                float diffTime = time - pointerEvent.clickTime;
                if (diffTime < 0.3f)
                    ++pointerEvent.clickCount;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = currentOverGo;

            pointerEvent.clickTime = time;

            // Save the drag handler as well
            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

            _inputPointerEvent = pointerEvent;
        }

        // PointerUp notification
        if (released)
        {
            // Debug.Log("Executing pressup on: " + pointer.pointerPress);
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

            // see if we mouse up on the same element that we clicked on...
            GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
            }
            else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // send exit events as we need to simulate this on touch up on touch device
            ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
            pointerEvent.pointerEnter = null;

            _inputPointerEvent = pointerEvent;
        }
    }

    /// <summary>
    /// Calculate and send a submit event to the current selected object.
    /// </summary>
    /// <returns>If the submit event was used by the selected object.</returns>
    protected bool SendSubmitEventToSelectedObject()
    {
        if (eventSystem.currentSelectedGameObject == null)
            return false;

        BaseEventData data = GetBaseEventData();
        if (input.GetButtonDown(_submitButton))
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

        if (input.GetButtonDown(_cancelButton))
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
        return data.used;
    }

    private Vector2 GetRawMoveVector()
    {
        Vector2 move = Vector2.zero;
        move.x = input.GetAxisRaw(_horizontalAxis);
        move.y = input.GetAxisRaw(_verticalAxis);

        if (input.GetButtonDown(_horizontalAxis))
        {
            if (move.x < 0)
                move.x = -1f;
            if (move.x > 0)
                move.x = 1f;
        }
        if (input.GetButtonDown(_verticalAxis))
        {
            if (move.y < 0)
                move.y = -1f;
            if (move.y > 0)
                move.y = 1f;
        }
        return move;
    }

    /// <summary>
    /// Calculate and send a move event to the current selected object.
    /// </summary>
    /// <returns>If the move event was used by the selected object.</returns>
    protected bool SendMoveEventToSelectedObject()
    {
        float time = Time.unscaledTime;

        Vector2 movement = GetRawMoveVector();
        if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
        {
            _consecutiveMoveCount = 0;
            return false;
        }

        bool similarDir = (Vector2.Dot(movement, _lastMoveVector) > 0);

        // If direction didn't change at least 90 degrees, wait for delay before allowing consequtive event.
        if (similarDir && _consecutiveMoveCount == 1)
        {
            if (time <= _prevActionTime + _repeatDelay)
                return false;
        }
        // If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
        else
        {
            if (time <= _prevActionTime + 1f / _inputActionsPerSecond)
                return false;
        }

        AxisEventData axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);

        if (axisEventData.moveDir != MoveDirection.None)
        {
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
            if (!similarDir)
                _consecutiveMoveCount = 0;
            _consecutiveMoveCount++;
            _prevActionTime = time;
            _lastMoveVector = movement;
        }
        else
        {
            _consecutiveMoveCount = 0;
        }

        return axisEventData.used;
    }

    protected void ProcessMouseEvent()
    {
        ProcessMouseEvent(0);
    }

    private void UpdateMousePressState(MouseState mouseData, MouseButtonEventData buttonData, PointerEventData.InputButton button, bool pressed)
    {
        PointerEventData.FramePressState pressState;

        bool wasPressed = _mouseButtonPressed[(int)button];
        if (pressed == wasPressed)
        {
            pressState = PointerEventData.FramePressState.NotChanged;
        }
        else if (pressed)
        {
            pressState = PointerEventData.FramePressState.Pressed;
        }
        else
        {
            pressState = PointerEventData.FramePressState.Released;
        }

        _mouseButtonPressed[(int)button] = pressed;
        mouseData.SetButtonState(button, pressState, buttonData.buttonData);
    }

    /// <summary>
    /// Process all mouse events.
    /// </summary>
    protected void ProcessMouseEvent(int id)
    {
        MouseState mouseData = GetMousePointerEventData(id);
        MouseButtonEventData leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;
        MouseButtonEventData middleButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData;
        MouseButtonEventData rightButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData;

        if (ActiveWindow != null)
        {
            WindowMouseButton buttonStates = ActiveWindow.MouseButton;
            UpdateMousePressState(mouseData, leftButtonData, PointerEventData.InputButton.Left, (buttonStates & WindowMouseButton.Left) == WindowMouseButton.Left);
            UpdateMousePressState(mouseData, middleButtonData, PointerEventData.InputButton.Middle, (buttonStates & WindowMouseButton.Middle) == WindowMouseButton.Middle);
            UpdateMousePressState(mouseData, rightButtonData, PointerEventData.InputButton.Right, (buttonStates & WindowMouseButton.Right) == WindowMouseButton.Right);
        }
        else
        {
            _mouseButtonPressed[0] = false;
            _mouseButtonPressed[1] = false;
            _mouseButtonPressed[2] = false;
        }

        _currentFocusedGameObject = leftButtonData.buttonData.pointerCurrentRaycast.gameObject;

        // Process the first mouse button fully
        ProcessMousePress(leftButtonData);
        ProcessMove(leftButtonData.buttonData);
        ProcessDrag(leftButtonData.buttonData);

        // Now process right / middle clicks
        ProcessMousePress(rightButtonData);
        ProcessDrag(rightButtonData.buttonData);
        ProcessMousePress(middleButtonData);
        ProcessDrag(middleButtonData.buttonData);

        if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
        {
            GameObject scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
            ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
        }
    }

    protected bool SendUpdateEventToSelectedObject()
    {
        if (eventSystem.currentSelectedGameObject == null)
            return false;

        BaseEventData data = GetBaseEventData();
        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
        return data.used;
    }

    /// <summary>
    /// Calculate and process any mouse button state changes.
    /// </summary>
    protected void ProcessMousePress(MouseButtonEventData data)
    {
        PointerEventData pointerEvent = data.buttonData;
        GameObject currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

        // PointerDown notification
        if (data.PressedThisFrame())
        {
            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
            pointerEvent.position = GetMousePos();

            DeselectIfSelectionChanged(currentOverGo, pointerEvent);

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            GameObject newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // Debug.Log("Pressed: " + newPressed);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                float diffTime = time - pointerEvent.clickTime;
                if (diffTime < 0.3f)
                    ++pointerEvent.clickCount;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = currentOverGo;

            pointerEvent.clickTime = time;

            // Save the drag handler as well
            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

            _inputPointerEvent = pointerEvent;
        }

        // PointerUp notification
        if (data.ReleasedThisFrame())
        {
            ReleaseMouse(pointerEvent, currentOverGo);
        }
    }

    protected GameObject GetCurrentFocusedGameObject()
    {
        return _currentFocusedGameObject;
    }

    protected override MouseState GetMousePointerEventData(int id)
    {
        PointerEventData leftData;
        bool created = GetPointerData(kMouseLeftId, out leftData, true);

        leftData.Reset();

        Vector2 screenSpaceCursorPosition = GetMousePos();

        if (created)
            leftData.position = screenSpaceCursorPosition;

        Vector2 pos = screenSpaceCursorPosition;
        leftData.delta = pos - leftData.position;
        leftData.position = pos;
        leftData.scrollDelta = Input.mouseScrollDelta;
        leftData.button = PointerEventData.InputButton.Left;
        eventSystem.RaycastAll(leftData, m_RaycastResultCache);
        RaycastResult raycast = FindFirstRaycast(m_RaycastResultCache);
        leftData.pointerCurrentRaycast = raycast;
        m_RaycastResultCache.Clear();

        // copy the apropriate data into right and middle slots
        PointerEventData rightData;
        GetPointerData(kMouseRightId, out rightData, true);
        CopyFromTo(leftData, rightData);
        rightData.button = PointerEventData.InputButton.Right;

        PointerEventData middleData;
        GetPointerData(kMouseMiddleId, out middleData, true);
        CopyFromTo(leftData, middleData);
        middleData.button = PointerEventData.InputButton.Middle;

        _mouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
        _mouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
        _mouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);

        return _mouseState;
    }
}