using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;

[Flags]
public enum WindowMouseButton
{
    None = 0,
    Left = 1,
    Middle = 2,
    Right = 4
}

public delegate void WindowMovedHandler(int mouseX, int mouseY, bool cursorInUnityWindow);

public class ExternalWindow : IDisposable
{
    [DllImport("UnityWindowPlugin")]
    private static extern void DisposeWindow(IntPtr windowHandle);
    
    [DllImport("UnityWindowPlugin")]
    private static extern void SetWindowPosition(IntPtr windowHandle, int x, int y);

    [DllImport("UnityWindowPlugin")]
    private static extern void DragWindow(IntPtr windowHandle);

    private IntPtr _windowHandle;
    private readonly HashSet<Canvas> _canvases;

    public event EventHandler OnClose;
    public event WindowMovedHandler OnMoved;

    public RenderTexture RenderTexture { get; private set; }
    public Vector2 MousePosition { get; set; }
    public WindowMouseButton MouseButton { get; set; }

    internal ExternalWindow(IntPtr windowHandle, RenderTexture renderTexture)
    {
        _windowHandle = windowHandle;
        RenderTexture = renderTexture;
        _canvases = new HashSet<Canvas>();
    }

    internal IntPtr Resize(int width, int height)
    {
        if (RenderTexture.width == width && RenderTexture.height == height)
        {
            return RenderTexture.GetNativeDepthBufferPtr();
        }

        Camera targetCamera = null;
        Camera[] cameras = Object.FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; ++i)
        {
            Camera cam = cameras[i];
            if (cam.targetTexture == RenderTexture)
            {
                targetCamera = cam;
                break;
            }
        }

        Object.Destroy(RenderTexture);
        RenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        
        if (targetCamera != null)
        {
            targetCamera.targetTexture = RenderTexture;
        }

        return RenderTexture.GetNativeTexturePtr();
    }

    public void AssociateCanvas(Canvas canvas)
    {
        _canvases.Add(canvas);
    }

    public void DisassociateCanvas(Canvas canvas)
    {
        _canvases.Remove(canvas);
    }

    public HashSet<Canvas> GetAssociatedCanvases()
    {
        return _canvases;
    }

    public void SetPosition(int x, int y)
    {
        SetWindowPosition(_windowHandle, x, y);
    }

    public void Drag()
    {
        DragWindow(_windowHandle);
    }

    internal void Moved(int mouseX, int mouseY, bool cursorInUnityWindow)
    {
        if (OnMoved != null)
        {
            OnMoved(mouseX, mouseY, cursorInUnityWindow);
        }
    }

    public void Dispose()
    {
        if (OnClose != null)
        {
            OnClose(this, EventArgs.Empty);
        }

        if (RenderTexture != null)
        {
            Object.Destroy(RenderTexture);
            RenderTexture = null;
        }

        if (_windowHandle != IntPtr.Zero)
        {
            DisposeWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;
        }
    }
}
