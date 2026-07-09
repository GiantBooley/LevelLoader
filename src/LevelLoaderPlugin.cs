using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Rendering.PostProcessing;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using I2.Loc;
using HarmonyLib;
using FPModLoader;

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


	[BepInPlugin("goi.flowerpot.levelloader", "Level Loader", "0.1.0")]
	[BepInProcess("GettingOverIt.exe")]
	[BepInDependency("goi.flowerpot.fpmodloader")]
	public class LevelLoaderPlugin : FPMod {
		internal static new ManualLogSource Logger;
		public static LevelLoaderPlugin Instance;

		public override string author => "Flower Pot";
		public override string description => "Level loader for loading bundle maps";

		// config
		private ConfigEntry<string> config_selectedLevel;
		private ConfigEntry<int> config_viewMode;
		

		private LevelInfo _currentLevel;
		private GameObject _levelSelectMenu;
		private readonly string _pluginFolder = "BepInEx/plugins/LevelLoader";
		private List<LevelInfo> _levels;

		public string sceneName;
		public AssetBundle bundle;
		private byte[] _bundleFileHash;
		public List<string> loadedMapAssemblies;
		
		public string dummySceneName = "DummyDio";
		public AssetBundle dummyBundle;

		private readonly string _levelLoaderUIBundleName = "levelloaderui.assets";
		private AssetBundle _levelLoaderUIBundle;
		private GameObject _levelLoaderPanelPrefab;
		private GameObject _templateLevelCardPrefab;
		private GameObject _templateLevelListItemPrefab;

		private Loader _loader;
		private bool _didPlayOpeningAnimation;
		public bool isCustomMap;
		public bool isSwitchingScene; // prevent multiple scene switches at once

		private static readonly string[] ValidMetadataExtensions = {"mdata", "txt"};

		private void Awake() {
			// Plugin startup logic
			Logger = base.Logger;
			Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

			// config
			config_selectedLevel = Config.Bind("Level Loader", "SelectedLevel", "", "Title of the current selected level");
			config_viewMode = Config.Bind("Level Loader", "ViewMode", 0, "Grid if set to 0, list if set to 1");

			Instance = this;

			// patch with Harmony
			var harmony = new Harmony("goi.flowerpot.levelloader");
			harmony.PatchAll();

			_levels = new List<LevelInfo>();

			// create level folder if it doesnt exist
			string levelsFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "Levels");
			if (!Directory.Exists(levelsFolder)) Directory.CreateDirectory(levelsFolder);

			isCustomMap = false;
			isSwitchingScene = false;
			_didPlayOpeningAnimation = false;
			loadedMapAssemblies = new List<string>();

			// template prefabs
			_levelLoaderUIBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, _levelLoaderUIBundleName));
			_levelLoaderPanelPrefab = _levelLoaderUIBundle.LoadAsset<GameObject>("LevelLoader.prefab");
			_templateLevelCardPrefab = _levelLoaderUIBundle.LoadAsset<GameObject>("TemplateLevelCard.prefab");
			_templateLevelListItemPrefab = _levelLoaderUIBundle.LoadAsset<GameObject>("TemplateLevelListItem.prefab");

			FPModLoaderPlugin.Instance.RegisterMod(this);
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
					 *
					 * cam: not tested
					 * zplane: not added
					 * farplane: added
					 * bgfarplane: added
					 *
					 * fog: not added
					 *
					 * shadow: not added
					 * hammermat: not added
					 */
					// Set BGCamera background to black
					Camera bgCam = GOIConstants.mainCamera.transform.Find("BGCamera").GetComponent<Camera>();
					bgCam.backgroundColor = Color.black;

					// Set far plane of Camera to farplane
					if (_currentLevel.metadata.ContainsKey("farplane") && float.TryParse(_currentLevel.metadata["farplane"], out float farPlane)) {
						GOIConstants.mainCamera.GetComponent<Camera>().farClipPlane = farPlane;
					}
					// Set far plane of BGCamera to bgfarplane
					if (_currentLevel.metadata.ContainsKey("bgfarplane") && float.TryParse(_currentLevel.metadata["bgfarplane"], out float bgFarPlane)) {
						bgCam.farClipPlane = bgFarPlane;
					}

					// Disable BGCamera>Sky if sky=r
					if (_currentLevel.metadata.ContainsKey("sky") && _currentLevel.metadata["sky"] == "r") {
						Transform sky = GOIConstants.mainCamera.transform.Find("BGCamera").Find("Sky");
						if (sky != null) {
							sky.gameObject.SetActive(false);
						}
					}

					// Teleport to startPos
					GameObject startPos = GameObject.Find("startPos");
					if (startPos == null) {
						TeleportPlayer(new Vector2(-44.292595f, -2.4218144f), true);
					} else {
						TeleportPlayer(new Vector2(startPos.transform.position.x, startPos.transform.position.y), true);
					}

					// Set camera type to perspective if cam=1
					if (_currentLevel.metadata.ContainsKey("cam") && _currentLevel.metadata["cam"] == "1") {
						GOIConstants.mainCamera.GetComponent<Camera>().orthographic = false;
					}

					// ======= Other fixes ================
					// Disable hammer doppler
					GOIConstants.player.transform.Find("Hub").Find("Slider").Find("Handle").Find("PoleMiddle")
						.Find("Tip").gameObject.GetComponent<AudioSource>().dopplerLevel = 0f;
					// Solution for ending in space then joining map
					Physics2D.gravity = new Vector2(0f, -30f);

					_didPlayOpeningAnimation = false;
					break;
			}
			if (newState != GameState.MainMenu) {
				_currentLevel.thumbnail = null;
				_levels.Clear();
			}
		}
		private void Update() {
			// Play pot opening animation on the first frame of joining a map
			if (!_didPlayOpeningAnimation && GOIConstants.gameState == GameState.InGame && isCustomMap && GOIConstants.player != null) {
				GOIConstants.player.GetComponentInChildren<PoseControl>().PlayOpeningAnimation();
				GOIConstants.playerCtrl.loadFinished = true;
				GOIConstants.cameraCtrl.loadFinished = true;
				_didPlayOpeningAnimation = true;
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
		public void ToggleLevelSelectMenu() {
			if (_levelSelectMenu == null) return;
			_levelSelectMenu.SetActive(!_levelSelectMenu.activeSelf);
		}
		public void ClickLevelCard(LevelInfo level) {
			_currentLevel = level;
			config_selectedLevel.Value = level.title;

			// enable right panel
			_levelSelectMenu.transform.Find("Panels").Find("Side Bar").gameObject.SetActive(true);
			
			Transform sideBar = _levelSelectMenu.transform.Find("Panels").Find("Side Bar").Find("Viewport").Find("Content");
			sideBar.Find("Thumbnail").gameObject.GetComponent<RawImage>().texture = level.thumbnail;
			sideBar.Find("Title Panel").Find("Title Text").gameObject.GetComponent<TextMeshProUGUI>().text = level.title;
			if (level.metadata.ContainsKey("credit")) {
				sideBar.Find("Title Panel").Find("Credit Text").gameObject.SetActive(true);
				sideBar.Find("Title Panel").Find("Credit Text").gameObject.GetComponent<TextMeshProUGUI>().text = "By " + level.metadata["credit"];
			} else {
				sideBar.Find("Title Panel").Find("Credit Text").gameObject.SetActive(false);
			}
			if (level.metadata.ContainsKey("description")) {
				sideBar.Find("Description Panel").gameObject.SetActive(true);
				sideBar.Find("Description Panel").Find("Mask").Find("Scroll View").Find("Viewport")
					.Find("Text (TMP)").gameObject.GetComponent<TextMeshProUGUI>().text = level.metadata["description"];
			} else {
				sideBar.Find("Description Panel").gameObject.SetActive(false);
			}
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

			if (dummyBundle == null) {
				dummyBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Application.dataPath), _pluginFolder, "dummy dio.scene"));
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
				if (bundle != null) {
					bundle.Unload(unloadAllLoadedObjects: true);
				}
				bundle = AssetBundle.LoadFromFile(bundleLocation);
				_bundleFileHash = hashResult;
			}

			// check if scene exists
			if (!bundle.GetAllScenePaths().Any(a => a.EndsWith(sceneName + ".unity"))) {
				Logger.LogError("Tried to load scene \"" + sceneName + "\" that doesnt exist");
				isSwitchingScene = false;
				return;
			}

			// add lambda to scene loaded to change gamestate when both are loaded
			UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = null;

			onLoaded = (Scene scene, LoadSceneMode loadSceneMode) => {
				if (SceneManager.GetSceneByName(dummySceneName).isLoaded && SceneManager.GetSceneByName(sceneName).isLoaded) {
					SceneManager.sceneLoaded -= onLoaded;
					FPModLoaderPlugin.Instance.ChangeGameState(GameState.InGame, SceneManager.GetSceneByName(sceneName));
					isSwitchingScene = false;
				}
			};


			SceneManager.sceneLoaded += onLoaded;
			// load map scene then dummy scene
			SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
			SceneManager.LoadScene(dummySceneName, LoadSceneMode.Additive);
		}
		public void LoadScene(string name) {
			if (isSwitchingScene) return;
			sceneName = name;
			ReloadScene();
		}
		public void LoadSelectedLevel() {
			Logger.LogInfo("Loading map: " + _currentLevel.title);
			if (bundle != null) {
				bundle.Unload(unloadAllLoadedObjects: true);
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
			bundle = AssetBundle.LoadFromFile(bundleLocation);
			if (_currentLevel.metadata.ContainsKey("scene") && bundle.GetAllScenePaths().Any(a => a.EndsWith(_currentLevel.metadata["scene"] + ".unity"))) {
				sceneName = _currentLevel.metadata["scene"];
			} else {
				sceneName = GetSceneName(bundle.GetAllScenePaths()[0]); // get scene name without .unity
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

			// create levelloader ui
			_levelSelectMenu = Instantiate(_levelLoaderPanelPrefab);
			_levelSelectMenu.transform.SetParent(GOIConstants.canvas.transform, false);
			
			// add events to buttons
			Transform buttonColumn = _levelSelectMenu.transform.Find("Panels").Find("Side Bar").Find("Viewport").Find("Content").Find("Button Column");

			buttonColumn.Find("New Game Button").gameObject.GetComponent<Button>().onClick.AddListener(LoadSelectedLevel);
			buttonColumn.Find("Cancel Button").gameObject.GetComponent<Button>().onClick.AddListener(ToggleLevelSelectMenu);
			
			// load thumbnail textures
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
			// disable right panel if no level is selected, if a level is selected then update the right panel
			if (_currentLevel == null) {
				_levelSelectMenu.transform.Find("Panels").Find("Side Bar").gameObject.SetActive(false);
				config_selectedLevel.Value = "";
			} else {
				ClickLevelCard(_currentLevel);
			}

			// load level list and scroll to selected level
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
					case 0: // search in title
						if (!String.IsNullOrEmpty(searchText) && !level.title.ToLower().Contains(searchText.ToLower())) continue;
						break;
					case 1: // search in author
						if (
							!String.IsNullOrEmpty(searchText) &&
							(!level.metadata.ContainsKey("credit") ||
							 !level.metadata["credit"].ToLower().Contains(searchText.ToLower()))
							) continue;
						break;
				}

				

				switch (config_viewMode.Value) {
					case 0:
						GameObject levelCard = Instantiate(_templateLevelCardPrefab);
						levelCard.name = level.title;
						levelCard.transform.Find("Mask").Find("Title").gameObject.GetComponent<TextMeshProUGUI>().text = level.title;
						
						if (level.thumbnail != null) {
							levelCard.transform.Find("Mask").Find("Thumbnail").gameObject.GetComponent<RawImage>().texture = level.thumbnail;
						}

						levelCard.GetComponent<Button>().onClick.AddListener(() => { ClickLevelCard(level); });

						levelCard.transform.SetParent(levelGridContent, false);
						break;
					case 1:
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

	// ==============================
	// ---- level loader scripts ----
	// ==============================
	/*
	[Serializable]
	public class CustomHitSound {
		public string name;

		public List<AudioClip> hits;
		public List<AudioClip> hardHits;
		public List<AudioClip> scrapes;

		public AudioClip GetHit() {
			if (hits.Count > 0) {
				return hits[UnityEngine.Random.Range(0, hits.Count)];
			}
			return null;
		}

		public AudioClip GetHardHit() {
			if (hits.Count > 0) {
				return hardHits[UnityEngine.Random.Range(0, hardHits.Count)];
			}
			return GetHit();
		}

		public AudioClip GetScrape() {
			if (scrapes.Count > 0) {
				return scrapes[UnityEngine.Random.Range(0, scrapes.Count)];
			}
			return null;
		}
	};
	public class CustomHitSoundProvider : MonoBehaviour {
		public CustomHitSound[] hitSounds;

		public Dictionary<string, CustomHitSound> hitDict = new Dictionary<string, CustomHitSound>();

		public AudioClip GetHit(string mat) {
			if (hitDict.ContainsKey(mat)) {
				return hitDict[mat].GetHit();
			}
			return null;
		}
		public AudioClip GetHardHit(string mat) {
			if (hitDict.ContainsKey(mat)) {
				return hitDict[mat].GetHardHit();
			}
			return null;
		}
		public AudioClip GetScrape(string mat) {
			if (hitDict.ContainsKey(mat)) {
				return hitDict[mat].GetScrape();
			}
			return null;
		}

		public void AwakeAfterCopy() {
			CustomHitSound[] sounds = hitSounds;
			foreach (CustomHitSound customHitSound in sounds) {
				hitDict.Add(customHitSound.name, customHitSound);
			}
		}
	};
	public class CustomGroundCol : MonoBehaviour {
		public Color groundCol = new Color(0.7f, 0.6f, 0.3f);
		public string material;
		public bool isSolid;
	};




	[RequireComponent(typeof(Collider2D))]
	public class CustomTrigger : MonoBehaviour {
		public enum Trigger {
			Reset = 0,
			Teleport = 1,
			Finish = 2,
			Playsound = 3,
			Event = 5,
			Animation = 4,
			SwitchScene = 6
		}

		public enum DetectMode {
			PlayerOnly,
			PlayerAndProps,
			PropsOnly,
			SpecificProp
		}

		public Trigger TriggerType;

		public bool Mode;

		public Vector3 Destination;

		public AudioSource source;

		public AudioClip Sound;

		public UnityEvent TriggerEvent;

		public Animation animation;

		public string anim;

		public DetectMode Detect;

		public GameObject[] TargetProp;

		private void Reset() {
			Destination = base.transform.position;
		}

		private void OnDrawGizmos() {
			if (TriggerType == Trigger.Teleport) {
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(Destination, 0.25f);
			}
		}

		private void Start() {
			if (TriggerType == Trigger.Playsound) {
				source = base.gameObject.GetComponent<AudioSource>();
				if (source == null) {
					source = (AudioSource)base.gameObject.AddComponent(typeof(AudioSource));
				}
			}
		}

		private void OnTriggerEnter2D(Collider2D col) {
			if ((col.gameObject.layer != 8 && Detect == DetectMode.PlayerOnly) ||
				(col.gameObject.layer == 8 && Detect == DetectMode.PropsOnly) ||
				(Detect == DetectMode.SpecificProp && !TargetProp.Contains(col.gameObject))) {
				return;
				}
				switch (TriggerType) {
					case Trigger.Reset:
						LevelLoader.LevelLoaderPlugin.TeleportPlayer(new Vector2(-1000f, -1000f));
						break;
					case Trigger.Teleport:
						LevelLoader.LevelLoaderPlugin.TeleportPlayer(new Vector2(Destination.x, Destination.y));
						if (!Mode) {
							Rigidbody2D component = GOIConstants.player.GetComponent<Rigidbody2D>();
							if (component != null) {
								component.velocity = Vector2.zero;
							}
						}
						break;
					case Trigger.Finish:
					{
						MonoBehaviour.print("Finished");
						PlayerPrefs.SetFloat("LastTime", GOIConstants.timer);
						PlayerPrefs.DeleteKey("NumSaves");
						PlayerPrefs.DeleteKey("SaveGame0");
						PlayerPrefs.DeleteKey("SaveGame1");
						PlayerPrefs.DeleteKey("targetScene");
						int @int = PlayerPrefs.GetInt("NumWins");
						@int++;
						PlayerPrefs.SetInt("NumWins", @int);
						Debug.Log("Game Done, deleting saves");
						PlayerPrefs.Save();
						SceneManager.LoadScene("Reward Loader");
						break;
					}
					case Trigger.Playsound:
						if (!source.isPlaying) {
							source.PlayOneShot(Sound);
						}
						if (Mode) {
							Collider2D[] components = GetComponents<Collider2D>();
							for (int i = 0; i < components.Length; i++) {
								components[i].enabled = false;
							}
						}
						break;
					case Trigger.Animation:
						if (animation != null) {
							if (!string.IsNullOrEmpty(anim)) {
								animation.Play(anim);
							} else {
								animation.Play();
							}
						}
						break;
					case Trigger.Event:
						TriggerEvent.Invoke();
						break;
					case Trigger.SwitchScene:
						LevelLoader.LevelLoaderPlugin.Instance.LoadScene(anim);
						break;
				}
		}
	}
	public class OnlyColliders_Hammer : MonoBehaviour {
		private void Start() {
			base.gameObject.GetComponent<PolygonCollider2D>().usedByEffector = true;
			base.gameObject.AddComponent<PlatformEffector2D>();
			PlatformEffector2D component = base.gameObject.GetComponent<PlatformEffector2D>();
			component.useOneWay = false;
			component.useColliderMask = true;
			component.colliderMask = -270795137;
		}
	};
	public class OnlyColliders_Pot : MonoBehaviour {
		private void Start() {
			base.gameObject.AddComponent<PlatformEffector2D>();
			PlatformEffector2D component = base.gameObject.GetComponent<PlatformEffector2D>();
			component.useOneWay = false;
			component.useColliderMask = true;
			component.colliderMask = -539230977;
			PolygonCollider2D[] components = base.gameObject.GetComponents<PolygonCollider2D>();
			for (int i = 0; i < components.Length; i++) {
				components[i].usedByEffector = true;
			}
		}
	};

	public class GravityControlLite : MonoBehaviour {
		[Serializable]
		public class Attractors
		{
			public Transform gravityWell;

			public float gravityModifier;
		}

		private Vector2 gvec;

		public Attractors[] gravityWells;

		public static float gravmod;

		private void OnTriggerStay2D(Collider2D coll)
		{
			Attractors[] array = gravityWells;
			foreach (Attractors attractors in array)
			{
				gvec = new Vector2(attractors.gravityWell.position.x, attractors.gravityWell.position.y) - coll.attachedRigidbody.position;
				coll.attachedRigidbody.AddForce(2500f / gvec.sqrMagnitude * gvec.normalized * (1f + attractors.gravityModifier));
			}
		}

		private void OnTriggerEnter2D(Collider2D coll)
		{
			Physics2D.gravity = new Vector2(0f, 0f);
			gravmod = 1f;
		}

		private void OnTriggerExit2D(Collider2D coll)
		{
			if (coll.attachedRigidbody != null)
			{
				Physics2D.gravity = new Vector2(0f, -30f);
				gravmod = 0f;
			}
		}
	}
	*/
};


// =========================
// ---- harmony patches ----
// =========================

// dont load default map in loader
[HarmonyPatch(typeof(Loader), "Start")]
public static class DontLoadDefaultMapInLoaderStartPatch {

	[HarmonyPrefix]
	public static bool Prefix(Loader __instance) {
		var t = Traverse.Create(__instance);

		t.Field("safeToClick").SetValue(false);
		t.Field("hideLogo").SetValue(PlayerPrefs.GetInt("HideLogo") == 1);
		if (!PlayerPrefs.HasKey("Trackpad") || Application.isEditor) {
			t.Field("shouldShowMouseQuery").SetValue(true);
		} else {
			t.Field("shouldShowMouseQuery").SetValue(false);
		}
		t.Field("progress").SetValue(1f); // original: 0f
		t.Field("lerpProgress").SetValue(1f); // original: 0f
		t.Field("loadFinished").SetValue(true); // original: false
		t.Field("menuItemClicked").SetValue(-1);

		t.Field("canContinue").SetValue(false);
		if (PlayerPrefs.GetString("SaveGame0").Length > 0 || PlayerPrefs.GetString("SaveGame1").Length > 0) {
			t.Field("canContinue").SetValue(true);
		}
		TextMeshProUGUI[] componentsInChildren = __instance.menu.GetComponentsInChildren<TextMeshProUGUI>();
		for (int i = 0; i < componentsInChildren.Length; i++) {
			Color color = componentsInChildren[i].color;
			color.a = 0f;
			componentsInChildren[i].color = color;
		}
		__instance.continueBar.color = new Color(0f, 0f, 0f, 0f);
		__instance.newgameBar.color = new Color(0f, 0f, 0f, 0f);
		__instance.scrapeParticles.gameObject.SetActive(value: true);
		__instance.fog.SetActive(value: true);
		t.Field("introAnimDone").SetValue(false);
		__instance.humble.gameObject.SetActive(value: true);
		__instance.titleMask.sizeDelta = new Vector2(0f, 216f);
		if (t.Field("hideLogo").GetValue<bool>()) {
			__instance.fader.color = new Color(0f, 0f, 0f, 1f);
			__instance.humble.gameObject.SetActive(value: false);
			if (t.Field("shouldShowMouseQuery").GetValue<bool>()) {
				__instance.mouseQuery.SetActive(value: true);
				__instance.languageBar.SetActive(value: true);
				__instance.languageBar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 826.7f);
			}
			__instance.StartCoroutine("FadeInTitle");
		} else {
			__instance.StartCoroutine("FadeInHumble");
			PlayerPrefs.SetInt("HideLogo", 1);
			PlayerPrefs.Save();
		}
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		return false;
	}
}
// load sync instead of async for new game button
[HarmonyPatch(typeof(Loader), "DoStart")]
public static class LoaderDoStartPatch {
	[HarmonyPrefix]
	public static bool Prefix(Loader __instance) {
		__instance.variants.WarmUp();
		SceneManager.LoadScene("Mian");

		return false;
	}
};
// ctrl r in maps
[HarmonyPatch(typeof(PlayerControl), "Update")]
public static class PlayerControlUpdatePatch {
	private static readonly AccessTools.FieldRef<PlayerControl, int> numWinsRef = AccessTools.FieldRefAccess<PlayerControl, int>("numWins");

	[HarmonyPrefix]
	public static bool Prefix(PlayerControl __instance) {
		if (numWinsRef(__instance) > 0 && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.R)) {
			PlayerPrefs.DeleteKey("NumSaves");
			PlayerPrefs.DeleteKey("SaveGame0");
			PlayerPrefs.DeleteKey("SaveGame1");
			PlayerPrefs.Save();
			if (LevelLoader.LevelLoaderPlugin.Instance.isCustomMap) {
				LevelLoader.LevelLoaderPlugin.Instance.ReloadScene();
			} else {
				SceneManager.LoadScene("Mian");
			}
		}
		__instance.fakeCursor.position = new Vector3(__instance.fakeCursor.position.x, __instance.fakeCursor.position.y, __instance.tip.position.z - 1f);

		return false;
	}
};
// auto discover colorsets
[HarmonyPatch(typeof(FogControl), "Start")]
public static class FogControlStartPatch {
	private static readonly AccessTools.FieldRef<FogControl, PostProcessVolume> postRef = AccessTools.FieldRefAccess<FogControl, PostProcessVolume>("post");
	private static readonly AccessTools.FieldRef<FogControl, ColorGrading> settingsRef = AccessTools.FieldRefAccess<FogControl, ColorGrading>("settings");

