using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

public class Saviour : MonoBehaviour
{

	public void ResetPlayerButNotDialogue()
	{
	}

	public void SaveGameNow(bool writeToDisk)
	{
	}


	public SaveState Save()
	{
		SaveState saveState = new SaveState();
		return saveState;
	}

	public bool LoadNewestSave()
	{
		return true;
	}

	public void Load(SaveState loadedSave)
	{
	}

	public Narrator narrator;

	public Rigidbody2D hammer;

	public SliderJoint2D slider;

	public HingeJoint2D hinge;

	public Rigidbody2D cursor;

	public HingeJoint2D hubJoint;

	public Transform playerTransform;

	public PlayerControl pc;

	public CameraControl cc;

	public bool canReset;

	public Transform spine1;

	public Transform spine2;

	public float targetMaxDelta = 0.06666667f;
}
