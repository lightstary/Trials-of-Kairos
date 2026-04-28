using UnityEngine;

/// <summary>
/// Marker component for vine-covered tiles that kill the player on contact.
/// Stepping onto one triggers the fall modal for checkpoint/restart options.
/// FallDetection raycasts for this component to identify lethal tiles.
/// </summary>
public class DeathTile : MonoBehaviour
{
    // Intentionally empty — FallDetection raycasts for this component
    // to determine if the player is standing on a death tile.
}
