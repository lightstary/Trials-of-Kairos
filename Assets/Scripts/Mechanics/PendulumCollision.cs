using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PendulumCollision : MonoBehaviour
{
    public float hitForce = 10f;
	public MonoBehaviour playerMovementScript;
	private Rigidbody rb;
	
	void Start()
	{
		rb = GetComponent<Rigidbody>();
	}
	
	void OnCollisionEnter(Collision collision)
	{
		if(collision.gameObject.CompareTag("Pendulum"))
		{
			Vector3 direction = (transform.position - collision.contacts[0].point).normalized;
			direction.y = 0.5f;
			
			rb.AddForce(direction * hitForce, ForceMode.Impulse);
			
			if(playerMovementScript != null)
			{
				playerMovementScript.enabled = false;
				Invoke("EnableMovement", 0.5f);
			}
		}
	}
	
	void EnableMovement()
	{
		playerMovementScript.enabled = true;
	}
}
