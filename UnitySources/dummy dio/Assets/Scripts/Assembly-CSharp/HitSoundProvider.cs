using System;
using System.Collections.Generic;
using UnityEngine;

public class HitSoundProvider : MonoBehaviour
{
	public AudioClip GetHit(GroundCol.SoundMaterial mat)
	{
		return cardboardhardhits[0];
	}

	public AudioClip GetHardHit(GroundCol.SoundMaterial mat)
	{
		return cardboardhardhits[0];
	}

	public AudioClip GetScrape(GroundCol.SoundMaterial mat)
	{
		return cardboardhardhits[0];
	}

	public List<AudioClip> rockhits;

	public List<AudioClip> woodhits;

	public List<AudioClip> metalhits;

	public List<AudioClip> plastichits;

	public List<AudioClip> furniturehits;

	public List<AudioClip> snowhits;

	public List<AudioClip> cardboardhits;

	public List<AudioClip> snakehits;

	public List<AudioClip> solidmetalhits;

	public List<AudioClip> rockhardhits;

	public List<AudioClip> woodhardhits;

	public List<AudioClip> metalhardhits;

	public List<AudioClip> plastichardhits;

	public List<AudioClip> furniturehardhits;

	public List<AudioClip> snowhardhits;

	public List<AudioClip> cardboardhardhits;

	public List<AudioClip> snakehardhits;

	public List<AudioClip> solidmetalhardhits;

	public List<AudioClip> rockscrapes;

	public List<AudioClip> woodscrapes;

	public List<AudioClip> metalscrapes;

	public List<AudioClip> plasticscrapes;

	public List<AudioClip> furniturescrapes;

	public List<AudioClip> snowscrapes;

	public List<AudioClip> cardboardscrapes;

	public List<AudioClip> snakescrapes;

	public List<AudioClip> solidmetalscrapes;
}
