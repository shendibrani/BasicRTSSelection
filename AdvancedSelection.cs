using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;

public class AdvancedSelection : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask selectableLayer;
    public Color hoverColor = new Color(1, 0.8f, 0, 0.5f);
    public Color selectedColor = new Color(0, 1, 0, 0.5f);
    public Color selectionBoxColor = new Color(1, 1, 1, 0.2f);

    private Vector2 selectionStart;
    private Rect selectionRect;
    private bool isSelecting;
    private List<GameObject> selectedObjects = new List<GameObject>();
    private GameObject hoveredObject;
    private Plane[] cameraFrustumPlanes;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleSelectionInput();
        HandleHover();
        UpdateCameraFrustum();
    }

    void OnGUI()
    {
        if (isSelecting)
        {
            DrawSelectionBox();
        }
    }

    void UpdateCameraFrustum()
    {

        cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
    }

    void HandleSelectionInput()
    {

        // Start selection
        if (Input.GetMouseButtonDown(0))
        {
            selectionStart = Input.mousePosition;
            isSelecting = true;
        }

        // During selection
        if (isSelecting)
        {
            Vector2 currentPos = Input.mousePosition;
            selectionRect = new Rect(
                Mathf.Min(selectionStart.x, currentPos.x),
                Mathf.Min(Screen.height - selectionStart.y, Screen.height - currentPos.y),
                Mathf.Abs(currentPos.x - selectionStart.x),
                Mathf.Abs(currentPos.y - selectionStart.y)
            );

            // Finalize selection
            if (Input.GetMouseButtonUp(0))
            {
                if (IsClick())
                {
                    HandleSingleClick();
                }
                else
                {
                    Profiler.BeginSample("Selection");
                    FinalizeSelection();
                    Profiler.EndSample();
                }
                isSelecting = false;
            }
        }
    }

    bool IsClick()
    {
        return selectionRect.width < 5f && selectionRect.height < 5f;
    }

    void HandleSingleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
        {
            // Single object selection
            ClearSelection();
            selectedObjects.Add(hit.collider.gameObject);
            SetObjectColor(hit.collider.gameObject, selectedColor);
        }
        else
        {
            // Clicked empty space - clear all
            ClearSelection();
        }
    }

    void ClearSelection()
    {
        foreach (GameObject obj in selectedObjects)
        {
            ResetObjectColor(obj);
        }
        selectedObjects.Clear();
    }

    void FinalizeSelection()
    {
        ClearSelection();
        Collider[] allColliders = Physics.OverlapSphere(Vector3.zero, float.MaxValue, selectableLayer);

        foreach (Collider col in allColliders)
        {
            if (IsInSelection(col, selectionRect))
            {
                selectedObjects.Add(col.gameObject);
                SetObjectColor(col.gameObject, selectedColor);
            }
        }
    }

    bool IsInSelection(Collider col, Rect selection)
    {
        Bounds bounds = col.bounds;
        if (!GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, bounds)) return false;

        Vector3[] corners = GetBoundsCorners(bounds);
        Rect screenRect = GetScreenRect(corners);

        return selection.Overlaps(screenRect, true);
    }

    Vector3[] GetBoundsCorners(Bounds bounds)
    {
        return new Vector3[] {
            bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z)
        };
    }

    Rect GetScreenRect(Vector3[] worldCorners)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        Camera cam = Camera.main;

        foreach (Vector3 corner in worldCorners)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(corner);
            if (screenPos.z < 0) continue; // Behind camera

            // Convert to GUI coordinates (Y inverted)
            screenPos.y = Screen.height - screenPos.y;

            min.x = Mathf.Min(min.x, screenPos.x);
            min.y = Mathf.Min(min.y, screenPos.y);
            max.x = Mathf.Max(max.x, screenPos.x);
            max.y = Mathf.Max(max.y, screenPos.y);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
        {
            GameObject newHover = hit.collider.gameObject;

            if (newHover != hoveredObject)
            {
                if (hoveredObject != null && !selectedObjects.Contains(hoveredObject))
                    ResetObjectColor(hoveredObject);

                hoveredObject = newHover;

                if (hoveredObject != null && !selectedObjects.Contains(hoveredObject))
                    SetObjectColor(hoveredObject, hoverColor);
            }
        }
        else
        {
            if (hoveredObject != null && !selectedObjects.Contains(hoveredObject))
                ResetObjectColor(hoveredObject);
            hoveredObject = null;
        }
    }

    void DrawSelectionBox()
    {
        GUI.color = selectionBoxColor;
        GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void SetObjectColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = color;
            }
        }
    }

    void ResetObjectColor(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = Color.white;
            }
        }
    }
}