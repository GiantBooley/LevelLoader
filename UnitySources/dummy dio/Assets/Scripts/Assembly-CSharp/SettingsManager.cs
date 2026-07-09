using System;
using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using LetterboxCamera;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
	public void SetLanguage(int newLang)
	{
	}

	public void OnLanguageChanged(int newLanguage)
	{
	}

	public void SetSFXVolume(float newVolume)
	{
	}

	public void SetVoiceoverVolume(float newVolume)
	{
	}

	public void SetMusicVolume(float newVolume)
	{
	}

	public void SetMouseSensitivity(float newSensitivity)
	{
	}

	public void ToggleMenu()
	{
	}

	public void ToggleMotionBlur(bool mb)
	{
	}

	public void ToggleFullscreen(bool fs)
	{
	}

	public void ToggleVsync(bool vs)
	{
	}

	public void ToggleCursor()
	{
	}

	public void ToggleTrackpad(bool shouldEnable)
	{
	}

	public void ToggleSubtitles()
	{
	}

	public void ApplyResolution()
	{
	}

	public void ChangeQuality(int changeBy = 1)
	{
	}


	public void QuitGame()
	{
	}

	public void SetResByIndex(int changeTo = 0)
	{
	}

	public void Ouch(int i)
	{
	}

	public GameObject menu;

	public TMP_Dropdown resolutionDropdown;

	public TextMeshProUGUI currentQualityText;

	public Button currentQualityButton;

	public Button currentQualityIncrementButton;

	public Button currentQualityDecrementButton;

	public Toggle cursorToggle;

	public Toggle trackpadToggle;

	public Toggle subtitleToggle;

	public Toggle fullscreenToggle;

	public Toggle motionBlurToggle;

	public Toggle vsyncToggle;

	public Button applyResolutionButton;

	public Slider mouseSensitivitySlider;

	public Slider SFXVolumeSlider;

	public Slider MusicVolumeSlider;

	public Slider VOVolumeSlider;

	public GameObject player;

	public GameObject cursor;

	public GameObject narrator;

	public RestartOnContact underwaterDetector;

	public GameObject menuMobile;

	public Toggle subtitleToggleMobile;

	public Slider mouseSensitivitySliderMobile;

	public Slider SFXVolumeSliderMobile;

	public Slider MusicVolumeSliderMobile;

	public Slider VOVolumeSliderMobile;

	public AudioMixer mixer;

	public ForceCameraRatio ratioFitter;

	public PostProcessProfile bgProfile;

	public PostProcessProfile bgProfile_low;

	public PostProcessProfile fgProfile;

	public PostProcessProfile fgProfile_low;

	public PostProcessProfile fgProfile_lowest;

	public Camera fgCam;

	public Camera bgCam;

	public bool canMenu;

	public Toggle[] languageToggles;

	public Material potMat;
}
