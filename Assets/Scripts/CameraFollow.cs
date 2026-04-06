using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Position Settings")]
    public Vector3 offset = new Vector3(0f, 6f, -8f); 
    public float followSpeed = 5f;

    [Header("Look Settings")]
    public Vector3 lookOffset = new Vector3(0f, 1f, 0f); 
    public float rotateSpeed = 5f;

    void LateUpdate()
    {
        if (player == null) return;

        
        Vector3 targetPosition = player.position + offset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation((player.position + lookOffset) - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }
}