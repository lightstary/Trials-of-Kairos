using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
	private Animator anim;
	
	void Start()
	{
		anim = GetComponent<Animator>();
	}
	
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.UpArrow))
		{
			anim.SetTrigger("PushForward");
		}
		
		if(Input.GetKeyDown(KeyCode.DownArrow))
		{
			anim.SetTrigger("PushBack");
		}
		
		if(Input.GetKeyDown(KeyCode.LeftArrow))
		{
			anim.SetTrigger("PushLeft");
		}
		
		if(Input.GetKeyDown(KeyCode.RightArrow))
		{
			anim.SetTrigger("PushRight");
		}
	}
}
