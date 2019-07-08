using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    [DllImport("UnityWindowPlugin")]
    private static extern void InitPlugin(
        [MarshalAs(UnmanagedType.FunctionPtr)] MessageDelegate messageCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] CloseDelegate closeCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] ResizeDelegate resizeCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] MouseUpdateDelegate mouseCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] MoveDelegate moveCallback);
    
    [DllImport("UnityWindowPlugin")]
    private static extern void ShutdownPlugin();

    [DllImport("UnityWindowPlugin")]
    private static extern IntPtr CreateNewWindow(string title, int width, int height, bool resizeable, IntPtr texturePtr);

    [DllImport("UnityWindowPlugin")]
    private static extern void UpdateWindows();
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void MessageDelegate(string message);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void CloseDelegate(IntPtr window);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr ResizeDelegate(IntPtr window, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void MouseUpdateDelegate(IntPtr window, int mouseX, int mouseY, uint mouseButtonMask);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void MoveDelegate(IntPtr window, int mouseX, int mouseY, bool cursorInUnityWindow);

    private static Dictionary<long, ExternalWindow> _windows;
    private static ExternalWindow _focusedWindow;
    
    public ExternalWindow[] GetAllWindows()
    {
        return _windows.Values.ToArray();
    }

    public static WindowManager Instance { get; private set; }

    [UsedImplicitly]
    private void Awake()
    {
        Instance = this;
        _windows = new Dictionary<long, ExternalWindow>();
        InitPlugin(MessageCallback, CloseCallback, ResizeCallback, MouseUpdateCallback, MoveCallback);
    }
    
    public ExternalWindow CreateWindow(string title, int width, int height, bool resizable)
    {
        RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        texture.Create();
        IntPtr texturePtr = texture.GetNativeTexturePtr();

        IntPtr windowHandle = CreateNewWindow(title, width, height, resizable, texturePtr);
        if (windowHandle == IntPtr.Zero)
        {
            Debug.LogError("Failed to create new window.");
            return null;
        }

        ExternalWindow window = new ExternalWindow(windowHandle, texture);
        long windowAddress = windowHandle.ToInt64();

        // In rare cases a duplicate window can be produced (despite all happening on the same thread), this catches it.
        ExternalWindow existing;
        if (_windows.TryGetValue(windowAddress, out existing))
        {
            existing.Dispose();
            _windows[windowAddress] = window;
        }
        else
        {
            _windows.Add(windowAddress, window);
        }
        return window;
    }

    [UsedImplicitly]
    private void Update()
    {
        _focusedWindow = null;
        
        UpdateWindows();

        MultiWindowInputModule.Instance.ActiveWindow = _focusedWindow;
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        Instance = null;
        foreach(ExternalWindow window in _windows.Values)
        {
            window.Dispose();
        }
        _windows.Clear();

        ShutdownPlugin();
    }
    
    private static IntPtr ResizeCallback(IntPtr windowHandle, int width, int height)
    {
        ExternalWindow window;
        if (!_windows.TryGetValue(windowHandle.ToInt64(), out window))
        {
            Debug.LogError("Received a window resize callback, but no matching window could be found.");
            return IntPtr.Zero;
        }

        return window.Resize(width, height);
    }

    private static void MouseUpdateCallback(IntPtr windowHandle, int mouseX, int mouseY, uint mouseButtonMask)
    {
        ExternalWindow window;
        if (!_windows.TryGetValue(windowHandle.ToInt64(), out window))
        {
            Debug.LogError("Received a window mouse callback, but no matching window could be found.");
            return;
        }

        window.MousePosition = new Vector2(mouseX, mouseY);
        window.MouseButton = (WindowMouseButton)mouseButtonMask;
        _focusedWindow = window;
    }

    private static void CloseCallback(IntPtr windowHandle)
    {
        ExternalWindow window;
        long ptrValue = windowHandle.ToInt64();
        if (!_windows.TryGetValue(ptrValue, out window))
        {
            Debug.LogError("Received a window close callback, but no matching window could be found.");
            return;
        }

        if (window == _focusedWindow)
        {
            _focusedWindow = null;
        }

        window.Dispose();
        _windows.Remove(ptrValue);
    }

    private static void MoveCallback(IntPtr windowHandle, int mouseX, int mouseY, bool cursorInUnityWindow)
    {
        ExternalWindow window;
        long ptrValue = windowHandle.ToInt64();
        if (!_windows.TryGetValue(ptrValue, out window))
        {
            Debug.LogError("Received a window move callback, but no matching window could be found.");
            return;
        }

        window.Moved(mouseX, mouseY, cursorInUnityWindow);
    }

    private static void MessageCallback(string message)
    {
        Debug.Log(message);
    }
}
