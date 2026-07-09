using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class FogControl : MonoBehaviour
{
	public MeshRenderer sky;

	public Light sun;

	public AnimationCurve blendCurve;

	public ColorSet[] colorSets;
}