	[HarmonyPrefix]
	public static bool Prefix(FogControl __instance) {
		var t = Traverse.Create(__instance);

		t.Field("topColor").SetValue(Color.white);
		t.Field("bottomColor").SetValue(Color.white);
		t.Field("midColor").SetValue(Color.white);
		t.Field("contrast").SetValue(1f);
		t.Field("exposure").SetValue(0f);
		t.Field("saturation").SetValue(1f);
		postRef(__instance) = __instance.GetComponent<PostProcessVolume>();
		settingsRef(__instance) = postRef(__instance).profile.GetSetting<ColorGrading>();

		// auto find colorsets
		List<ColorSet> newColorSets = UnityEngine.Object.FindObjectsOfType<ColorSet>().ToList();
		foreach (ColorSet colorSet in __instance.colorSets) { // add fogcontrol colorsets to list
			if (!newColorSets.Contains(colorSet)) {
				newColorSets.Add(colorSet);
			}
		}
		__instance.colorSets = newColorSets.ToArray();

		return false;
	}
}

// check if in game for pause menu
[HarmonyPatch(typeof(SettingsManager), "Update")]
public static class SettingsManagerPausePatch {
	// access private fields
	private static readonly AccessTools.FieldRef<SettingsManager, RestartOnContact> underwaterDetectorRef = AccessTools.FieldRefAccess<SettingsManager, RestartOnContact>("underwaterDetector");
	private static readonly AccessTools.FieldRef<SettingsManager, GameObject> narratorRef				 = AccessTools.FieldRefAccess<SettingsManager, GameObject>("narrator");

