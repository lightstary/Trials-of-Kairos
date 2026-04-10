using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Normal Follow Settings")]
    public Vector3 offset = new Vector3(0.5f, 4f, -6f);
    public float followSpeed = 5f;

    [Header("Look Settings")]
    public Vector3 lookOffset = new Vector3(0f, 1f, 0f);
    public float rotateSpeed = 5f;

    [Header("Zoom Mode (E key)")]
    public Vector3 zoomOffset = new Vector3(0.3f, 1.5f, -2.5f);
    public float zoomFollowSpeed = 8f;
    public float mouseSensitivity = 2f;

    private bool isZoomed = false;
    private float yaw = 0f;
    private float pitch = 20f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            isZoomed = !isZoomed;
            Cursor.lockState = isZoomed ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isZoomed;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (isZoomed)
        {
            // Mouse look in zoom mode press "e" for now or whatever clay wants
            yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch  = Mathf.Clamp(pitch, -20f, 60f);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 targetPosition = player.position + rotation * zoomOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, zoomFollowSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation((player.position + lookOffset) - transform.position),
                rotateSpeed * Time.deltaTime);
        }
        else
        {
            // Normal over the shoulder follow
            Vector3 targetPosition = player.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation((player.position + lookOffset) - transform.position),
                rotateSpeed * Time.deltaTime);
        }
    }
}