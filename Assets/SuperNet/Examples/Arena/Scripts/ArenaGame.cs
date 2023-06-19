using SuperNet.Unity.Components;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperNet.Examples.Arena {

	public class ArenaGame : MonoBehaviour {

		// UI objects
		public GameObject EscapeCanvas;
		public Button EscapeBackground;
		public Button EscapeResume;
		public Button EscapeSpawnSphere;
		public Button EscapeSpawnCube;
		public Button EscapeSpawnNPC;
		public Button EscapeSpawnCar;
		public Button EscapeExit;
		public GameObject ErrorCanvas;
		public Button ErrorConfirm;
		public Text ErrorStatus;
		public GameObject GameCanvas;
		public Text GameStatus;
		public Text GameInfo;

		// Scene objects
		public NetworkSpawner SpawnerPlayers;
		public NetworkSpawner SpawnerSpheres;
		public NetworkSpawner SpawnerCubes;
		public NetworkSpawner SpawnerCars;
		public NetworkSpawner SpawnerNPC;
		public BoxCollider[] Spawns;

		// Network created in the ArenaMenu scene
		private ArenaNetwork Network;

		private void Reset() {
			// Reset the inspector fields to default values (editor only)
			EscapeCanvas = transform.Find("Escape").gameObject;
			EscapeBackground = transform.Find("Escape/Background").GetComponent<Button>();
			EscapeResume = transform.Find("Escape/Layout/Resume").GetComponent<Button>();
			EscapeSpawnSphere = transform.Find("Escape/Layout/SpawnSphere").GetComponent<Button>();
			EscapeSpawnCube = transform.Find("Escape/Layout/SpawnCube").GetComponent<Button>();
			EscapeSpawnCar = transform.Find("Escape/Layout/SpawnCar").GetComponent<Button>();
			EscapeSpawnNPC = transform.Find("Escape/Layout/SpawnNPC").GetComponent<Button>();
			EscapeExit = transform.Find("Escape/Layout/Exit").GetComponent<Button>();
			ErrorCanvas = transform.Find("Error").gameObject;
			ErrorConfirm = transform.Find("Error/Confirm").GetComponent<Button>();
			ErrorStatus = transform.Find("Error/Status").GetComponent<Text>();
			GameCanvas = transform.Find("Game").gameObject;
			GameStatus = transform.Find("Game/Status/Text").GetComponent<Text>();
			GameInfo = transform.Find("Game/Info").GetComponent<Text>();
			SpawnerPlayers = transform.Find("/Spawners/Players").GetComponent<NetworkSpawner>();
			SpawnerSpheres = transform.Find("/Spawners/Spheres").GetComponent<NetworkSpawner>();
			SpawnerCubes = transform.Find("/Spawners/Cubes").GetComponent<NetworkSpawner>();
			SpawnerCars = transform.Find("/Spawners/Cars").GetComponent<NetworkSpawner>();
			SpawnerNPC = transform.Find("/Spawners/NPC").GetComponent<NetworkSpawner>();
			Spawns = transform.Find("/Spawners/Spawns").GetComponentsInChildren<BoxCollider>();
		}

		private void Awake() {
			// Register button listeners and find network
			EscapeBackground.onClick.AddListener(OnClickEscapeBackground);
			EscapeResume.onClick.AddListener(OnClickEscapeResume);
			EscapeSpawnSphere.onClick.AddListener(OnClickEscapeSpawnSphere);
			EscapeSpawnCube.onClick.AddListener(OnClickEscapeSpawnCube);
			EscapeSpawnNPC.onClick.AddListener(OnClickEscapeSpawnNPC);
			EscapeSpawnCar.onClick.AddListener(OnClickEscapeSpawnCar);
			EscapeExit.onClick.AddListener(OnClickEscapeExit);
			ErrorConfirm.onClick.AddListener(OnClickErrorConfirm);
			Network = FindObjectOfType<ArenaNetwork>();
		}

		private void Start() {
			// Open game hud
			OpenCanvasGame();
		}

		private void Update() {
			// If escape is pressed while in game, open escape menu
			if (GameCanvas.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape)) {
				OpenCanvasEscape();
			}
		}

		private void OpenCanvasEscape() {
			// Open the escape menu and close everything else
			EscapeCanvas.SetActive(true);
			ErrorCanvas.SetActive(false);
			GameCanvas.SetActive(false);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		public void OpenCanvasError(string error) {
			// Open error screen and close everything else
			EscapeCanvas.SetActive(false);
			ErrorCanvas.SetActive(true);
			GameCanvas.SetActive(false);
			ErrorStatus.text = error;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void OpenCanvasGame() {
			// Open the game hud and close everything else
			EscapeCanvas.SetActive(false);
			ErrorCanvas.SetActive(false);
			GameCanvas.SetActive(true);
			if (Network == null || !Network.ConnectedToRelay) {
				GameStatus.text = "No network";
			} else if (Network.Server != null) {
				GameStatus.text = "Connected to " + Network.Server.Remote;
			} else {
				GameStatus.text = "Hosting a server";
			}
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		private void OnClickErrorConfirm() {
			// Open game hud
			OpenCanvasGame();
		}

		private void OnClickEscapeBackground() {
			// Resume game
			OpenCanvasGame();
		}

		private void OnClickEscapeResume() {
			// Resume game
			OpenCanvasGame();
		}

		private void OnClickEscapeExit() {
			// Shutdown game and open menu
			try {
				if (Network != null) Network.Shutdown();
				SceneManager.LoadScene("ArenaMenu");
				OpenCanvasError("Add ArenaMenu to build settings");
			} catch (Exception exception) {
				Debug.LogException(exception);
				OpenCanvasError(exception.Message);
			}
		}

		private void OnClickEscapeSpawnSphere() {
			Vector3 position = GetRandomSpawnPosition(10f);
			Quaternion rotation = GetRandomMovableRotation();
			SpawnerSpheres.Spawn(position, rotation);
		}

		private void OnClickEscapeSpawnCube() {
			Vector3 position = GetRandomSpawnPosition(10f);
			Quaternion rotation = GetRandomMovableRotation();
			SpawnerCubes.Spawn(position, rotation);
		}

		private void OnClickEscapeSpawnNPC() {
			Vector3 position = GetRandomSpawnPosition(2f);
			Quaternion rotation = GetRandomPlayerRotation();
			SpawnerNPC.Spawn(position, rotation);
		}

		private void OnClickEscapeSpawnCar() {
			Vector3 position = GetRandomSpawnPosition(2f);
			Quaternion rotation = GetRandomPlayerRotation();
			SpawnerCars.Spawn(position, rotation);
		}

		private Vector3 GetRandomSpawnPosition(float height) {
			BoxCollider area = Spawns[UnityEngine.Random.Range(0, Spawns.Length)];
			float x = UnityEngine.Random.Range(area.bounds.min.x, area.bounds.max.x);
			float y = area.bounds.min.y + height;
			float z = UnityEngine.Random.Range(area.bounds.min.z, area.bounds.max.z);
			return new Vector3(x, y, z);
		}

		private Quaternion GetRandomPlayerRotation() {
			return Quaternion.Euler(
				0f,
				UnityEngine.Random.Range(0f, 360f),
				0f
			);
		}

		private Quaternion GetRandomMovableRotation() {
			return Quaternion.Euler(
				UnityEngine.Random.Range(0f, 360f),
				UnityEngine.Random.Range(0f, 360f),
				UnityEngine.Random.Range(0f, 360f)
			);
		}

	}

}