	[HarmonyPrefix]
	public static bool Prefix(SettingsManager __instance) {
		// if escape and timer is above 1.5 and not in water and in game then toggle menu
		if (Input.GetKeyDown(KeyCode.Escape) &&
			GOIConstants.timer > 1.5f &&
			(underwaterDetectorRef(__instance) == null || (underwaterDetectorRef(__instance) != null && !underwaterDetectorRef(__instance).resetting)) &&
			GOIConstants.gameState == GameState.InGame) {
				__instance.Invoke("ToggleMenu", 0f);
			}

			return false;
	}
};
// check if water isnt null when starting camera
[HarmonyPatch(typeof(CameraControl), "Start")]
public static class CameraControlStartPatch {
	// access private fields
	private static readonly AccessTools.FieldRef<CameraControl, Camera> mainCamRef	   = AccessTools.FieldRefAccess<CameraControl, Camera>("mainCam");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> velRef		  = AccessTools.FieldRefAccess<CameraControl, Vector3>("vel");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> lookaheadPosRef = AccessTools.FieldRefAccess<CameraControl, Vector3>("lookaheadPos");
	private static readonly AccessTools.FieldRef<CameraControl, GameObject> waterRef	 = AccessTools.FieldRefAccess<CameraControl, GameObject>("water");
	private static readonly AccessTools.FieldRef<CameraControl, GameObject> playerRef	= AccessTools.FieldRefAccess<CameraControl, GameObject>("player");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> targetRef	   = AccessTools.FieldRefAccess<CameraControl, Vector3>("target");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> oldTargetRef	= AccessTools.FieldRefAccess<CameraControl, Vector3>("oldTarget");

