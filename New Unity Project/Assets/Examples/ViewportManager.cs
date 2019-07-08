using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class ViewportManager : MonoBehaviour
{
    private HashSet<Viewport> _viewports;
    private Viewport _draggedViewport;

    [UsedImplicitly]
    private void Awake()
    {
        _viewports = new HashSet<Viewport>();

        CreateViewport(new Rect(0f, 0f, 0.49f, 0.49f), Color.blue * 0.5f);
        CreateViewport(new Rect(0f, 0.51f, 0.49f, 0.49f), Color.green * 0.5f);
        CreateViewport(new Rect(0.51f, 0f, 0.49f, 0.49f), Color.red * 0.5f);
        CreateViewport(new Rect(0.51f, 0.51f, 0.49f, 0.49f), Color.yellow * 0.5f);
    }

    [UsedImplicitly]
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            foreach (Viewport viewport in _viewports)
            {
                if (viewport.CursorInViewport())
                {
                    _draggedViewport = viewport;
                    break;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _draggedViewport = null;
        }

        if (_draggedViewport != null && _draggedViewport.Docked)
        {
            Vector3 mousePos = Input.mousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            {
                _draggedViewport.Undock();
                _draggedViewport = null;
            }
        }
    }

    public void CreateViewport(Rect rect, Color backgroundColour)
    {
        GameObject viewportObject = new GameObject("Viewport");
        Viewport viewport = viewportObject.AddComponent<Viewport>();
        viewport.Initialise(rect, backgroundColour);
        _viewports.Add(viewport);
    }
}
