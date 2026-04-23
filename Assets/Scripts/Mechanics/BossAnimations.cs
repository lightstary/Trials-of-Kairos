using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAnimations : MonoBehaviour
{
	[SerializeField] private Vector3 _rotation;
	[SerializeField] private float _speed;
	
	void Update()
	{
		transform.Rotate(_rotation * _speed * Time.deltaTime);
	}
}