	[HarmonyPrefix]
	public static bool Prefix(CameraControl __instance) {
		// traversal for primitives
		var t = Traverse.Create(__instance);

		// modify private fields
		mainCamRef(__instance) = Camera.main;
		velRef(__instance) = Vector3.zero;
		lookaheadPosRef(__instance) = Vector3.zero;

		GameObject water = waterRef(__instance);
		if (water != null) {
			float waterLevel = water.GetComponent<MeshRenderer>().bounds.max.y;
			t.Field("waterLevel").SetValue(waterLevel);
		}

		t.Field("fadeInTimerRef").SetValue(0);
		t.Field("fadeOutTimerRef").SetValue(0);

		GameObject player = playerRef(__instance);
		Vector3 target = player.transform.position + new Vector3(0f, 0f, -20f);
		targetRef(__instance) = target;
		oldTargetRef(__instance) = target;
		t.Field("alpha").SetValue(1f);

		__instance.Invoke("FadeIn", 0f);

		return false;
	}
}
// check if spline is null when teleporting camera
[HarmonyPatch(typeof(CameraControl), "Teleport")]
public static class CameraControlTeleportPatch {

	[HarmonyPrefix]
	public static bool Prefix(CameraControl __instance, Vector3 newPosition) {
		// traversal for primitives
		var t = Traverse.Create(__instance);

		__instance.gameObject.transform.position = newPosition + new Vector3(0f, 0f, -20f);
		if (__instance.spline != null) {
			float lastTF = __instance.spline.GetNearestPointTF(newPosition);
			t.Field("lastTF").SetValue(lastTF);
			Vector3 vector = __instance.spline.Interpolate(lastTF + 0.01f);
			Vector3 vector2 = __instance.spline.Interpolate(lastTF + 0.02f);
			Vector3 vector3 = __instance.spline.Interpolate(lastTF + 0.03f);
			t.Field("lookAheadPos").SetValue(0.3333f * (vector + vector2 + vector3));
		}
		t.Field("vel").SetValue(Vector3.zero);

		return false;
	}
}
// check if saviour is null when playing sad sound
[HarmonyPatch(typeof(PlayerSounds), "PlaySadSound")]
public static class PlaySadSoundSaviourPatch {
	// access private fields
	private static readonly AccessTools.FieldRef<PlayerSounds, float> lastSoundTimerRef	 = AccessTools.FieldRefAccess<PlayerSounds, float>("lastSoundTimer");
	private static readonly AccessTools.FieldRef<PlayerSounds, int>  lastSoundIndexRef	  = AccessTools.FieldRefAccess<PlayerSounds, int>("lastSoundIndex");
	private static readonly AccessTools.FieldRef<PlayerSounds, AudioSource>  audioSourceRef = AccessTools.FieldRefAccess<PlayerSounds, AudioSource>("audioSource");
	private static readonly AccessTools.FieldRef<PlayerSounds, AudioClip[]>  sadSoundsRef   = AccessTools.FieldRefAccess<PlayerSounds, AudioClip[]>("sadSounds");
	private static readonly AccessTools.FieldRef<PlayerSounds, float>  mouthTimerRef		= AccessTools.FieldRefAccess<PlayerSounds, float>("mouthTimer");
	private static readonly AccessTools.FieldRef<PlayerSounds, Saviour>  saviourRef		 = AccessTools.FieldRefAccess<PlayerSounds, Saviour>("saviour");

	private static MethodInfo saviourSaveMethod = AccessTools.Method(typeof(Saviour), "SaveGameNow");

	[HarmonyPrefix]
	public static bool Prefix(PlayerSounds __instance, float volume = 1f) {

		if (!(lastSoundTimerRef(__instance) > 0f)) {
			AudioClip[] sadSounds = sadSoundsRef(__instance);
			if (sadSounds == null || sadSounds.Length == 0)
				return false;

			int index = UnityEngine.Random.Range(0, sadSounds.Length);
			lastSoundIndexRef(__instance) = index;

			AudioSource source = audioSourceRef(__instance);
			if (source != null)
				source.PlayOneShot(sadSounds[index], volume);

			lastSoundTimerRef(__instance) = sadSounds[index].length * 2.5f;
			mouthTimerRef(__instance) = sadSounds[index].length;

			Saviour saviour = saviourRef(__instance);
			if (saviour != null) { // saviour is null in maps
				saviourSaveMethod.Invoke(saviour, new object[] { false });
			}
		}

		return false;
	}
};


