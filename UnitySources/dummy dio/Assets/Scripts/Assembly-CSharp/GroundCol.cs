using System;
using UnityEngine;

public class GroundCol : MonoBehaviour
{
	public enum SoundMaterial
	{
		rock,
		wood,
		metal,
		plastic,
		furniture,
		snow,
		cardboard,
		none,
		snake,
		solidmetal
	}

	public Color groundCol = new Color(0.7f, 0.6f, 0.3f);

	public GroundCol.SoundMaterial material;
}
