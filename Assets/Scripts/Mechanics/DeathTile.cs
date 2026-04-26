using UnityEngine;

/// <summary>
/// Marker component for tiles that kill the player on contact.
/// These are placeholder "vine" tiles that will eventually be replaced
/// with vine meshes. Stepping onto one triggers the Game Over screen.
/// </summary>
public class DeathTile : MonoBehaviour
{
    // Intentionally empty — FallDetection raycasts for this component
    // to determine if the player is standing on a death tile.
}
