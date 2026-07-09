using FluffyUnderware.Curvy;
using UnityEngine;

public class CameraControl : MonoBehaviour
{

	public void EnableBlur(bool enable)
	{
	}

	public void FadeIn()
	{
	}

	public void FadeOut()
	{
	}


	public void FixedUpdate()
	{
		Vector3 pos = Vector3.Lerp(transform.position, player.transform.position, 0.05f);
		pos.z = -20;
		transform.position = pos;
	}


	public void Teleport(Vector3 newPosition)
	{
	}

	public GameObject water;

	public GameObject player;

	public ProgressMeter progressMeter;

	public bool loadFinished;

	public Texture2D fadeTex;

	public CurvySpline spline;

}
