
using System;
using JetBrains.Annotations;
using UnityEngine;

public class Viewport : MonoBehaviour
{
    private Camera _camera;
    private Transform _transform;
    private GameObject _gameObject;
    private ExternalWindow _window;
    private Rect _startRect;

    public bool Docked
    {
        get { return _window == null; }
    }

    public void Dock()
    {
        _camera.rect = _startRect;
        if (_window != null)
        {
            _camera.targetTexture = null;
            _window.OnMoved -= OnWindowMoved;
            _window.OnClose -= OnWindowClosed;

            _window.Dispose();
            _window = null;
        }
    }

    public void Undock()
    {
        _camera.rect = new Rect(0f, 0f, 1f, 1f);
        _window = WindowManager.Instance.CreateWindow("Window", 1024, 768, false);
        _camera.targetTexture = _window.RenderTexture;
        _window.OnClose += OnWindowClosed;
        _window.OnMoved += OnWindowMoved;
        _window.Drag();
    }

    public bool CursorInViewport()
    {
        Vector3 mousePos = Input.mousePosition;
        return _camera.pixelRect.Contains(mousePos);
    }
    
    public void Initialise(Rect rect, Color backgroundColour)
    {
        _transform = transform;
        _gameObject = gameObject;
        _camera = _gameObject.AddComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.Color;
        _camera.backgroundColor = backgroundColour;
        _camera.allowMSAA = false;
        _camera.allowHDR = false;
        _camera.renderingPath = RenderingPath.Forward;
        _camera.rect = rect;
        _startRect = rect;

        _transform.position = new Vector3(-3f, 3f, -3f);
        _transform.LookAt(Vector3.zero);
    }

    private void OnWindowClosed(object sender, EventArgs args)
    {
        _camera.rect = _startRect;
        _camera.targetTexture = null;
        _window = null;
    }

    private void OnWindowMoved(int mouseX, int mouseY, bool cursorInUnityWindow)
    {
        if (cursorInUnityWindow)
        {
            Dock();
        }
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        Dock();
    }
}
