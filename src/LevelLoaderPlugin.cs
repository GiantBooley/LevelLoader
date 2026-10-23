using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using FlourModLoader;

namespace LevelLoader {

	public class LevelInfo {
		public readonly string title;
		public readonly Dictionary<string, string> metadata;
		public Texture2D thumbnail;

		public LevelInfo(string title) {
			this.title = title;
			this.metadata = new Dictionary<string, string>();
			this.thumbnail = null;
		}
		public LevelInfo(string title, Dictionary<string, string> metadata) {
			this.title = title;
			this.metadata = metadata;
			this.thumbnail = null;
		}
	};


	[BepInPlugin("org.flour.levelloader", "Level Loader", "0.1.0")]
	[BepInProcess("GettingOverIt.exe")]
	[BepInDependency("org.flour.flourmodloader")]
	public class LevelLoaderPlugin : FlourMod {
		internal static new ManualLogSource Logger;
		public static LevelLoaderPlugin Instance;

		public override string Author => "Flour";
		public override string Description => "Level loader for loading bundle maps";

		// BepInEx config
		private ConfigEntry<string> config_selectedLevel;
		private ConfigEntry<int> config_viewMode;
		

		private LevelInfo _currentLevel;
		private GameObject _levelSelectMenu;
		private readonly string _pluginFolder = "BepInEx/plugins/LevelLoader";
		private List<LevelInfo> _levels;

		private string _sceneName;
		private AssetBundle _bundle;
		private byte[] _bundleFileHash;
		public List<string> loadedMapAssemblies;
		
		private string _dummySceneName = "DummyDio";
		private AssetBundle _dummyBundle;

		private readonly string _levelLoaderUIBundleName = "levelloaderui.assets";
		private AssetBundle _levelLoaderUIBundle;
		private GameObject _levelLoaderPanelPrefab;
		private GameObject _templateLevelCardPrefab;
		private GameObject _templateLevelListItemPrefab;
		private Texture2D _missingThumbnail;

		private Loader _loader;
		public bool isCustomMap;
		// prevent multiple scene switches at once
		public bool isSwitchingScene;

		private static readonly string[] ValidMetadataExtensions = {"mdata", "txt"};

		private void Awake() {
			// Singleton
			Instance = this;

			// Plugin startup logic
			Logger = base.Logger;
			Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

			// BepInEx config
			config_selectedLevel = Config.Bind("Level Loader", "SelectedLevel", "", "Title of the current selected level");
			config_viewMode = Config.Bind("Level Loader", "ViewMode", 0, "Grid if set to 0, list if set to 1");

			// Harmony patches
			var harmony = new Harmony("goi.flowerpot.levelloader");
			harmony.PatchAll();

			_levels = new List<LevelInfo>();

			// Create level folder if it doesn't exist
			string levelsFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "Levels");
			if (!Directory.Exists(levelsFolder)) Directory.CreateDirectory(levelsFolder);

			// Initialize members
			isCustomMap = false;
			isSwitchingScene = false;
			loadedMapAssemblies = new List<string>();