[HarmonyPatch]
public static class HammerCollisionsPatch {
	private static readonly AccessTools.FieldRef<HammerCollisions, AudioSource> audioSourceRef = AccessTools.FieldRefAccess<HammerCollisions, AudioSource>("audioSource");

	private static Type customHitSoundProviderType = AccessTools.TypeByName("CustomHitSoundProvider");
	private static Type customGroundColType = AccessTools.TypeByName("CustomGroundCol");

	private static MethodInfo playHitMethod = typeof(HammerCollisions).GetMethod("PlayHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo playHardHitMethod = typeof(HammerCollisions).GetMethod("PlayHardHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo playScrapeMethod = typeof(HammerCollisions).GetMethod("PlayScrape", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static MethodInfo getHitMethod = customHitSoundProviderType.GetMethod("GetHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo getHardHitMethod = customHitSoundProviderType.GetMethod("GetHardHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo getScrapeMethod = customHitSoundProviderType.GetMethod("GetScrape", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);


	private static FieldInfo isSolidField = customGroundColType.GetField("isSolid", BindingFlags.Public | BindingFlags.Instance);
	private static FieldInfo groundColField = customGroundColType.GetField("groundCol", BindingFlags.Public | BindingFlags.Instance);
	private static FieldInfo materialField = customGroundColType.GetField("material", BindingFlags.Public | BindingFlags.Instance);

	public static class HammerCollisionsCustomFunctions {
		public static void PlayCustomHardHit(HammerCollisions __instance, string customMat, float volume = 1f) {
			var t = Traverse.Create(__instance);
			Type customHitSoundProviderType = AccessTools.TypeByName("CustomHitSoundProvider");

			if (t.Field("lastSoundTimer").GetValue<float>() < 0.1f) {
				return;
			}
			if (volume > 3f) {
				volume = 3f;
			}
			t.Field("lastSoundTimer").SetValue(0f);
			AudioClip audioClip = null;

			UnityEngine.Object customHitSoundProvider = UnityEngine.Object.FindObjectOfType(customHitSoundProviderType);

			if (customHitSoundProvider != null) {

				audioClip = (AudioClip)getHardHitMethod.Invoke(customHitSoundProvider, new object[] { customMat });
			}
			if (audioClip != null) {
				audioSourceRef(__instance).PlayOneShot(audioClip, volume);
			} else {
				playHardHitMethod.Invoke(__instance, new object[] { volume, GroundCol.SoundMaterial.rock });
			}
		}

		public static void PlayCustomHit(HammerCollisions __instance, string customMat, float volume = 1f) {
			var t = Traverse.Create(__instance);

			if (t.Field("lastSoundTimer").GetValue<float>() < 0.1f) {
				return;
			}
			if (volume > 3f) {
				volume = 3f;
			}
			t.Field("lastSoundTimer").SetValue(0f);
			AudioClip audioClip = null;

			UnityEngine.Object customHitSoundProvider = UnityEngine.Object.FindObjectOfType(customHitSoundProviderType);

			if (customHitSoundProvider != null) {
				audioClip = (AudioClip)getHitMethod.Invoke(customHitSoundProvider, new object[] { customMat });
			}
			if (audioClip != null) {
				audioSourceRef(__instance).PlayOneShot(audioClip, volume);
			} else {
				playHitMethod.Invoke(__instance, new object[] { volume, GroundCol.SoundMaterial.rock });
			}
		}
		public static void PlayCustomScrape(HammerCollisions __instance, string customMat, float volume = 1f) {
			var t = Traverse.Create(__instance);

			if (t.Field("lastSoundTimer").GetValue<float>() < 0.1f){
				return;
			}
			if (volume > 3f) {
				volume = 3f;
			}
			t.Field("lastSoundTimer").SetValue(0f);
			AudioClip audioClip = null;

			UnityEngine.Object customHitSoundProvider = UnityEngine.Object.FindObjectOfType(customHitSoundProviderType);

			if (customHitSoundProvider != null) {
				audioClip = (AudioClip)getScrapeMethod.Invoke(customHitSoundProvider, new object[] { customMat });
			}
			if (audioClip != null) {
				audioSourceRef(__instance).PlayOneShot(audioClip, volume);
			} else {
				playScrapeMethod.Invoke(__instance, new object[] { volume, GroundCol.SoundMaterial.rock });
			}
		}
	}
	[HarmonyPatch(typeof(HammerCollisions), "OnCollisionEnter2D")]
	public static class EnterPatch {
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> sparkParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("sparkParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> debrisParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("debrisParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> dustParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("dustParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ContactPoint2D[]> contactsRef = AccessTools.FieldRefAccess<HammerCollisions, ContactPoint2D[]>("contacts");


		[HarmonyPrefix]
		public static bool Prefix(HammerCollisions __instance, Collision2D coll) {
			var t = Traverse.Create(__instance);

			if (__instance.collisionPoints == null) {
				return false;
			}
			int contactCount = coll.GetContacts(contactsRef(__instance));
			if (contactCount <= 0 || (coll.gameObject.layer != 10 && coll.gameObject.layer != 29)) {
				return false;
			}
			if (coll.relativeVelocity.magnitude > 14f) {
				__instance.player.SendMessage("HammerReturn");
			}
			if (__instance.collisionPoints.ContainsKey(coll.collider)) {
				return false;
			}
			__instance.collisionPoints[coll.collider] = contactsRef(__instance);
			GroundCol groundCol = coll.collider.GetComponent<GroundCol>();

			Component customGroundCol = coll.collider.GetComponent(customGroundColType);
			bool customGroundColIsSolid = false;;
			Color customGroundColGroundCol = new Color();
			string customGroundColMaterial = "";
			if (customGroundCol != null) {
				// is solid
				customGroundColIsSolid = (bool)isSolidField.GetValue(customGroundCol);
				// hit color
				customGroundColGroundCol = (Color)groundColField.GetValue(customGroundCol);
				// hit sound
				customGroundColMaterial = (string)materialField.GetValue(customGroundCol);
			}
			Color impactColor = new Color();
			bool hasImpactColor = true;
			if (customGroundCol != null) {
				impactColor = customGroundColGroundCol;
			} else if (groundCol != null){
				impactColor = groundCol.groundCol;
			} else {
				hasImpactColor = false;
			}

			__instance.impactBlur.Impact(new Vector2(contactsRef(__instance)[0].point.x, contactsRef(__instance)[0].point.y), coll.relativeVelocity.magnitude / 12f);

			bool isSolid = false;
			if ((groundCol != null && (groundCol.material == GroundCol.SoundMaterial.rock || groundCol.material == GroundCol.SoundMaterial.metal || groundCol.material == GroundCol.SoundMaterial.solidmetal)) || (customGroundCol != null && customGroundColIsSolid)) {
				isSolid = true;
			}
			if (coll.relativeVelocity.magnitude < t.Field("hardHitThreshold").GetValue<float>()) { // soft hit
				for (int i = 0; i < contactCount; i++) {
					debrisParamsRef(__instance).position = contactsRef(__instance)[i].point;
					if (hasImpactColor) {
						debrisParamsRef(__instance).startColor = impactColor;
					}
					float normalImpulse = contactsRef(__instance)[i].normalImpulse;
					float tangentImpulse = contactsRef(__instance)[i].tangentImpulse;
					Vector3 contactNormal = contactsRef(__instance)[i].normal;
					Vector3 contactNormal2D = new Vector3(contactNormal.y, 0f - contactNormal.x, 0f);
					for (int j = 0; j < (int)Mathf.Min((normalImpulse + tangentImpulse) * 0.1f, 20f); j++) {
						debrisParamsRef(__instance).velocity = contactNormal * normalImpulse * 0.2f + contactNormal2D * ((j % 2 == 0) ? 1f : (-1f)) * tangentImpulse * 0.1f + UnityEngine.Random.insideUnitSphere * 3.4f;
						__instance.debris.Emit(debrisParamsRef(__instance), 1);
					}
				} if (customGroundCol != null) {
					HammerCollisionsCustomFunctions.PlayCustomHit(__instance, customGroundColMaterial, coll.relativeVelocity.magnitude / t.Field("hardHitThreshold").GetValue<float>());
				} else if (groundCol != null) {
					playHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / t.Field("hardHitThreshold").GetValue<float>(), groundCol.material });
				} else {
					playHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / t.Field("hardHitThreshold").GetValue<float>(), GroundCol.SoundMaterial.rock});
				}
				return false;
			}
			if (isSolid) {
				for (int k = 0; k < contactCount; k++) {
					sparkParamsRef(__instance).position = contactsRef(__instance)[k].point;
					float normalImpulse2 = contactsRef(__instance)[k].normalImpulse;
					float tangentImpulse2 = contactsRef(__instance)[k].tangentImpulse;
					float value = normalImpulse2 + tangentImpulse2;
					Vector3 contactNormal = contactsRef(__instance)[k].normal;
					Vector3 contactNormal2D = new Vector3(contactNormal.y, 0f - contactNormal.x, 0f);
					for (int l = 0; l < (int)Mathf.Min((normalImpulse2 + tangentImpulse2) * 0.1f, 20f); l++) {
						sparkParamsRef(__instance).velocity = contactNormal * normalImpulse2 * 0.3f + contactNormal2D * ((l % 2 == 0) ? 1f : (-1f)) * tangentImpulse2 * 0.3f + UnityEngine.Random.insideUnitSphere * Mathf.Clamp(value, 1f, 60f) * 0.1f;
						__instance.sparks.Emit(sparkParamsRef(__instance), 1);
					}
				}
			} else {
				for (int m = 0; m < contactCount; m++) {
					debrisParamsRef(__instance).position = contactsRef(__instance)[m].point;
					if (hasImpactColor) {
						debrisParamsRef(__instance).startColor = impactColor;
					}
					float normalImpulse3 = contactsRef(__instance)[m].normalImpulse;
					float tangentImpulse3 = contactsRef(__instance)[m].tangentImpulse;
					float value2 = normalImpulse3 + tangentImpulse3;
					Vector3 contactNormal = contactsRef(__instance)[m].normal;
					Vector3 contactNormal2D = new Vector3(contactNormal.y, 0f - contactNormal.x, 0f);
					for (int n = 0; n < (int)Mathf.Min((normalImpulse3 + tangentImpulse3) * 0.1f, 20f); n++) {
						debrisParamsRef(__instance).velocity = contactNormal * normalImpulse3 * 0.1f + contactNormal2D * ((n % 2 == 0) ? 1f : (-1f)) * tangentImpulse3 * 0.1f + UnityEngine.Random.insideUnitSphere * Mathf.Clamp(value2, 1f, 60f) * 0.1f;
						__instance.debris.Emit(debrisParamsRef(__instance), 1);
					}
				}
			} if (customGroundCol != null) {
				HammerCollisionsCustomFunctions.PlayCustomHardHit(__instance, customGroundColMaterial, coll.relativeVelocity.magnitude / 4f);
			} else if (groundCol != null) {
				playHardHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / 4f, groundCol.material });
			} else {
				playHardHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / 4f, GroundCol.SoundMaterial.rock });
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(HammerCollisions), "OnCollisionStay2D")]
	public class StayPatch {
		private static Type customGroundColType = AccessTools.TypeByName("CustomGroundCol");

		private static readonly AccessTools.FieldRef<HammerCollisions, AudioSource> audioSourceRef = AccessTools.FieldRefAccess<HammerCollisions, AudioSource>("audioSource");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> sparkParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("sparkParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> debrisParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("debrisParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> dustParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("dustParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ContactPoint2D[]> contactsRef = AccessTools.FieldRefAccess<HammerCollisions, ContactPoint2D[]>("contacts");
		private static readonly AccessTools.FieldRef<HammerCollisions, Rigidbody2D> tipRef = AccessTools.FieldRefAccess<HammerCollisions, Rigidbody2D>("tip");

		[HarmonyPrefix]
		public static bool Prefix(HammerCollisions __instance, Collision2D coll) {
			var t = Traverse.Create(__instance);

			if (__instance.collisionPoints == null || coll.gameObject.layer != LayerMask.NameToLayer("Terrain")) {
				return false;
			}
			int contactCount = coll.GetContacts(contactsRef(__instance));
			if (!__instance.collisionPoints.ContainsKey(coll.collider)) {
				return false;
			}
			bool isScrape = true;
			float minMagnitude = 0.3f;
			float maxTangentialness = 0f;
			for (int i = 0; i < contactCount; i++) {
				for (int j = 0; j < __instance.collisionPoints[coll.collider].Length; j++) {
					float magnitude = (__instance.collisionPoints[coll.collider][j].point - contactsRef(__instance)[i].point).magnitude;
					minMagnitude = Mathf.Min(minMagnitude, magnitude);
					maxTangentialness = Mathf.Max((1f + Mathf.Abs(contactsRef(__instance)[i].tangentImpulse)) / (1f + Mathf.Abs(contactsRef(__instance)[i].normalImpulse)), maxTangentialness);
					if (magnitude < t.Field("moveThreshold").GetValue<float>()) {
						isScrape = false;
						break;
					}
				}
			}
			if (tipRef(__instance).velocity.magnitude > 0.3f && maxTangentialness > 5f) { // if hammer is fast and
				isScrape = true;
			}
			if (isScrape && coll.rigidbody == null) {
				__instance.collisionPoints[coll.collider] = contactsRef(__instance);

				GroundCol groundCol = coll.collider.GetComponent<GroundCol>();
				// get custom ground col
				Component customGroundCol = coll.collider.GetComponent(customGroundColType);
				bool customGroundColIsSolid = false;
				Color customGroundColGroundCol = new Color();
				string customGroundColMaterial = "";
				if (customGroundCol != null) {
					// is solid
					customGroundColIsSolid = (bool)isSolidField.GetValue(customGroundCol);
					// hit color
					customGroundColGroundCol = (Color)groundColField.GetValue(customGroundCol);
					// hit sound
					customGroundColMaterial = (string)materialField.GetValue(customGroundCol);
				}

				if (customGroundCol != null) {
					dustParamsRef(__instance).startColor = customGroundColGroundCol;
				} else if (groundCol != null) {
					dustParamsRef(__instance).startColor = groundCol.groundCol;
				}

				for (int k = 0; k < contactsRef(__instance).Length; k++) {
					dustParamsRef(__instance).position = contactsRef(__instance)[k].point;
					Vector3 vector = 0.4f * new Vector3(contactsRef(__instance)[k].normal.y, 0f - contactsRef(__instance)[k].normal.x) * Vector2.Dot(new Vector2(contactsRef(__instance)[k].normal.y, 0f - contactsRef(__instance)[k].normal.x), coll.relativeVelocity);
					dustParamsRef(__instance).velocity = UnityEngine.Random.insideUnitSphere * 2f + vector;
					__instance.dust.Emit(dustParamsRef(__instance), (int)Mathf.Max(minMagnitude * 50f - 1f, 0f));
					if (!audioSourceRef(__instance).isPlaying) {
						if (customGroundCol != null) {
							HammerCollisionsCustomFunctions.PlayCustomScrape(__instance, customGroundColMaterial, 1f);
						} else if (groundCol != null) {
							playScrapeMethod.Invoke(__instance, new object[] { 1f, groundCol.material});
						} else {
							playScrapeMethod.Invoke(__instance, new object[] { 1f, GroundCol.SoundMaterial.rock });
						}
					}
				}
			}
			t.Field("slide").SetValue(isScrape);
			return false;
		}
	}
}
// level loader component patches

// CUSTOM HIT SOUND ==================================
[HarmonyPatch]
public static class CustomHitSoundPatch {
	public static readonly Type customHitSoundType = AccessTools.TypeByName("CustomHitSound");

	public static FieldInfo customHitSoundHardHitsField = customHitSoundType.GetField("hardHits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	public static FieldInfo customHitSoundHitsField = customHitSoundType.GetField("hits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	public static FieldInfo customHitSoundScrapesField = customHitSoundType.GetField("scrapes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	public static MethodInfo customHitSoundGetHitMethod = customHitSoundType.GetMethod("GetHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	[HarmonyPatch]
	public static class GetHardHitPatch {
		public static MethodBase TargetMethod() => AccessTools.Method(customHitSoundType, "GetHardHit");

		[HarmonyPrefix]
		public static bool Prefix(object __instance, ref AudioClip __result) {
			var t = __instance.GetType();

			List<AudioClip> hardHits = customHitSoundHardHitsField?.GetValue(__instance) as List<AudioClip>;

			if (hardHits.Count > 0) { // return hard hit
				__result = hardHits[UnityEngine.Random.Range(0, hardHits.Count)];
			} else { // if theres no hard hits return hit
				__result = (AudioClip)customHitSoundGetHitMethod.Invoke(__instance, null);
			}
			return false;
		}
	};
	[HarmonyPatch]
	public static class GetHitPatch {
		public static MethodBase TargetMethod() => AccessTools.Method(customHitSoundType, "GetHit");

		[HarmonyPrefix]
		public static bool Prefix(object __instance, ref AudioClip __result) {
			var t = __instance.GetType();

			List<AudioClip> hits = customHitSoundHitsField?.GetValue(__instance) as List<AudioClip>;

			if (hits.Count > 0) { // return hit
				__result = hits[UnityEngine.Random.Range(0, hits.Count)];
			} else { // if theres no hard hits return null
				__result = null;
			}
			return false;
		}
	}
	[HarmonyPatch]
	public static class GetScrapePatch {
		public static MethodBase TargetMethod() => AccessTools.Method(customHitSoundType, "GetScrape");

		[HarmonyPrefix]
		public static bool Prefix(object __instance, ref AudioClip __result) {
			var t = __instance.GetType();

			List<AudioClip> scrapes = customHitSoundScrapesField?.GetValue(__instance) as List<AudioClip>;

			if (scrapes.Count > 0) { // return scrape
				__result = scrapes[UnityEngine.Random.Range(0, scrapes.Count)];
			} else { // if theres no scrapes return null
				__result = null;
			}
			return false;
		}
	};
}


// CUSTOM HIT SOUND PROVIDER
[HarmonyPatch]
public static class CustomHitSoundProviderPatch {
	private static readonly Type customHitSoundProviderType = AccessTools.TypeByName("CustomHitSoundProvider");
	private static readonly Type customHitSoundType = AccessTools.TypeByName("CustomHitSound");

	private static FieldInfo hitSoundsField = customHitSoundProviderType.GetField("hitSounds", BindingFlags.Instance | BindingFlags.Public);
	private static FieldInfo hitDictField = customHitSoundProviderType.GetField("hitDict", BindingFlags.Instance | BindingFlags.Public);

	private static FieldInfo customHitSoundNameField = customHitSoundType.GetField("name");
	private static MethodInfo customHitSoundGetHardHitMethod = customHitSoundType.GetMethod("GetHardHit");
	private static MethodInfo customHitSoundGetHitMethod = customHitSoundType.GetMethod("GetHit");
	private static MethodInfo customHitSoundGetScrapeMethod = customHitSoundType.GetMethod("GetScrape");

	[HarmonyPatch]
	public static class AwakePatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "Awake");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			var hitSounds = hitSoundsField?.GetValue(__instance) as Array;
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			// initialize hitdict
			if (hitDict == null) {
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), customHitSoundType);
				hitDict = Activator.CreateInstance(dictionaryType) as System.Collections.IDictionary;
			}

			hitDict.Clear();

			foreach (var customHitSound in hitSounds) {
				if (customHitSound == null) continue;

				string name = customHitSoundNameField.GetValue(customHitSound) as string;
				hitDict[name] = customHitSound;
			}

			hitDictField?.SetValue(__instance, hitDict);

			return false;
		}
	}
	[HarmonyPatch]
	public static class GetHitPatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "GetHit", new[] { typeof(string) });

		[HarmonyPrefix]
		public static bool Prefix(object __instance, string mat, ref AudioClip __result) {
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			if (hitDict != null && hitDict.Contains(mat)) {
				var customHitSound = hitDict[mat];
				__result = (AudioClip)customHitSoundGetHitMethod.Invoke(customHitSound, null);
			} else {
				__result = null;
			}

			return false;
		}
	}
	[HarmonyPatch]
	public static class GetHardHitPatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "GetHardHit", new[] { typeof(string) });


		static bool Prefix(object __instance, string mat, ref AudioClip __result) {
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			if (hitDict != null && hitDict.Contains(mat)) {
				var customHitSound = hitDict[mat];
				__result = (AudioClip)customHitSoundGetHardHitMethod.Invoke(customHitSound, null);
			} else {
				__result = null;
			}

			return false;
		}
	}
	[HarmonyPatch]
	public static class GetScrapePatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "GetScrape", new[] { typeof(string) });

		static bool Prefix(object __instance, string mat, ref AudioClip __result) {
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			if (hitDict != null && hitDict.Contains(mat)) {
				var customHitSound = hitDict[mat];
				__result = (AudioClip)customHitSoundGetScrapeMethod.Invoke(customHitSound, null);
			} else {
				__result = null;
			}

			return false;
		}
	}
}

