using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSounds : MonoBehaviour
{
	public AudioClip[] hardStrainSounds;

	public AudioClip[] strainSounds;

	public AudioClip[] releaseSounds;

	public AudioClip[] painSounds;

	public AudioClip[] impactSounds;

	public AudioClip[] sadSounds;

	public AudioClip[] grunts;

	public AudioClip[] jumpSounds;

	public AudioClip[] whuSounds;

	public HingeJoint2D hj;

	public SliderJoint2D sj;

	public Transform hammerTransform;

	public LayerMask fallLayer;

	public Image strainbar;

	public Image deltaStrainbar;

	public Image fallBar;

	public Image jerkBar;

	public bool isTouching;

	public SkinnedMeshRenderer skin;

	public Transform neck;

	public PoseControl pose;
}
