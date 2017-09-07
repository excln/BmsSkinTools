using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventToEvent : MonoBehaviour
{

	public UnityEvent e;

	public void Run()
	{
		if (e != null)
			e.Invoke();
	}
}
