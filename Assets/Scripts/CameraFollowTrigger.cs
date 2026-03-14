using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowTrigger : MonoBehaviour
{
    public CameraPivotRotator.CameraMovingType changeToType = CameraPivotRotator.CameraMovingType.Follow;
    public bool oneTimeOnly = true;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && oneTimeOnly) return;

        if (other.CompareTag("Player"))
        {
            CameraPivotRotator.Instance.cameraType = changeToType;
            var playerscript1 = other.GetComponent<PlayerMovement>();
            var playerscript3 = other.GetComponent<PlayerMovement3>();
            playerscript1.enabled = false;
            playerscript3.enabled = true;
            triggered = true;

            Debug.Log("[CameraTriggerZone] 카메라 모드 변경됨 → " + changeToType);
        }
    }
}
