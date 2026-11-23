using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public Transform destination;
    public bool isLinkActive = true;

    [SerializeField] private bool arrivalSideView;
    [SerializeField] private float arrivalHeightOffset;

    public static bool TryTeleport(Transform player, CameraPivotRotator camera)
    {
        if (player == null || !player.TryGetComponent(out Rigidbody body))
            return false;

        Collider[] playerColliders = body.GetComponents<Collider>();
        Teleporter source = null;
        float closestDistance = float.PositiveInfinity;

        foreach (Teleporter portal in FindObjectsByType<Teleporter>(FindObjectsSortMode.None))
        {
            if (!portal.isActiveAndEnabled || !portal.isLinkActive || portal.destination == null ||
                !portal.destination.TryGetComponent(out Teleporter _) ||
                !portal.TryGetComponent(out Collider trigger) || !trigger.enabled)
                continue;

            float distance = (portal.transform.position - body.position).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            foreach (Collider collider in playerColliders)
            {
                if (!collider.enabled || collider.isTrigger)
                    continue;

                if (Physics.ComputePenetration(
                    collider, body.position, body.rotation,
                    trigger, trigger.transform.position, trigger.transform.rotation, out _, out _))
                {
                    source = portal;
                    closestDistance = distance;
                    break;
                }
            }
        }

        if (source == null)
            return false;

        Teleporter exit = source.destination.GetComponent<Teleporter>();
        Vector3 arrival = exit.transform.position + Vector3.up * exit.arrivalHeightOffset;
        if (camera.UsesMapRotation)
        {
            camera.TurnThroughPortal(exit.arrivalSideView, arrival);
            return true;
        }

        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        Vector3 playerFraming = camera.CapturePlayerFraming();
        body.position = arrival;
        Physics.SyncTransforms();
        camera.RestorePortalView(exit.arrivalSideView, playerFraming);
        return true;
    }
}