/*[HarmonyPatch]
public static class CustomTriggerPatch {
	private static readonly Type customTriggerType = Type.GetType("CustomTrigger, Assembly-CSharp");

	private static FieldInfo customTriggerDestinationField = customTriggerType.GetField("Destination");

	[HarmonyPatch]
	public static class ResetPatch {
		static MethodBase TargetMethod() => AccessTools.Method(customTriggerType, "Reset");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			Transform transform = Traverse.Create(__instance).Property("transform").GetValue<Transform>();

			customTriggerDestinationField.SetValue(__instance, transform.position);
		}
	}

	[HarmonyPatch]
	public static class StartPatch {
		static MethodBase TargetMethod() => AccessTools.Method(customTriggerType, "Start");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
		}
	}
}*/
// Namespaces required for the patches
[HarmonyPatch]
public static class CustomTriggerPatch {
	private static readonly Type T_CustomTrigger = AccessTools.TypeByName("CustomTrigger");

	private static readonly Type T_Trigger = AccessTools.Inner(T_CustomTrigger, "Trigger");
	private static readonly Type T_DetectMode = AccessTools.Inner(T_CustomTrigger, "DetectMode");

	// get enums as ints
	private static readonly int V_Trigger_Reset = (int)Enum.Parse(T_Trigger, "Reset");
	private static readonly int V_Trigger_Teleport = (int)Enum.Parse(T_Trigger, "Teleport");
	private static readonly int V_Trigger_Finish = (int)Enum.Parse(T_Trigger, "Finish");
	private static readonly int V_Trigger_Playsound = (int)Enum.Parse(T_Trigger, "Playsound");
	private static readonly int V_Trigger_Animation = (int)Enum.Parse(T_Trigger, "Animation");
	private static readonly int V_Trigger_Event = (int)Enum.Parse(T_Trigger, "Event");
	private static readonly int V_Trigger_SwitchScene = (int)Enum.Parse(T_Trigger, "SwitchScene");

