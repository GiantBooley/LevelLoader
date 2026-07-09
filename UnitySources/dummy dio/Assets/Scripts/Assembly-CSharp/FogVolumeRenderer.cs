using System;
using UnityEngine;

[ExecuteInEditMode]
public class FogVolumeRenderer : MonoBehaviour
{
	public enum BlendMode
	{
		PremultipliedTransparency = 1,
		TraditionalTransparency = 5
	}

	[SerializeField]
	[Serializable]
	public enum UpsampleMode
	{
		DOWNSAMPLE_MIN,
		DOWNSAMPLE_MAX,
		DOWNSAMPLE_CHESSBOARD
	}

	public enum UpsampleMaterialPass
	{
		DEPTH_DOWNSAMPLE,
		BILATERAL_UPSAMPLE
	}

	public void setDownsample(int val)
	{
	}

	public bool showBilateralEdge { get; set; }

	public void ShowBilateralEdge(bool b)
	{
	}

	public bool useBilateralUpsampling { get; set; }


	public FogVolumeRenderer.UpsampleMode upsampleMode { get; set; }

	public RenderTextureReadWrite GetRTReadWrite()
	{
		return RenderTextureReadWrite.Default;
	}

	public RenderTextureFormat GetRTFormat()
	{
		return RenderTextureFormat.DefaultHDR;
	}


	public void ReleaseRT(RenderTexture rt)
	{
	}


	public bool ShowCamera;

	public string FogVolumeResolution;

	public FogVolumeRenderer.BlendMode _BlendMode = FogVolumeRenderer.BlendMode.PremultipliedTransparency;

	public bool GenerateDepth = true;


	[SerializeField]
	[Range(0f, 8f)]
	public int _Downsample = 1;

	public bool _showBilateralEdge;


	public bool _useBilateralUpsampling = true;

	public FogVolumeRenderer.UpsampleMode _upsampleMode = FogVolumeRenderer.UpsampleMode.DOWNSAMPLE_MAX;

	[SerializeField]
	[Range(0f, 0.01f)]
	public float upsampleDepthThreshold = 0.00187f;

	public bool HDR;

	public bool TAA;

	public FogVolumeTAA _TAA;

	[SerializeField]
	public int DepthLayer2;

	public RenderTexture[] lowProfileDepthRT;

	public RenderTexture[] lowProfileDepthRRT;

	public bool SceneBlur = true;
}
