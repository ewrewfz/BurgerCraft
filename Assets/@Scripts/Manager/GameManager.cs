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

    /// <summary>
    /// GameManager는 씬 전환 시 파괴되도록 설정
    /// </summary>
    protected override bool ShouldDestroyOnLoad()
    {
        return true; // 씬 전환 시 파괴됨
    }

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

    public int Experience
    {
        get 
        { 
            if (SaveData == null || SaveData.Restaurants == null)
                return 0;
                
            if (Restaurant != null && Restaurant.StageNum >= 0 && Restaurant.StageNum < SaveData.Restaurants.Count)
            {
                return SaveData.Restaurants[Restaurant.StageNum].Experience;
            }
            return 0;
        }
        set
        {
            if (SaveData == null || SaveData.Restaurants == null)
                return;
                
            if (Restaurant != null && Restaurant.StageNum >= 0 && Restaurant.StageNum < SaveData.Restaurants.Count)
            {
                SaveData.Restaurants[Restaurant.StageNum].Experience = value;
                BroadcastEvent(EEventType.ExpChanged);
                
                // 경험치 변경 시 즉시 저장
                SaveManager.Instance.SaveGame();
            }
        }
    }

    public int Level
    {
        get 
        { 
            if (SaveData == null || SaveData.Restaurants == null)
                return 1;
                
            if (Restaurant != null && Restaurant.StageNum >= 0 && Restaurant.StageNum < SaveData.Restaurants.Count)
            {
                return SaveData.Restaurants[Restaurant.StageNum].Level;
            }
            return 1;
        }
        set
        {
            if (SaveData == null || SaveData.Restaurants == null)
                return;
                
            if (Restaurant != null && Restaurant.StageNum >= 0 && Restaurant.StageNum < SaveData.Restaurants.Count)
            {
                // 최대 레벨 제한
                int clampedLevel = Mathf.Clamp(value, 1, MAX_LEVEL);
                SaveData.Restaurants[Restaurant.StageNum].Level = clampedLevel;
                BroadcastEvent(EEventType.ExpChanged);
                
                // 레벨 변경 시 즉시 저장
                SaveManager.Instance.SaveGame();
            }
        }
    }

    /// <summary>
    /// 경험치를 추가합니다. (손님이 버거를 받을 때 호출)
    /// 레벨업은 슬라이더가 1.0에 도달할 때 UI에서 처리합니다.
    /// </summary>
    public void AddExperience(int amount = EXP_PER_GUEST)
    {
        if (SaveData == null || SaveData.Restaurants == null)
            return;
            
        if (Restaurant == null || Restaurant.StageNum < 0 || Restaurant.StageNum >= SaveData.Restaurants.Count)
            return;

        int currentLevel = Level;
        if (currentLevel >= MAX_LEVEL)
        {
            // 최대 레벨이면 경험치 추가 안 함
            return;
        }

        int currentExp = Experience;
        int newExp = currentExp + amount;

        // 경험치만 업데이트 (레벨업은 슬라이더가 1.0에 도달할 때 UI에서 처리)
        Experience = newExp;
    }

    /// <summary>
    /// 레벨업을 처리합니다. (슬라이더가 1.0에 도달했을 때 UI에서 호출)
    /// </summary>
    public void ProcessLevelUp()
    {
        if (SaveData == null || SaveData.Restaurants == null)
            return;
            
        if (Restaurant == null || Restaurant.StageNum < 0 || Restaurant.StageNum >= SaveData.Restaurants.Count)
            return;

        int currentLevel = Level;
        if (currentLevel >= MAX_LEVEL)
        {
            return; // 최대 레벨
        }

        int currentExp = Experience;
        
        // 경험치가 EXP_PER_LEVEL 이상이면 레벨업
        if (currentExp >= EXP_PER_LEVEL)
        {
            int newExp = currentExp - EXP_PER_LEVEL;
            int newLevel = currentLevel + 1;
            
            // 경험치를 먼저 업데이트 (레벨업 후 남은 경험치)
            Experience = newExp;
            
            // 레벨업
            Level = newLevel;
        }
    }

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

        // 저장된 경험치와 레벨이 이미 SaveData에 있으므로, UI 업데이트를 위해 이벤트 발생
        // (Experience와 Level 프로퍼티는 SaveData에서 직접 읽으므로 별도 설정 불필요)
        yield return new WaitForEndOfFrame(); // UI 초기화 대기
        BroadcastEvent(EEventType.ExpChanged);

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
                    restaurantData.Experience = Experience;
                    restaurantData.Level = Level;
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
