using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

public class GameManager : Singleton<GameManager>
{
    public UI_UpgradeEmployeePopup UpgradeEmployeePopup;
    public UI_GameScene GameSceneUI;

    public Vector2 JoystickDir { get; set; } = Vector2.zero;

    public PlayerController Player;

    public Restaurant Restaurant;
    private GameSaveData SaveData
    {
        get
        {
            return SaveManager.Instance.SaveData;
        }
    }

    public long Money
    {
        get { return SaveData.Money; }
        set
        {
            long before = SaveData.Money;
            SaveData.Money = value;
            BroadcastEvent(EEventType.MoneyChanged);
        }
    }
    
    // 일시정지 관련
    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

    private void Start()
    {
        UpgradeEmployeePopup = Utils.FindChild<UI_UpgradeEmployeePopup>(gameObject);
        UpgradeEmployeePopup.gameObject.SetActive(false);
        StartCoroutine(CoInitialize());
    }
    
    public IEnumerator CoInitialize()
    {
        yield return new WaitForEndOfFrame();

        Player = GameObject.FindAnyObjectByType<PlayerController>();
        Restaurant = GameObject.FindAnyObjectByType<Restaurant>();

        int index = Restaurant.StageNum;
        Restaurant.SetInfo(SaveData.Restaurants[index]);

        StartCoroutine(CoSaveData());
    }

    IEnumerator CoSaveData()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);

            SaveData.RestaurantIndex = Restaurant.StageNum;
            SaveData.PlayerPosition = Player.transform.position;
            
            // Restaurant 데이터 저장
            if (Restaurant != null && SaveData.Restaurants != null && Restaurant.StageNum < SaveData.Restaurants.Count)
            {
                RestaurantData restaurantData = SaveData.Restaurants[Restaurant.StageNum];
                if (restaurantData != null)
                {
                    restaurantData.WorkerCount = Restaurant.Workers.Count;
                    restaurantData.WorkerBoosterLevel = Restaurant.WorkerBoosterLevel;
                    restaurantData.WorkerSpeedLevel = Restaurant.WorkerSpeedLevel;
                }
            }

            SaveManager.Instance.SaveGame();
        }
    }

    #region ObjectManager
    public GameObject WorkerPrefab;
    public GameObject SpawnWorker() { return PoolManager.Instance.Pop(WorkerPrefab); }
    public void DespawnWorker(GameObject worker) { PoolManager.Instance.Push(worker); }

    public GameObject BurgerPrefab;
    public GameObject SpawnBurger() { return PoolManager.Instance.Pop(BurgerPrefab); }
    public void DespawnBurger(GameObject burger) { PoolManager.Instance.Push(burger); }

    public GameObject MoneyPrefab;
    public GameObject SpawnMoney() { return PoolManager.Instance.Pop(MoneyPrefab); }
    public void DespawnMoney(GameObject money) { PoolManager.Instance.Push(money); }

    public GameObject TrashPrefab;
    public GameObject SpawnTrash() { return PoolManager.Instance.Pop(TrashPrefab); }
    public void DespawnTrash(GameObject trash) { PoolManager.Instance.Push(trash); }

    public GameObject GuestPrefab;
    public GameObject SpawnGuest() { return PoolManager.Instance.Pop(GuestPrefab); }
    public void DespawnGuest(GameObject guest) { PoolManager.Instance.Push(guest); }
    #endregion

    #region Events
    public void AddEventListener(EEventType type, Action action)
    {
        int index = (int)type;
        if (_events.Length <= index)
            return;

        _events[index] += action;
    }

    public void RemoveEventListener(EEventType type, Action action)
    {
        int index = (int)type;
        if (_events.Length <= index)
            return;

        _events[index] -= action;
    }

    public void BroadcastEvent(EEventType type)
    {
        int index = (int)type;
        // index는 0 기반이므로 Length 이하일 때도 방어
        if (_events.Length <= index)
            return;

        _events[index]?.Invoke();
    }

    Action[] _events = new Action[(int)EEventType.MaxCount];
    #endregion
}
