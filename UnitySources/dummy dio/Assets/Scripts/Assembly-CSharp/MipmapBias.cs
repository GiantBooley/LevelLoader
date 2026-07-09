using System;
using UnityEngine;

public class MipmapBias : MonoBehaviour
{
	private void Start()
	{
		Texture[] array = this.textures;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].mipMapBias = this.bias;
		}
	}

	public Texture[] textures;

	private float bias = -1f;
}
