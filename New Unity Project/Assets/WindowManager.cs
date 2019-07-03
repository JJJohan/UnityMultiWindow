using UnityEngine;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.UI;

public class WindowManager : MonoBehaviour
{
    [DllImport("UnityWindowPlugin")]
    private static extern void InitPlugin(
        [MarshalAs(UnmanagedType.FunctionPtr)] MessageDelegate messageCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] CloseDelegate closeCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] ResizeDelegate resizeCalllback);
    
    [DllImport("UnityWindowPlugin")]
    private static extern void ShutdownPlugin();

    [DllImport("UnityWindowPlugin")]
    private static extern IntPtr CreateNewWindow(string title, int width, int height, bool resizeable, IntPtr texturePtr);

    [DllImport("UnityWindowPlugin")]
    private static extern void UpdateWindows();
    
    [DllImport("UnityWindowPlugin")]
    private static extern void DisposeWindow(IntPtr windowHandle);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void MessageDelegate(string message);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void CloseDelegate(IntPtr window);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr ResizeDelegate(IntPtr window, int width, int height);

    private static Dictionary<IntPtr, RenderTexture> _windows;

    public Camera SecondCamera;
    public RawImage rawImage;
    public static Camera SecondCameraStatic;

    [UsedImplicitly]
    private void Awake()
    {
        _windows = new Dictionary<IntPtr, RenderTexture>();
        InitPlugin(MessageCallback, CloseCallback, ResizeCallback);
        SecondCameraStatic = SecondCamera;
    }
    
    private void CreateWindow(string title, int width, int height, bool resizable)
    {
        RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        SecondCamera.targetTexture = texture;
        IntPtr texturePtr = texture.GetNativeTexturePtr();

        IntPtr window = CreateNewWindow(title, width, height, resizable, texturePtr);
        if (window == null)
        {
            Debug.LogError("Failed to create new window.");
            return;
        }

        _windows.Add(window, texture);
    }

    [UsedImplicitly]
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateWindow("Hello", 1024, 768, true);
        }

        UpdateWindows();
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        foreach(RenderTexture texture in _windows.Values)
        {
            Destroy(texture);
        }
        _windows.Clear();

        ShutdownPlugin();
    }

    private static IntPtr ResizeCallback(IntPtr window, int width, int height)
    {
        RenderTexture texture;
        if (!_windows.TryGetValue(window, out texture))
        {
            Debug.LogError("Received a window resize callback, but no matching window could be found.");
            return IntPtr.Zero;
        }
        
        if (texture.width != width || texture.height != height)
        {
            Destroy(texture);
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _windows[window] = texture;
            SecondCameraStatic.targetTexture = texture;
            return texture.GetNativeTexturePtr();
        }

        return IntPtr.Zero;
    }

    private static void CloseCallback(IntPtr window)
    {
        RenderTexture texture;
        if (!_windows.TryGetValue(window, out texture))
        {
            Debug.LogError("Received a window close callback, but no matching window could be found.");
            return;
        }

        DisposeWindow(window);
        Destroy(texture);
        _windows.Remove(window);
    }

    private static void MessageCallback(string message)
    {
        Debug.Log(message);
    }
}
