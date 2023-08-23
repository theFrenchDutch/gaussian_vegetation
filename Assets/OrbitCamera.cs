using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class OrbitCamera : MonoBehaviour
{
	public GameObject target;
	public Vector3 offset = Vector3.zero;
	public float distance = 10.0f;
	public float maxDistance = 100.0f;

	public float xSpeed = 250.0f;
	public float ySpeed = 120.0f;

	public float yMinLimit = -20;
	public float yMaxLimit = 80;

	public float x = 0.0f;
	public float y = 0.0f;

	public bool disableControls = false;

	private float oldDistance;
	private Vector3 oldOffset;
	private float oldX;
	private float oldY;

	void Start()
	{
		var angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
	}

	float prevDistance;

	void Update()
	{
		// Manual animation
		//x = math.pow(math.cos(Time.timeSinceLevelLoad * 0.5f) * 0.5f + 0.5f, 1.0f) * 90.0f + 45.0f;
		//y = math.pow(math.cos(Time.timeSinceLevelLoad * 0.15f) * 0.5f + 0.5f, 1.0f) * 140.0f - 70.0f;
		//distance = math.pow(math.cos(Time.timeSinceLevelLoad * 0.3f) * 0.5f + 0.5f, 1.0f) * 0.9f + 0.6f;

		if (disableControls == false)
		{
			distance -= Input.GetAxis("Mouse ScrollWheel") * distance;

			if (Input.GetKey(KeyCode.UpArrow))
				distance -= 0.001f * distance;
			if (Input.GetKey(KeyCode.DownArrow))
				distance += 0.001f * distance;
			if (Input.GetKey(KeyCode.RightArrow))
				x -= 0.1f;
			if (Input.GetKey(KeyCode.LeftArrow))
				x += 0.1f;

			if (Input.GetKey(KeyCode.Keypad8))
				offset.z += 0.01f;
			if (Input.GetKey(KeyCode.Keypad2))
				offset.z -= 0.01f;
			if (Input.GetKey(KeyCode.Keypad6))
				offset.x += 0.01f;
			if (Input.GetKey(KeyCode.Keypad4))
				offset.x -= 0.01f;
			if (Input.GetKey(KeyCode.Keypad9))
				offset.y += 0.001f;
			if (Input.GetKey(KeyCode.Keypad3))
				offset.y -= 0.001f;
		}
		distance = Mathf.Min(distance, maxDistance);

		if (target && (disableControls == true || Input.GetMouseButton(1) || oldDistance != distance || oldOffset != offset || oldX != x || oldY != y))
		{
			var pos = Input.mousePosition;
			var dpiScale = 1f;
			if (Screen.dpi < 1) dpiScale = 1;
			if (Screen.dpi < 200) dpiScale = 1;
			else dpiScale = Screen.dpi / 200f;

			//if (pos.x < 380 * dpiScale && Screen.height - pos.y < 250 * dpiScale) return;

			// comment out these two lines if you don't want to hide mouse curser or you have a UI button 
			//Cursor.visible = false;
			//Cursor.lockState = CursorLockMode.Locked;

			if (Input.GetMouseButton(1) && disableControls == false)
			{
				x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
				y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
			}

			y = ClampAngle(y, yMinLimit, yMaxLimit);
			var rotation = Quaternion.Euler(y, x, 0);
			var position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.transform.position + offset;
			transform.rotation = rotation;
			transform.position = position;

		}

		if (Mathf.Abs(prevDistance - distance) > 0.0f)
		{
			prevDistance = distance;
			var rot = Quaternion.Euler(y, x, 0);
			var po = rot * new Vector3(0.0f, 0.0f, -distance) + target.transform.position + offset;
			transform.rotation = rot;
			transform.position = po;
		}

		oldDistance = distance;
		oldOffset = offset;
		oldX = x;
		oldY = y;
	}

	static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp(angle, min, max);
	}
}