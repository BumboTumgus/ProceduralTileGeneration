using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;

    private Vector3 cameraPosition;

    private void FixedUpdate()
    {
        cameraPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, transform.position.z);
        transform.position = cameraPosition;
    }
}
