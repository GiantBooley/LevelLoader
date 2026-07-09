using System;
using FluffyUnderware.Curvy;
using UnityEngine;

public class PoseControl : MonoBehaviour
{

	public void StopAnimator()
	{
	}

	public void StartAnimator()
	{
	}

	public void PlayOpeningAnimation()
	{
	}
	public void EnableHammer(bool enable)
	{
	}

	public AnimationClip wakeup;

	public Animator anim;

	public Animator potAnim;

	public Transform leftHand;

	public Transform rightHand;

	public Transform leftElbow;

	public Transform rightElbow;

	public Transform center;

	public Transform handle;

	public Transform gripCenterLeft;

	public Transform gripCenterRight;

	public Transform rightHandGripCenter;

	public Transform leftHandGripCenter;

	public SliderJoint2D slider;

	public HingeJoint2D hinge;

	public Vector3 off;

	public Transform lookTarget;

	public Transform interestingItemParent;

	public float blendAmt;

	public float handBlend;

	public PlayerControl pc;

	public CurvySpline spline;

	public Transform dudeMeshHub;

	public Transform hammerMeshHub;

	public Transform potMeshHub;

	public Transform tip;

}