	private static readonly int V_DetectMode_PlayerOnly = (int)Enum.Parse(T_DetectMode, "PlayerOnly");
	private static readonly int V_DetectMode_PropsOnly = (int)Enum.Parse(T_DetectMode, "PropsOnly");
	private static readonly int V_DetectMode_SpecificProp = (int)Enum.Parse(T_DetectMode, "SpecificProp");

	private static readonly FieldInfo F_TriggerType = AccessTools.Field(T_CustomTrigger, "TriggerType");
	private static readonly FieldInfo F_Mode = AccessTools.Field(T_CustomTrigger, "Mode");
	private static readonly FieldInfo F_Destination = AccessTools.Field(T_CustomTrigger, "Destination");
	private static readonly FieldInfo F_source = AccessTools.Field(T_CustomTrigger, "source");
	private static readonly FieldInfo F_Sound = AccessTools.Field(T_CustomTrigger, "Sound");
	private static readonly FieldInfo F_TriggerEvent = AccessTools.Field(T_CustomTrigger, "TriggerEvent");
	private static readonly FieldInfo F_animation = AccessTools.Field(T_CustomTrigger, "animation");
	private static readonly FieldInfo F_anim = AccessTools.Field(T_CustomTrigger, "anim");
	private static readonly FieldInfo F_Detect = AccessTools.Field(T_CustomTrigger, "Detect");
	private static readonly FieldInfo F_TargetProp = AccessTools.Field(T_CustomTrigger, "TargetProp");

