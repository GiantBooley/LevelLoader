using System;
using System.Collections;
using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControl : MonoBehaviour
{

	public void SetSensitivity(float newSensitivity)
	{
		mouseSensitivity = newSensitivity;
	}

	public void PauseInput(float timeToPause)
	{
	}

	public void StopAnimator()
	{
	}

	public void StartAnimator()
	{
	}

	public void PlayOpeningAnimation()
	{
	}

	public void HammerReturn()
	{
	}

	public void Pause()
	{
	}

	public void UnPause()
	{
	}
	
	private void Start()
	{
		this.oldHammerPos = this.tip.position;
		this.mouseInput = Vector2.zero;
		this.motor = this.hj.motor;
		this.slider = this.sj.motor;
		this.motor.motorSpeed = 0f;
		this.slider.motorSpeed = 0f;
		this.hj.motor = this.motor;
		this.sj.motor = this.slider;
		this.fakeCursorRB = this.fakeCursor.GetComponent<Rigidbody2D>();
		this.mouseVelocityAverage = 0f;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		this.trackpad = true;
		mouseSensitivity = 1.5f;
	}

	private void Update()
	{
		this.fakeCursor.position = new Vector3(this.fakeCursor.position.x, this.fakeCursor.position.y, this.tip.position.z - 1f);
	}

	private void FixedUpdate()
	{
		FixedUpdateTest();
		return;
		float num = 0f;
		float num2 = 0f;

		// mouse input stuff
		AnimationCurve animationCurve = (this.trackpad ? this.trackpadCurve : this.mouseCurve);
		this.oldMouse = this.mouseInput;
		this.mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * this.mouseSensitivity;
		this.mouseInput *= animationCurve.Evaluate(this.mouseInput.magnitude);
		this.mouseInput = Vector2.Lerp(this.oldMouse, this.mouseInput, 0.5f);
		this.mouseVelocityAverage = Mathf.Lerp(this.mouseVelocityAverage, Mathf.Max(0.1f * this.mouseInput.magnitude, 0.001f), 0.05f) + 0.005f;

		// position vector stuff
		Vector2 vector4 = this.fakeCursorRB.position + this.mouseInput * this.mouseVelocityAverage;
		if (((Vector2)transform.position - vector4).magnitude > 3.5f)
		{
			vector4 = (Vector2)transform.position + (vector4 - (Vector2)transform.position).normalized * 3.5f;
		}
		Vector2 vector5 = (Vector2)tip.position - this.fakeCursorRB.position;
		num2 = Vector2.Dot((this.tip.position - this.hj.transform.position).normalized, vector5.normalized);
		num = vector5.magnitude * num2;
		this.oldHammerPos = this.tip.position;

		// math stuff
		vector4 += 0.5f * vector5 * Mathf.Clamp(0.2f - Mathf.Min(this.mouseVelocityAverage, 0.2f), 0f, 0.2f);
		this.fakeCursorRB.MovePosition(vector4);
		this.fakeCursorRB.velocity = vector4 - this.fakeCursorRB.position;
		Vector2 vector6 = (Vector2)hj.transform.position - this.fakeCursorRB.position;
		float num4 = -Mathf.DeltaAngle(57.29578f * Mathf.Atan2(vector6.y, vector6.x) - 180f - this.hj.referenceAngle, this.hj.jointAngle);
		num4 = num4 * Mathf.Abs(num4);
		this.motor.motorSpeed = Mathf.Clamp(num4 * 3f * Mathf.Pow(Mathf.Clamp01(vector6.magnitude / this.deadzone), 2f), -800f, 800f);
		float num5 = 16f - Mathf.Max(this.sj.reactionTorque * 0.001f, 5f);
		float num6 = Mathf.Pow(num2, 4f);
		this.slider.motorSpeed = Mathf.Clamp(-num * Mathf.Abs(num) * num5 * num6, -50f, 50f);
		this.hj.motor = this.motor;
		this.sj.motor = this.slider;
	}
	
	private void FixedUpdateTest()
	{
		AnimationCurve animationCurve = (this.trackpad ? this.trackpadCurve : this.mouseCurve);
		this.oldMouse = this.mouseInput;
		this.mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * this.mouseSensitivity;
		this.mouseInput *= animationCurve.Evaluate(this.mouseInput.magnitude);
		this.mouseInput = Vector2.Lerp(this.oldMouse, this.mouseInput, 0.5f);
		Vector2 vector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		Vector2 vector2 = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		Vector2 vector3 = Vector2.zero;
		if (vector.sqrMagnitude > 0.01f || vector2.sqrMagnitude > 0.01f)
		{
			vector3 = (vector + vector2) * this.stickCurve.Evaluate(vector.magnitude + vector2.magnitude);
		}
		if (vector3.sqrMagnitude > this.mouseInput.sqrMagnitude)
		{
			this.mouseInput = vector3;
		}
		this.mouseVelocityAverage = Mathf.Lerp(this.mouseVelocityAverage, Mathf.Max(0.1f * this.mouseInput.magnitude, 0.001f), 0.05f) + 0.005f;
		this.motor.motorSpeed = this.HingeMotorSpeed();
		this.slider.motorSpeed = this.SliderMotorSpeed();
		this.hj.motor = this.motor;
		this.sj.motor = this.slider;
		this.SetupFakeCursor(this.fakeCursorRB);
	}

	private Vector2 v2(Vector3 vector3)
	{
		return vector3;
	}

	private float HingeMotorSpeed()
	{
		Vector2 vector6 = this.v2(this.hj.transform.position) - this.fakeCursorRB.position;
		float num6 = -Mathf.DeltaAngle(57.29578f * Mathf.Atan2(vector6.y, vector6.x) - 180f - this.hj.referenceAngle, this.hj.jointAngle);
		num6 = Mathf.Sign(num6) * Mathf.Max(Mathf.Abs(num6 / 2f), num6 * num6);
		num6 *= Mathf.Clamp01(Mathf.Abs(num6) / this.angleEpsilon);
		return Mathf.Clamp(num6 * 3f * Mathf.Pow(Mathf.Clamp01(vector6.magnitude / this.deadzone), 2f), -800f, 800f);
	}

	private float SliderMotorSpeed()
	{
		Vector2 vector5 = this.v2(this.tip.position) - this.fakeCursorRB.position;
		float num7 = Vector2.Dot((this.tip.position - this.hj.transform.position).normalized, vector5.normalized);
		float num8 = vector5.magnitude * num7;
		float num9 = 16f - Mathf.Max(this.sj.reactionTorque * 0.001f, 5f);
		float num10 = Mathf.Pow(num7, 4f);
		return Mathf.Clamp(-num8 * Mathf.Abs(num8) * num9 * num10, -50f, 50f);
	}

	private void SetupFakeCursor(Rigidbody2D fakeCursor)
	{
		Vector2 vector4 = fakeCursor.position + this.mouseInput * this.mouseVelocityAverage;
		if ((this.v2(base.transform.position) - vector4).magnitude > 3.5f)
		{
			vector4 = this.v2(base.transform.position) + (vector4 - this.v2(base.transform.position)).normalized * 3.5f;
		}
		vector4 += 0.5f * (this.v2(this.tip.position) - fakeCursor.position) * Mathf.Clamp(0.2f - Mathf.Min(this.mouseVelocityAverage, 0.2f), 0f, 0.2f);
		fakeCursor.MovePosition(vector4);
		fakeCursor.velocity = vector4 - fakeCursor.position;
	}

	private float mouseSensitivity = 1.5f;
	
	private float angleEpsilon = 1f;
	
	private Vector3 oldHammerPos;

	private Rigidbody2D fakeCursorRB;

	private float mouseVelocityAverage;

	private JointMotor2D motor;

	private JointMotor2D slider;
	
	private Vector2 mouseInput;

	private Vector2 oldMouse;

	public HingeJoint2D hj;

	public SliderJoint2D sj;

	public Transform tip;

	public float deadzone;

	public Transform fakeCursor;

	public Transform potMesh;

	public bool trackpad;

	public AnimationCurve trackpadCurve;

	public AnimationCurve mouseCurve;

	public AnimationCurve stickCurve;

	public bool loadedFromSave;

	public bool loadFinished;

}