			// Load template prefabs
			_levelLoaderUIBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, _levelLoaderUIBundleName));
			_levelLoaderPanelPrefab = _levelLoaderUIBundle.LoadAsset<GameObject>("LevelLoader.prefab");
			_templateLevelCardPrefab = _levelLoaderUIBundle.LoadAsset<GameObject>("TemplateLevelCard.prefab");
			_templateLevelListItemPrefab = _levelLoaderUIBundle.LoadAsset<GameObject>("TemplateLevelListItem.prefab");

			_missingThumbnail = _levelLoaderUIBundle.LoadAsset<Texture2D>("MissingThumbnail.png");
		}
		public override void OnGameStateChange(GameState oldState, GameState newState, Scene scene) {
			switch (newState) {
				case GameState.MainMenu:
					_loader = GameObject.Find("LoaderLogic").GetComponent<Loader>();

					// Add Select Level button
					GameObject selectLevelObject = CreateMenuButton("Select Level", _loader.menu.transform, 0);
					Button selectLevelButton = selectLevelObject.GetComponent<Button>();
					selectLevelButton.onClick.AddListener(ToggleLevelSelectMenu);

					GetLevelsFromFolder();
					AddLevelSelector();

					isCustomMap = false;
					break;
				case GameState.InGame:
					isCustomMap = scene.name != "Mian";
					if (!isCustomMap) return;

					// ============ Map metadata/txt file ==============================
					/*
					 * credit: added
					 * description: added
					 * scene: added
					 * mode: not added
					 * sky: added
					 * lighting: not tested
					 *
					 * cam: added
					 * zplane: not added
					 * farplane: added
					 * bgfarplane: added
					 *
					 * fog: not added
					 *
					 * shadow: not added
					 * hammermat: not added
					 */
					// =================== Camera ======================
					// Set far plane of Camera to farplane
					if (_currentLevel.metadata.ContainsKey("farplane") && float.TryParse(_currentLevel.metadata["farplane"], out float farPlane)) {
						GOIConstants.mainCamera?.GetComponent<Camera>()?.farClipPlane = farPlane;
					}
					// Set camera type to perspective if cam=1
					if (_currentLevel.metadata.ContainsKey("cam") && _currentLevel.metadata["cam"] == "1") {
						GOIConstants.mainCamera?.GetComponent<Camera>()?.orthographic = false;
					}

					// Set BGCamera background to black
					Camera bgCam = GOIConstants.mainCamera?.transform?.Find("BGCamera")?.GetComponent<Camera>();
					bgCam?.backgroundColor = Color.black;
					// Disable BGCamera>Sky if sky=r
					if (_currentLevel.metadata.ContainsKey("sky") && _currentLevel.metadata["sky"] == "r") {
						Transform sky = GOIConstants.mainCamera?.transform?.Find("BGCamera")?.Find("Sky");
						if (sky != null) {
							sky?.gameObject?.SetActive(false);
						}
					}
					// Set far plane of BGCamera to bgfarplane
					if (_currentLevel.metadata.ContainsKey("bgfarplane") && float.TryParse(_currentLevel.metadata["bgfarplane"], out float bgFarPlane)) {
						bgCam?.farClipPlane = bgFarPlane;
					}


					// =================== Player ================
					// Teleport to startPos
					GameObject startPos = GameObject.Find("startPos");
					if (startPos == null) {
						// default map start pos
						TeleportPlayer(new Vector2(-44.292595f, -2.4218144f), true);
					} else {
						TeleportPlayer(new Vector2(startPos.transform.position.x, startPos.transform.position.y), true);
					}


					// ======= Other fixes ================
					// Disable hammer doppler
					GOIConstants.player?.transform?.Find("Hub")?.Find("Slider")?.Find("Handle")?.Find("PoleMiddle")?.Find("Tip")?.gameObject?.GetComponent<AudioSource>()?.dopplerLevel = 0f;
					// Solution for ending in space then joining map
					Physics2D.gravity = new Vector2(0f, -30f);

					break;
			}
			if (newState != GameState.MainMenu) {
				_currentLevel.thumbnail = null;
				_levels.Clear();
			}
		}
		private void GetLevelsFromFolder() {
			_levels.Clear();
			string levelsFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "Levels");
			string[] filePaths = Directory.GetFiles(levelsFolder);
			foreach (string scenePath in filePaths) {
				if (!scenePath.EndsWith(".scene")) continue;

				string levelName = Path.GetFileNameWithoutExtension(scenePath);

				// get metadata file
				Dictionary<string, string> metadata = new Dictionary<string, string>();
				foreach (string extension in ValidMetadataExtensions) {
					string metadataPath = Path.Combine(levelsFolder, levelName + "." + extension);
					if (File.Exists(metadataPath)) {
						metadata = ParseMetadataFile(metadataPath);
						break;
					}
				}
				_levels.Add(new LevelInfo(levelName, metadata));
			}
		}
		private GameObject CreateMenuButton(string text, Transform parent, int siblingIndex=-1) {
			if (_loader == null) {
				Logger.LogWarning("Loader is null");
				return null;
			}
			// Create levels button by copying credits button
			GameObject menuButtonObject = UnityEngine.Object.Instantiate(_loader.menu.Find("Credits").gameObject, parent, false);
			if (siblingIndex != -1) menuButtonObject.transform.SetSiblingIndex(siblingIndex);

			// remove click event
			Button menuButton = menuButtonObject.GetComponent<Button>();
			menuButton.onClick = new Button.ButtonClickedEvent();

			// set text
			GameObject textObject = menuButton.transform.Find("Text").gameObject;
			TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
			Localize localize = textObject.GetComponent<Localize>();
			if (localize != null) UnityEngine.Object.Destroy(localize);
			textComponent.SetText(text);

			return menuButtonObject;
		}
		private GameObject CreateMenuText(string text, Transform parent, int siblingIndex=-1) {
			if (_loader == null) {
				Logger.LogWarning("Loader is null");
				return null;
			}
			// create text
			GameObject menuTextObject = UnityEngine.Object.Instantiate(_loader.menu.Find("Credits").Find("Text").gameObject, parent, false);
			if (siblingIndex != -1) menuTextObject.transform.SetSiblingIndex(siblingIndex);

			// set text
			TextMeshProUGUI textComponent = menuTextObject.GetComponent<TextMeshProUGUI>();
			Localize localize = menuTextObject.GetComponent<Localize>();
			if (localize != null) UnityEngine.Object.Destroy(localize);
			textComponent.SetText(text);

			return menuTextObject;
		}
		private void ToggleLevelSelectMenu() {
			if (_levelSelectMenu == null) return;
			_levelSelectMenu.SetActive(!_levelSelectMenu.activeSelf);
		}
		private string ConvertLevelMetadataToReadableTMPString(LevelInfo level) {
			Dictionary<string, string> metadata = level.metadata;

			// Game
			List<string> gameDescriptions = new List<string>();
			if (metadata.ContainsKey("scene")) gameDescriptions.Add("Scene: " + metadata["scene"]);
			bool replacementMap = true;
			if (metadata.ContainsKey("mode") && metadata["mode"] == "a") replacementMap = false;
			if (replacementMap) {
				gameDescriptions.Add("Replacement map");
			} else {
				gameDescriptions.Add("<color=\"yellow\">[!] Add map</color>");
			}
			if (metadata.ContainsKey("lighting") && metadata["lighting"] == "r") gameDescriptions.Add("Replace lighting");
			if (metadata.ContainsKey("fog")) gameDescriptions.Add("<color=\"yellow\">[!] Fog color: " + metadata["fog"] + "</color>");

			// Camera
			List<string> cameraDescriptions = new List<string>();
			if (metadata.ContainsKey("sky") && metadata["sky"] == "r") cameraDescriptions.Add("Remove sky");
			if (metadata.ContainsKey("cam") && metadata["cam"] == "1") cameraDescriptions.Add("3D main camera");
			if (metadata.ContainsKey("zplane") && float.TryParse(_currentLevel.metadata["zplane"], out float zPlane)) {
				cameraDescriptions.Add("<color=\"yellow\">[!] Main camera Z: " + string.Format("{0:N2}", zPlane) + "</color>");
			}
			if (metadata.ContainsKey("farplane") && float.TryParse(_currentLevel.metadata["farplane"], out float farPlane)) {
				cameraDescriptions.Add("Main cam far plane: " + string.Format("{0:N2}", farPlane));
			}
			if (metadata.ContainsKey("bgfarplane") && float.TryParse(_currentLevel.metadata["bgfarplane"], out float bgFarPlane)) {
				cameraDescriptions.Add("BG cam far plane: " + string.Format("{0:N2}", bgFarPlane));
			}

			// Player
			List<string> playerDescriptions = new List<string>();
			if (metadata.ContainsKey("shadow") && metadata["shadow"] == "1") {
				playerDescriptions.Add("<color=\"yellow\">[!] shadow=1</color>");
			}
			if (metadata.ContainsKey("hammermat") && metadata["hammermat"] == "1") {
				playerDescriptions.Add("<color=\"yellow\">[!] hammermat=1</color>");
			}

			List<String> metadataRows = new List<String>();
			if (gameDescriptions.Count   > 0) metadataRows.Add("<b><u>Game:</u></b>\n" + String.Join("\n", gameDescriptions));
			if (cameraDescriptions.Count > 0) metadataRows.Add("<b><u>Camera:</u></b>\n" + String.Join("\n", cameraDescriptions));
			if (playerDescriptions.Count > 0) metadataRows.Add("<b><u>Player:</u></b>\n" + String.Join("\n", playerDescriptions));

			return String.Join("\n", metadataRows);
		}
		private void ClickLevelCard(LevelInfo level) {
			_currentLevel = level;
			config_selectedLevel.Value = level.title;

			// enable right panel
			_levelSelectMenu.transform.Find("Panels").Find("Side Bar").gameObject.SetActive(true);
			
			Transform sideBar = _levelSelectMenu.transform.Find("Panels").Find("Side Bar").Find("Viewport").Find("Content");
			// Thumbnail and title
			sideBar.Find("Thumbnail").gameObject.GetComponent<RawImage>().texture = level.thumbnail ?? _missingThumbnail;
			sideBar.Find("Title Panel").Find("Title Text").gameObject.GetComponent<TextMeshProUGUI>().text = level.title;
			// Author
			if (level.metadata.ContainsKey("credit")) {
				sideBar.Find("Title Panel").Find("Credit Text").gameObject.SetActive(true);
				sideBar.Find("Title Panel").Find("Credit Text").gameObject.GetComponent<TextMeshProUGUI>().text = "By " + level.metadata["credit"];
			} else {
				sideBar.Find("Title Panel").Find("Credit Text").gameObject.SetActive(false);
			}
			// Description
			if (level.metadata.ContainsKey("description")) {
				sideBar.Find("Description Panel").gameObject.SetActive(true);
				sideBar.Find("Description Panel").Find("Description Text").gameObject.GetComponent<TextMeshProUGUI>().text = level.metadata["description"];
			} else {
				sideBar.Find("Description Panel").gameObject.SetActive(false);
			}
			// Metadata
			string metadataReadableString = ConvertLevelMetadataToReadableTMPString(level);
			sideBar.Find("Metadata Panel").Find("Metadata Text").gameObject.GetComponent<TextMeshProUGUI>().text = metadataReadableString;
		}
		string GetSceneName(string path) {
			string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
			return fileName;
		}
		public void ReloadScene() {
			if (isSwitchingScene) {
				Logger.LogInfo("Skipping scene reload, already reloading");
				return;
			}
			isSwitchingScene = true;

			// ==================== Bundles ===================
			// Load dummy dio bundle
			if (_dummyBundle == null) {
				_dummyBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "dummy dio.scene"));
			}
			// Calculate bundle hash
			string bundleLocation = Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "Levels",
				_currentLevel.title + ".scene");
			byte[] hashResult;
			SHA1 sha = new SHA1CryptoServiceProvider();
			using(FileStream fs = File.OpenRead(@bundleLocation)) {
				hashResult = sha.ComputeHash(fs);
			}
			// Reload bundle if hash is different
			if (!hashResult.SequenceEqual(_bundleFileHash)) {
				if (_bundle != null) {
					_bundle.Unload(unloadAllLoadedObjects: true);
				}
				_bundle = AssetBundle.LoadFromFile(bundleLocation);
				_bundleFileHash = hashResult;
			}


			// ======================= Scenes ==============================
			// Check if map scene exists
			if (!_bundle.GetAllScenePaths().Any(a => a.EndsWith(_sceneName + ".unity"))) {
				Logger.LogError("Scene not found: \"" + _sceneName + "\"");
				isSwitchingScene = false;
				return;
			}

			// Get settings
			bool loadDummyDio = !_currentLevel.metadata.ContainsKey("dummy") || _currentLevel.metadata["dummy"] != "r";
			bool replaceLighting = _currentLevel.metadata.ContainsKey("lighting") && _currentLevel.metadata["lighting"] == "r";

			// Scene loaded event
			UnityAction<Scene, LoadSceneMode> onLoaded = null;
			onLoaded = (Scene scene, LoadSceneMode loadSceneMode) => {
				if ((!loadDummyDio || SceneManager.GetSceneByName(_dummySceneName).isLoaded) && SceneManager.GetSceneByName(_sceneName).isLoaded) {
					SceneManager.sceneLoaded -= onLoaded;

					// Set active scene if lighting=r
					if (replaceLighting || !loadDummyDio) {
						SceneManager.SetActiveScene(SceneManager.GetSceneByName(_sceneName));
					} else {
						SceneManager.SetActiveScene(SceneManager.GetSceneByName(_dummySceneName));
					}

					// Send message to mod loader
					FlourModLoaderPlugin.Instance.ChangeGameState(GameState.InGame, SceneManager.GetSceneByName(_sceneName));
					isSwitchingScene = false;
				}
			};
			SceneManager.sceneLoaded += onLoaded;

			// Load scenes
			SceneManager.LoadScene(_sceneName, LoadSceneMode.Single);
			if (loadDummyDio) {
				SceneManager.LoadScene(_dummySceneName, LoadSceneMode.Additive);
			}
		}
		public void LoadScene(string name) {
			if (isSwitchingScene) return;
			_sceneName = name;
			ReloadScene();
		}
		public void LoadSelectedLevel() {
			Logger.LogInfo("Loading map: " + _currentLevel.title);
			if (_bundle != null) {
				_bundle.Unload(unloadAllLoadedObjects: true);
			}
			string levelsFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "Levels");

			// Load map assembly
			string assemblyPath = Path.Combine(levelsFolder, _currentLevel.title + ".dll");
			if (File.Exists(assemblyPath) && !loadedMapAssemblies.Contains(_currentLevel.title)) {
				Assembly.LoadFrom(assemblyPath);
				Logger.LogInfo("Loaded assembly: " + assemblyPath);
				loadedMapAssemblies.Add(_currentLevel.title);
			}
			
			// Calculate hash for scene file
			string bundleLocation = Path.Combine(levelsFolder, _currentLevel.title + ".scene");
			SHA1 sha = new SHA1CryptoServiceProvider();
			using(FileStream fs = File.OpenRead(@bundleLocation)) {
				_bundleFileHash = sha.ComputeHash(fs);
			}
			// Load scene file
			_bundle = AssetBundle.LoadFromFile(bundleLocation);
			if (_currentLevel.metadata.ContainsKey("scene") && _bundle.GetAllScenePaths().Any(a => a.EndsWith(_currentLevel.metadata["scene"] + ".unity"))) {
				_sceneName = _currentLevel.metadata["scene"];
			} else {
				// no scene specified, load first scene in bundle
				_sceneName = GetSceneName(_bundle.GetAllScenePaths()[0]); // get scene name without .unity
			}
			ReloadScene();
		}
		private static Dictionary<string, string> ParseMetadataFile(string filePath) {
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] lines = File.ReadAllLines(filePath);

			foreach (string line in lines) {
				int equalsIndex = line.IndexOf("=");
				if (equalsIndex == -1 || equalsIndex == 0 || equalsIndex == line.Length - 1) continue;

				string key = line.Substring(0, equalsIndex).Trim();
				string value = line.Substring(equalsIndex + 1).Trim();

				dictionary.Add(key, value);
			}

			return dictionary;
		}
		public static void TeleportPlayer(Vector2 pos, bool keepMomentum = true) {
			Vector3 player3DPos = new Vector3(pos.x, pos.y, 0f);

			Vector3 cameraOffset = GOIConstants.mainCamera.transform.position - GOIConstants.player.transform.position;
			Vector3 cursorOffset = GOIConstants.playerCtrl.fakeCursor.position - GOIConstants.player.transform.position;

			SimulationMode2D simulationMode = Physics2D.simulationMode;
			Physics2D.simulationMode = SimulationMode2D.Script;
			GOIConstants.player.transform.position = player3DPos;

			Vector3 cursorPos = player3DPos + cursorOffset;
			cursorPos.z = 0f;

			GOIConstants.playerCtrl.fakeCursor.position = cursorPos;
			Physics2D.simulationMode = simulationMode;

			Vector3 newCameraPos = player3DPos + cameraOffset;
			newCameraPos.z = 0f;

			GOIConstants.cameraCtrl.Teleport(newCameraPos);

			if (!keepMomentum) {
				Vector2 playerVelocity = GOIConstants.player.GetComponent<Rigidbody2D>().velocity;
				Rigidbody2D[] allRigidbodies = GOIConstants.player.GetComponentsInChildren<Rigidbody2D>();
				foreach (Rigidbody2D rb in allRigidbodies) {
					rb.velocity -= playerVelocity;
				}
			}
		}

		private void AddLevelSelector() {
			// set currentLevel based on config title
			if (!String.IsNullOrEmpty(config_selectedLevel.Value)) {
				foreach (LevelInfo level in _levels) {
					if (level.title == config_selectedLevel.Value) {
						_currentLevel = level;
						break;
					}
				}
			}

			// Create level loader UI
			_levelSelectMenu = Instantiate(_levelLoaderPanelPrefab);
			_levelSelectMenu.transform.SetParent(GOIConstants.canvas.transform, false);
			
			// Add events to buttons
			Transform buttonColumn = _levelSelectMenu.transform.Find("Panels").Find("Side Bar").Find("Viewport").Find("Content").Find("Button Column");

			buttonColumn.Find("New Game Button").gameObject.GetComponent<Button>().onClick.AddListener(LoadSelectedLevel);
			buttonColumn.Find("Cancel Button").gameObject.GetComponent<Button>().onClick.AddListener(ToggleLevelSelectMenu);
			
			// Load thumbnail textures
			foreach (LevelInfo level in _levels) {
				string basePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "Levels", level.title);
				string thumbnailPngPath = basePath + ".png";
				string thumbnailJpgPath = basePath + ".jpg";
				if (File.Exists(thumbnailPngPath)) {
					byte[] fileData = File.ReadAllBytes(thumbnailPngPath);
					level.thumbnail = new Texture2D(2, 2);
					level.thumbnail.LoadImage(fileData);
				} else if (File.Exists(thumbnailJpgPath)) {
					byte[] fileData = File.ReadAllBytes(thumbnailJpgPath);
					level.thumbnail = new Texture2D(2, 2);
					level.thumbnail.LoadImage(fileData);
				}
			}
			// Disable right panel if no level is selected, if a level is selected then update the right panel
			if (_currentLevel == null) {
				_levelSelectMenu.transform.Find("Panels").Find("Side Bar").gameObject.SetActive(false);
				config_selectedLevel.Value = "";
			} else {
				ClickLevelCard(_currentLevel);
			}

			// Load level list and scroll to selected level
			LoadLevelList();

			Transform titleBar = _levelSelectMenu.transform.Find("Panels").Find("Levels").Find("Title Bar").Find("Horizontal Layout");
			
			titleBar.Find("View Mode").GetComponent<Button>().onClick.AddListener(() => {
				config_viewMode.Value = (config_viewMode.Value + 1) % 2;
				LoadLevelList();
			});
			titleBar.Find("Search").GetComponent<TMP_InputField>().onValueChanged.AddListener((text) => {
				LoadLevelList();
			});
			titleBar.Find("Search Dropdown").GetComponent<TMP_Dropdown>().onValueChanged.AddListener((item) => {
				LoadLevelList();
			});
			titleBar.Find("Refresh").GetComponent<Button>().onClick.AddListener(() => {
				// delete old level select menu
				Destroy(_levelSelectMenu);
				_levelSelectMenu = null;
				// clear level list
				_currentLevel.thumbnail = null;
				_levels.Clear();
				GetLevelsFromFolder();
				AddLevelSelector();
				_levelSelectMenu?.SetActive(true);
			});
			

			_levelSelectMenu.SetActive(false);
		}

		private void LoadLevelList() {
			// set view mode icon
			Transform titleBar = _levelSelectMenu.transform.Find("Panels").Find("Levels").Find("Title Bar").Find("Horizontal Layout");
			titleBar.Find("View Mode").Find("Padding").Find("Grid").gameObject.SetActive(config_viewMode.Value == 0);
			titleBar.Find("View Mode").Find("Padding").Find("List").gameObject.SetActive(config_viewMode.Value == 1);
			string searchText = titleBar.Find("Search").GetComponent<TMP_InputField>().text;
			int searchMode = titleBar.Find("Search Dropdown").GetComponent<TMP_Dropdown>().value;
			
			// clear existing level list
			Transform levelsTransform = _levelSelectMenu.transform.Find("Panels").Find("Levels");
			Transform levelGrid = levelsTransform.Find("Level Grid");
			Transform levelList = levelsTransform.Find("Level List");
			Transform levelGridContent = levelGrid.Find("Viewport").Find("Content");
			Transform levelListContent = levelList.Find("Viewport").Find("Content");
			while (levelGridContent.childCount > 0) {
				DestroyImmediate(levelGridContent.GetChild(0).gameObject);
			}
			while (levelListContent.childCount > 0) {
				DestroyImmediate(levelListContent.GetChild(0).gameObject);
			}
			levelGrid.gameObject.SetActive(config_viewMode.Value == 0);
			levelList.gameObject.SetActive(config_viewMode.Value == 1);

			GameObject scrollToObject = null;
			// add levels to list
			foreach (LevelInfo level in _levels) {
				switch (searchMode) {
					case 0: // Search in title
						if (!String.IsNullOrEmpty(searchText) && !level.title.ToLower().Contains(searchText.ToLower())) continue;
						break;
					case 1: // Search in author
						if (
							!String.IsNullOrEmpty(searchText) &&
							(!level.metadata.ContainsKey("credit") ||
							 !level.metadata["credit"].ToLower().Contains(searchText.ToLower()))
							) continue;
						break;
				}

				

				switch (config_viewMode.Value) {
					case 0: // Grid
						GameObject levelCard = Instantiate(_templateLevelCardPrefab);
						levelCard.name = level.title;
						levelCard.transform.Find("Mask").Find("Title").gameObject.GetComponent<TextMeshProUGUI>().text = level.title;
						
						if (level.thumbnail != null) {
							levelCard.transform.Find("Mask").Find("Thumbnail").gameObject.GetComponent<RawImage>().texture = level.thumbnail;
						}

						levelCard.GetComponent<Button>().onClick.AddListener(() => { ClickLevelCard(level); });

						levelCard.transform.SetParent(levelGridContent, false);
						break;
					case 1: // List
						GameObject levelListItem = Instantiate(_templateLevelListItemPrefab);
						levelListItem.name = level.title;
						levelListItem.transform.Find("Mask").Find("Title").gameObject.GetComponent<TextMeshProUGUI>().text = level.title;
						levelListItem.transform.Find("Mask").Find("Author").gameObject.GetComponent<TextMeshProUGUI>()
							.text = level.metadata.ContainsKey("credit") ? level.metadata["credit"] : "";
						
						if (level.thumbnail != null) {
							levelListItem.transform.Find("Mask").Find("Thumbnail").gameObject.GetComponent<RawImage>().texture = level.thumbnail;
						}

						levelListItem.GetComponent<Button>().onClick.AddListener(() => { ClickLevelCard(level); });
						
						levelListItem.transform.SetParent(levelListContent, false);
						break;
				}
			}
		}
	}
};