	private static MonoBehaviour AsMonoBehaviour(object instance) => (MonoBehaviour)instance;

	[HarmonyPatch]
	public static class ResetPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "Reset");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			F_Destination.SetValue(__instance, AsMonoBehaviour(__instance).transform.position);

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnDrawGizmosPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "OnDrawGizmos");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			int triggerTypeVal = (int)F_TriggerType.GetValue(__instance);

			if (triggerTypeVal == V_Trigger_Teleport) {
				Gizmos.color = Color.red;

				Vector3 destination = (Vector3)F_Destination.GetValue(__instance);
				Gizmos.DrawSphere(destination, 0.25f);
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class StartPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "Start");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			int triggerTypeVal = (int)F_TriggerType.GetValue(__instance);

			if (triggerTypeVal == V_Trigger_Playsound) {
				GameObject go = AsMonoBehaviour(__instance).gameObject;

				AudioSource audioSrc = go.GetComponent<AudioSource>();

				if (audioSrc == null) {
					audioSrc = (AudioSource)go.AddComponent(typeof(AudioSource));
				}

				F_source.SetValue(__instance, audioSrc);
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnTriggerEnter2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "OnTriggerEnter2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D col) {
			int detectVal = (int)F_Detect.GetValue(__instance);
			GameObject[] targetProp = (GameObject[])F_TargetProp.GetValue(__instance);

			if ((col.gameObject.layer != 8 && detectVal == V_DetectMode_PlayerOnly) ||
				(col.gameObject.layer == 8 && detectVal == V_DetectMode_PropsOnly) ||
				(detectVal == V_DetectMode_SpecificProp && !targetProp.Contains(col.gameObject))) {
				return false;
			}

			int triggerTypeVal = (int)F_TriggerType.GetValue(__instance);

			if (triggerTypeVal == V_Trigger_Reset) {
				LevelLoader.LevelLoaderPlugin.Instance.ReloadScene();
			} else if (triggerTypeVal == V_Trigger_Teleport) {
				Vector3 destination = (Vector3)F_Destination.GetValue(__instance);
				bool keepMomentum = (bool)F_Mode.GetValue(__instance);

				LevelLoader.LevelLoaderPlugin.TeleportPlayer(new Vector2(destination.x, destination.y), keepMomentum);
			} else if (triggerTypeVal == V_Trigger_Finish) {
				MonoBehaviour.print("Finished");
				PlayerPrefs.SetFloat("LastTime", GOIConstants.timer);
				PlayerPrefs.DeleteKey("NumSaves");
				PlayerPrefs.DeleteKey("SaveGame0");
				PlayerPrefs.DeleteKey("SaveGame1");
				PlayerPrefs.DeleteKey("targetScene");
				int @int = PlayerPrefs.GetInt("NumWins");
				@int++;
				PlayerPrefs.SetInt("NumWins", @int);
				Debug.Log("Game Done, deleting saves");
				PlayerPrefs.Save();
				SceneManager.LoadScene("Reward Loader");
			} else if (triggerTypeVal == V_Trigger_Playsound) {
				AudioSource audioSrc = (AudioSource)F_source.GetValue(__instance);
				AudioClip sound = (AudioClip)F_Sound.GetValue(__instance);
				bool mode = (bool)F_Mode.GetValue(__instance);

				if (!audioSrc.isPlaying) {
					audioSrc.PlayOneShot(sound);
				} if (mode) {
					Collider2D[] components = AsMonoBehaviour(__instance).GetComponents<Collider2D>();
					for (int i = 0; i < components.Length; i++) {
						components[i].enabled = false;
					}
				}
			}
			else if (triggerTypeVal == V_Trigger_Animation) {
				Animation animComponent = (Animation)F_animation.GetValue(__instance);
				string animName = (string)F_anim.GetValue(__instance);

				if (!string.IsNullOrEmpty(animName)) {
					animComponent.Play(animName);
				} else {
					animComponent.Play();
				}
			} else if (triggerTypeVal == V_Trigger_Event) {
				UnityEvent triggerEvent = (UnityEvent)F_TriggerEvent.GetValue(__instance);
				triggerEvent.Invoke();
			} else if (triggerTypeVal == V_Trigger_SwitchScene) {
				string sceneName = (string)F_anim.GetValue(__instance);

				LevelLoader.LevelLoaderPlugin.Instance.LoadScene(sceneName);
			}

			return false;
		}
	}
}

[HarmonyPatch]
public static class GravityControlLitePatch {
	private static readonly Type T_GravityControlLite = AccessTools.TypeByName("GravityControlLite");
	private static readonly Type T_Attractors = AccessTools.Inner(T_GravityControlLite, "Attractors");

	private static readonly FieldInfo F_gvec = AccessTools.Field(T_GravityControlLite, "gvec");
	private static readonly FieldInfo F_gravityWells = AccessTools.Field(T_GravityControlLite, "gravityWells");
	private static readonly FieldInfo F_gravmod = AccessTools.Field(T_GravityControlLite, "gravmod");

	// Fields for the nested Attractors class
	private static readonly FieldInfo F_Attractors_gravityWell = AccessTools.Field(T_Attractors, "gravityWell");
	private static readonly FieldInfo F_Attractors_gravityModifier = AccessTools.Field(T_Attractors, "gravityModifier");


	[HarmonyPatch]
	public static class OnTriggerStay2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_GravityControlLite, "OnTriggerStay2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D coll) {
			if (coll.attachedRigidbody == null) return false;

			Array gravityWells = (Array)F_gravityWells.GetValue(__instance);

			if (gravityWells == null) {
				Debug.LogWarning("gravityWells is null");
				return false;
			}
			foreach (object attractors in gravityWells) {
				Transform gravityWell = (Transform)F_Attractors_gravityWell.GetValue(attractors);

				float gravityModifier = (float)F_Attractors_gravityModifier.GetValue(attractors);

				Vector2 gvec = new Vector2(gravityWell.position.x, gravityWell.position.y) - coll.attachedRigidbody.position;

				F_gvec.SetValue(__instance, gvec);

				coll.attachedRigidbody.AddForce(2500f / gvec.sqrMagnitude * gvec.normalized * (1f + gravityModifier));
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnTriggerEnter2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_GravityControlLite, "OnTriggerEnter2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D coll) {
			if (coll.attachedRigidbody != null) {
				GOIConstants.ModifierSetEnabled<Vector2>("gravity", "zerograv", true);

				F_gravmod.SetValue(null, 1f);
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnTriggerExit2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_GravityControlLite, "OnTriggerExit2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D coll) {
			if (coll.attachedRigidbody != null) {
				GOIConstants.ModifierSetEnabled<Vector2>("gravity", "zerograv", false);

				F_gravmod.SetValue(null, 0f);
			}

			return false;
		}
	}
}
