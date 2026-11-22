using UnityEngine;

// Runs after the pivot finishes following/turning and before rendering.
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class CloseThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform player;
    [SerializeField] private CameraOccludingWall[] walls;
    [SerializeField] private Collider[] floors;
    [SerializeField, Min(0.01f)] private float floorClearance = 0.05f;
    private Vector3 authoredLensPosition;
    private bool initialized;


    private void Start()
    {
        authoredLensPosition = viewCamera.transform.localPosition;
        initialized = true;

    }

    private void LateUpdate()
    {
        if (viewCamera == null || player == null) return;
        ProtectFloor();
        // Fade only geometry between the lens and the player's body.
        if (walls != null)
            foreach (CameraOccludingWall wall in walls)
                if (wall != null) wall.SetOccluded(wall.OccludesPlayer(viewCamera, player));
    }

    private void ProtectFloor()
    {
        if (!initialized || !viewCamera.orthographic || floors == null) return;
        viewCamera.transform.localPosition = authoredLensPosition;

        Vector3 forward = viewCamera.transform.forward;
        if (forward.y >= -0.01f) return;
        // Keep the entire bottom of the near plane above the floor surface.
        // Pull back along the optical axis; orthographic framing stays unchanged.
        float floorY = float.NegativeInfinity;
        foreach (Collider floor in floors)
            if (floor != null && floor.enabled)
                floorY = Mathf.Max(floorY, floor.bounds.max.y);
        if (float.IsNegativeInfinity(floorY)) return;
        Vector3 bottomLeft = viewCamera.ViewportToWorldPoint(new Vector3(0f, 0f, viewCamera.nearClipPlane));
        Vector3 bottomRight = viewCamera.ViewportToWorldPoint(new Vector3(1f, 0f, viewCamera.nearClipPlane));
        float missingHeight = floorY + floorClearance - Mathf.Min(bottomLeft.y, bottomRight.y);
        if (missingHeight > 0f)
            viewCamera.transform.position -= forward * (missingHeight / -forward.y);
    }

    private void OnDisable()
    {
        if (walls != null)
            foreach (CameraOccludingWall wall in walls)
                if (wall != null) wall.Restore();
    }
}
