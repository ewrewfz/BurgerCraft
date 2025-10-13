using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Define;

#region DataModel
[Serializable]
public class GameSaveData
{
	// 소지금.
	public long Money = 0;

	// 플레이어 위치.
	public int RestaurantIndex;
	public Vector3 PlayerPosition;

	// 스테이지 별 상태.
	public List<RestaurantData> Restaurants;
}

[Serializable]
public class RestaurantData
{
	// 직원 수.
	public int WorkerCount;

	// 업그레이드.

	// 프랍들.
	public ETutorialState TutorialState = ETutorialState.None;
	public List<UnlockableStateData> UnlockableStates;
}

[Serializable]
public class UnlockableStateData
{
	public EUnlockedState State = EUnlockedState.Hidden;
	public long SpentMoney = 0;
}
#endregion

public class SaveManager : Singleton<SaveManager>
{
	private GameSaveData _saveData = new GameSaveData();
	public GameSaveData SaveData => _saveData;
	public string Path { get { return Application.persistentDataPath + "/SaveData.json"; } }

	private void Awake()
	{
		if (LoadGame() == false)
		{
			InitGame();
			SaveGame();
		}

		DontDestroyOnLoad(gameObject);
	}

	public void InitGame()
	{
		if (File.Exists(Path))
			return;

		// 소지금.
		_saveData.Money = 1000;

		// 각종 업그레이드.

		// 스테이지 별 상태.
		const int MAX_STAGE = 10;
		const int MAX_PROPS = 20;

		_saveData.Restaurants = new List<RestaurantData>();
		for (int i = 0; i < MAX_STAGE; i++)
		{
			RestaurantData restaurantData = new RestaurantData();

			restaurantData.UnlockableStates = new List<UnlockableStateData>();
			for (int j = 0; j < MAX_PROPS; j++)
				restaurantData.UnlockableStates.Add(new UnlockableStateData());

			_saveData.Restaurants.Add(restaurantData);
		}
	}

	public void SaveGame()
	{
		string jsonStr = JsonUtility.ToJson(_saveData);
		File.WriteAllText(Path, jsonStr);
		Debug.Log($"Save Game Completed : {Path}");
	}

	public bool LoadGame()
	{
		if (File.Exists(Path) == false)
			return false;

		string fileStr = File.ReadAllText(Path);
		GameSaveData data = JsonUtility.FromJson<GameSaveData>(fileStr);

		if (data != null)
			_saveData = data;

		Debug.Log($"Save Game Loaded : {Path}");
		return true;
	}
}
