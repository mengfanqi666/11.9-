using UnityEngine;
using UnityEngine.EventSystems;

public class IKDragController : MonoBehaviour
{
    public JointChain chain;
    public Camera cam;
    public float planeZ = 0f;

    private bool dragging;
    private Plane plane;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
    }

    void Update()
    {
        if (chain == null || cam == null || chain.Tip == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.collider != null && hit.collider.transform == chain.Tip)
                    dragging = true;
            }
        }

        if (dragging && Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 p = ray.GetPoint(enter);
                p.z = planeZ;
                chain.SetTarget(p);
            }
        }

        if (Input.GetMouseButtonUp(0))
            dragging = false;
    }
}