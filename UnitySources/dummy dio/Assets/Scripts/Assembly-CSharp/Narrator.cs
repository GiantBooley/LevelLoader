using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy;
using TMPro;
using UnityEngine;

public class Narrator : MonoBehaviour
{
	[Serializable]
	public class DialogBit
	{
		public string textLine;

		public AudioClip audioLine;

		public bool heard;

		public float splinePos;

		public bool isMusic;

		public bool inUse;

		public bool isObservation;
	}

	public void Pause()
	{
	}

	public void UnPause()
	{
	}

	public void setDialogDoneLists(byte[] keyDialogDone, byte[] observationDialogDone, byte[] condolenceDialogDone)
	{
	}

	public byte[] getKeyDialogDoneList()
	{
		return new byte[0];
	}

	public byte[] getObservationDialogDoneList()
	{
		return new byte[0];
	}

	public byte[] getCondolenceDialogDoneList()
	{
		return new byte[0];
	}

	public void SetLanguage(int newlang)
	{
	}

	public void ToggleSubtitles(bool show)
	{
	}

	public void SayDialog(Narrator.DialogBit bit)
	{
	}

	public void SlowProgress()
	{
	}

	public void FastRetreat()
	{
	}

	public void StuckOnAntenna()
	{
	}

	public void UpdateDistance(float d)
	{
	}

	public TextMeshProUGUI Subtitles;

	public List<Narrator.DialogBit> Condolences2 = new List<Narrator.DialogBit>(1);

	public List<Narrator.DialogBit> Observations2 = new List<Narrator.DialogBit>(1);

	public List<Narrator.DialogBit> KeyDialog;

	public CurvySpline levelSpline;

	public Transform keyDialogContainer;

	public int lang;

	public AudioSource DJ;

	public AudioSource VO;

	public AudioSource Piano;

	public float timePlayedThisGame;

	public AudioClip mouseClip;

	public AudioClip antennaClip;

	public AudioClip speedClip;

	public bool speedrun;
}
