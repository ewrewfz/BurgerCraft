using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using static Define;
//using static UnityEngine.Rendering.DebugUI;

public class MainCounterSystem : SystemBase
{
    public Grill Grill;
    public Counter Counter;
    public List<Table> Tables = new List<Table>();
    public TrashCan TrashCan;
    public Office Office;

    // 직원들이 담당하는 일들.
    public WorkerController[] Jobs = new WorkerController[(int)EMainCounterJob.MaxCount];
    public override bool HasJob
    {
        get
        {
            for (int i = 0; i < (int)EMainCounterJob.MaxCount; i++)
            {
                EMainCounterJob type = (EMainCounterJob)i;
                if (ShouldDoJob(type))
                    return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        Counter.Owner = this;
        
        // Jobs 배열 크기 강제 재초기화 (enum 변경 시 대비)
        if (Jobs == null || Jobs.Length != (int)EMainCounterJob.MaxCount)
        {
            Jobs = new WorkerController[(int)EMainCounterJob.MaxCount];
        }

        // _levelUnlockData가 비어있으면 Tables 리스트를 기반으로 자동 등록
        if (_levelUnlockData == null || _levelUnlockData.Count == 0)
        {
            AutoRegisterTables();
        }
    }

    /// <summary>
    /// Tables 리스트를 기반으로 레벨별 프랍 언락 데이터를 자동으로 등록합니다.
    /// 첫 번째 테이블은 레벨 1, 두 번째 테이블은 레벨 2... 이런 식으로 등록됩니다.
    /// </summary>
    private void AutoRegisterTables()
    {
        if (Tables == null || Tables.Count == 0)
            return;

        _levelUnlockData = new List<LevelUnlockData>();
        
        // Tables 리스트의 각 테이블을 순서대로 레벨별로 등록
        // 첫 번째 테이블은 레벨 2부터 시작 (레벨 1은 기본 테이블로 가정)
        for (int i = 0; i < Tables.Count; i++)
        {
            if (Tables[i] != null)
            {
                LevelUnlockData unlockData = new LevelUnlockData
                {
                    Level = i + 2, // 레벨 2부터 시작 (레벨 1은 기본)
                    Prop = Tables[i] as UnlockableBase
                };
                _levelUnlockData.Add(unlockData);
            }
        }
    }

    private void OnEnable()
    {
        // 레벨별 프랍 언락 체크를 위한 이벤트 리스너 등록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddEventListener(EEventType.ExpChanged, OnExpChanged);
        }
    }

    private void OnDisable()
    {
        // 이벤트 리스너 제거
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveEventListener(EEventType.ExpChanged, OnExpChanged);
        }
    }

    private void Start()
    {
        // 튜토리얼이 완료된 경우에만 BGM_Playing 재생 (Tutorial.SetInfo() 이후에 실행)
        Tutorial tutorial = GetComponent<Tutorial>();
        if (tutorial != null)
        {
            Restaurant restaurant = GetComponent<Restaurant>();
            if (restaurant != null && SaveManager.Instance != null)
            {
                int stageNum = restaurant.StageNum;
                if (stageNum < SaveManager.Instance.SaveData.Restaurants.Count)
                {
                    RestaurantData restaurantData = SaveManager.Instance.SaveData.Restaurants[stageNum];
                    if (restaurantData != null && restaurantData.TutorialState == ETutorialState.Done)
                    {
                        SoundManager.Instance.PlayBGM("BGM_Playing");
                    }
                }
            }
        }
        else
        {
            // Tutorial 컴포넌트가 없으면 바로 재생 (튜토리얼이 없는 경우)
            SoundManager.Instance.PlayBGM("BGM_Playing");
        }
    }

    private void Update()
    {
        foreach (WorkerController worker in Workers)
        {
            if (worker.WorkerJob != null)
                continue;

            IEnumerator job = DoMainCounterWorkerJob(worker);
            worker.DoJob(job);
        }
    }

    #region Worker
    public override void AddWorker(WorkerController worker)
    {
        base.AddWorker(worker);
    }

    bool ShouldDoJob(EMainCounterJob jobType)
    {
        // 배열 범위 체크
        int jobIndex = (int)jobType;
        if (jobIndex < 0 || jobIndex >= Jobs.Length)
        {
            Debug.LogError($"[MainCounterSystem] Jobs 배열 크기 불일치: jobType={jobType}, jobIndex={jobIndex}, Jobs.Length={Jobs.Length}, MaxCount={(int)EMainCounterJob.MaxCount}");
            // 배열 재초기화 시도
            Jobs = new WorkerController[(int)EMainCounterJob.MaxCount];
            return false;
        }
        
        // 이미 다른 직원이 점유중이라면 스킵.
        WorkerController wc = Jobs[jobIndex];
        if (wc != null)
            return false;

        // 일감이 있는지 확인.
        switch (jobType)
        {
            case EMainCounterJob.MoveBurger:
                {
                    if (Grill == null)
                        return false;
                    if (Grill.CurrentWorker != null)
                        return false;
                    if (Grill.BurgerCount == 0)
                        return false;
                    if (Counter.NeedMoreBurgers == false)
                        return false;
                    return true;
                }
            case EMainCounterJob.CounterCashier:
                {
                    if (Counter == null)
                        return false;
                    if (Counter.CurrentCashierWorker != null)
                        return false;
                    if (Counter.NeedCashier == false)
                        return false;
                    if (Counter.FindTableToServeGuests() == null)
                        return false;

                    return true;
                }
            case EMainCounterJob.CleanTable:
                {
                    foreach (Table table in Tables)
                    {
                        if (table.TableState == ETableState.Dirty)
                            return true;
                    }
                    return false;
                }
            case EMainCounterJob.CookBurger:
                {
                    if (Grill == null)
                        return false;
                    if (Grill.CurrentWorker != null)
                        return false;
                    // Grill에 주문이 있고 버거가 최대 개수 미만일 때 조리 필요
                    if (!Grill.HasOrders())
                        return false;
                    if (Grill.BurgerCount >= Define.GRILL_MAX_BURGER_COUNT)
                        return false;
                    return true;
                }
        }

        return false;
    }

    IEnumerator DoMainCounterWorkerJob(WorkerController wc)
    {
        while (true)
        {
            yield return new WaitForSeconds(1);

            bool foundJob = false;

            // 햄버거 운반 (우선순위 높음 - 버거가 있으면 먼저 옮기기)
            if (ShouldDoJob(EMainCounterJob.MoveBurger))
            {
                foundJob = true;

                // 일감 점유.
                Jobs[(int)EMainCounterJob.MoveBurger] = wc;

                // 버거 픽업 위치로 이동 (BurgerPickupPos가 있으면 사용, 없으면 WorkerPos 사용)
                Transform pickupPos = Grill.BurgerPickupPos != null ? Grill.BurgerPickupPos : Grill.WorkerPos;
                wc.SetDestination(pickupPos.position, () =>
                {
                    wc.transform.rotation = pickupPos.rotation;
                });

                // 가는중.
                yield return new WaitUntil(() => wc.HasArrivedAtDestination);

                // 픽업 위치 도착했으면 일정 시간 대기.
                wc.transform.rotation = pickupPos.rotation;
                
                // 버거를 트레이에 쌓기
                Grill.OnWorkerBurgerInteraction(wc);

                yield return new WaitForSeconds(3);

                // 햄버거 수집했으면 카운터로 이동.
                wc.SetDestination(Counter.BurgerWorkerPos.position, () =>
                {
                    wc.transform.rotation = Counter.BurgerWorkerPos.rotation;
                });

                // 가는중.
                yield return new WaitUntil(() => wc.HasArrivedAtDestination);

                // 카운터 도착했으면 일정 시간 대기.
                wc.transform.rotation = Counter.BurgerWorkerPos.rotation;
                yield return new WaitForSeconds(2);

                // 일감 점유 해제.
                Jobs[(int)EMainCounterJob.MoveBurger] = null;
            }

            // 버거 조리 (주문이 있으면 조리)
            if (ShouldDoJob(EMainCounterJob.CookBurger))
            {
                foundJob = true;

                // 일감 점유.
                Jobs[(int)EMainCounterJob.CookBurger] = wc;

                // 그릴로 이동.
                wc.SetDestination(Grill.WorkerPos.position, () =>
                {
                    wc.transform.rotation = Grill.WorkerPos.rotation;
                });

                // 가는중.
                yield return new WaitUntil(() => wc.HasArrivedAtDestination);

                // 그릴 도착했으면 일정 시간 대기
                wc.transform.rotation = Grill.WorkerPos.rotation;
                
                // 알바생이 그릴에 도착했을 때 직접 조리 시작 (OnGrillTriggerStart가 호출되지 않을 수 있음)
                // 주문이 있고 버거가 최대 개수 미만이면 조리 시작
                if (Grill.HasOrders() && Grill.BurgerCount < Define.GRILL_MAX_BURGER_COUNT)
                {
                    Grill.StartWorkerAutoCooking(wc);
                }
                
                // 조리가 완료될 때까지 대기
                // 주문이 없거나 버거가 최대 개수에 도달했을 때까지 대기
                // 주문이 생기면 자동으로 조리 시작
                yield return new WaitUntil(() =>
                {
                    // 진행바 상태 확인
                    UI_Progressbar progressbar = wc.GetComponentInChildren<UI_Progressbar>(true);
                    bool isCooking = progressbar != null && progressbar.gameObject.activeSelf;
                    
                    // 조리 중이 아니고 주문이 있으면 조리 시작
                    if (!isCooking && Grill.HasOrders() && Grill.BurgerCount < Define.GRILL_MAX_BURGER_COUNT)
                    {
                        Grill.StartWorkerAutoCooking(wc);
                        return false; // 조리 시작했으므로 계속 대기
                    }
                    
                    // 주문이 없거나 버거가 최대 개수에 도달하고 주문이 없으면 완료
                    if (!Grill.HasOrders())
                    {
                        return true; // 주문이 없으면 완료
                    }
                    
                    // 버거가 최대 개수에 도달했고 주문이 없으면 완료
                    if (Grill.BurgerCount >= Define.GRILL_MAX_BURGER_COUNT && !Grill.HasOrders())
                    {
                        return true;
                    }
                    
                    // 조리 중이면 계속 대기
                    return false;
                });

                // 일감 점유 해제 (그릴에 계속 머물면서 다음 주문을 기다림)
                Jobs[(int)EMainCounterJob.CookBurger] = null;
            }

            // 카운터 계산대.
            if (ShouldDoJob(EMainCounterJob.CounterCashier))
            {
                foundJob = true;

                // 일감 점유.
                Jobs[(int)EMainCounterJob.CounterCashier] = wc;

                // 계산대로 이동.
                wc.SetDestination(Counter.CashierWorkerPos.position);

                // 가는중.
                yield return new WaitUntil(() => wc.HasArrivedAtDestination);

                // 계산대 도착했으면 일정 시간 대기.
                wc.transform.rotation = Counter.CashierWorkerPos.rotation;
                
                // 알바생이 Counter 존에 있는 동안 대기 (OnBurgerTriggerStart에서 진행바가 시작됨)
                // 알바생이 Counter 존에서 나가면 CurrentCashierWorker가 해제됨
                yield return new WaitUntil(() => Counter.CurrentCashierWorker != wc);

                // 일감 점유 해제.
                Jobs[(int)EMainCounterJob.CounterCashier] = null;
            }

            // 테이블 청소.
            if (ShouldDoJob(EMainCounterJob.CleanTable))
            {
                Table table = Tables.Where(t => t.TableState == ETableState.Dirty).FirstOrDefault();
                if (table == null)
                    continue;

                foundJob = true;

                // 일감 점유.
                Jobs[(int)EMainCounterJob.CleanTable] = wc;

                // 테이블로 이동.
                wc.SetDestination(table.WorkerPos.position, () =>
                {
                    wc.transform.rotation = table.WorkerPos.rotation;
                });

                // 가는중.
                yield return new WaitUntil(() => wc.HasArrivedAtDestination);

                // 테이블 도착했으면 일정 시간 대기.
                wc.transform.rotation = table.WorkerPos.rotation;
                yield return new WaitUntil(() => table.TableState != ETableState.Dirty);

                // 쓰레기통으로 이동.
                wc.SetDestination(TrashCan.WorkerPos.position, () =>
                {
                    wc.transform.rotation = TrashCan.WorkerPos.rotation;
                });

                // 쓰레기통 도착했으면 일정 시간 대기.
                wc.transform.rotation = table.WorkerPos.rotation;
                yield return new WaitUntil(() => wc.IsServing == false);

                // 일감 점유 해제.
                Jobs[(int)EMainCounterJob.CleanTable] = null;
            }

            // 일이 없으면 반납.
            if (foundJob == false)
            {
                RemoveWorker(wc);
            }
        }
    }

    public bool HasEmptyCleanTable()
    {
        foreach (Table table in Tables)
        {
            if (table.TableState == ETableState.None)
                return true;
        }

        return false;
    }

    // 레벨별 언락할 프랍 정의 (하드코딩)
    // 레벨을 키로, 언락할 프랍의 위치와 프리팹을 값으로 사용
    [SerializeField]
    private List<LevelUnlockData> _levelUnlockData = new List<LevelUnlockData>();

    [System.Serializable]
    public class LevelUnlockData
    {
        public int Level; // 언락할 레벨
        public Vector3 Position; // 프랍을 생성할 위치
        public GameObject PropPrefab; // 생성할 프랍 프리팹 (또는 기존 프랍 참조)
        public UnlockableBase Prop; // 기존 씬에 있는 프랍 참조 (Prefab이 null일 때 사용)
        public long UnlockCost = 300; // 언락 비용
    }

    /// <summary>
    /// 경험치 변경 시 호출됩니다. 레벨에 따라 프랍을 언락합니다.
    /// </summary>
    private void OnExpChanged()
    {
        if (GameManager.Instance == null || GameManager.Instance.Restaurant == null)
            return;

        int currentLevel = GameManager.Instance.Level;
        CheckAndUnlockPropsByLevel(currentLevel);
    }

    /// <summary>
    /// 게임 시작 시 저장된 레벨에 따라 프랍을 생성하고 상태를 불러옵니다.
    /// </summary>
    public void LoadPropsByLevel()
    {
        if (GameManager.Instance == null || GameManager.Instance.Restaurant == null)
            return;

        int currentLevel = GameManager.Instance.Level;
        Restaurant restaurant = GetComponent<Restaurant>();
        
        if (restaurant == null || SaveManager.Instance == null || SaveManager.Instance.SaveData == null)
            return;

        int stageNum = restaurant.StageNum;
        if (stageNum < 0 || stageNum >= SaveManager.Instance.SaveData.Restaurants.Count)
            return;

        RestaurantData restaurantData = SaveManager.Instance.SaveData.Restaurants[stageNum];
        if (restaurantData.UnlockableStates == null)
        {
            restaurantData.UnlockableStates = new List<UnlockableStateData>();
        }

        // 현재 레벨까지의 모든 프랍 생성
        for (int level = 1; level <= currentLevel; level++)
        {
            foreach (var unlockData in _levelUnlockData)
            {
                if (unlockData.Level == level)
                {
                    UnlockableBase prop = GetOrCreateProp(unlockData);
                    if (prop != null)
                    {
                        // 프리팹으로 생성한 경우 위치 설정
                        if (unlockData.PropPrefab != null && unlockData.Prop == null)
                        {
                            prop.transform.position = unlockData.Position;
                            
                            // ConstructionArea 위치도 프랍 위치에 맞춤
                            if (prop.ConstructionArea != null)
                            {
                                prop.ConstructionArea.transform.position = unlockData.Position;
                            }
                        }
                    }
                }
            }
        }

        // Restaurant.Props가 업데이트되었으므로 다시 수집
        restaurant.Props = GetComponentsInChildren<UnlockableBase>().ToList();

        // 저장된 상태를 모든 프랍에 적용
        for (int i = 0; i < restaurant.Props.Count; i++)
        {
            UnlockableBase prop = restaurant.Props[i];
            if (prop == null)
                continue;

            // UnlockableStates 리스트 확장
            while (restaurantData.UnlockableStates.Count <= i)
            {
                restaurantData.UnlockableStates.Add(new UnlockableStateData());
            }

            UnlockableStateData savedState = restaurantData.UnlockableStates[i];
            
            // 저장된 상태 적용
            prop.SetInfo(savedState);
            
            // UI_ConstructionArea에 비용 설정 (ProcessingConstruction 상태인 경우)
            if (prop.State == EUnlockedState.ProcessingConstruction)
            {
                // 해당 프랍의 UnlockData 찾기
                LevelUnlockData unlockData = _levelUnlockData.Find(data => 
                    (data.Prop != null && data.Prop == prop) || 
                    (data.PropPrefab != null && prop.gameObject.name.Contains(data.PropPrefab.name)));
                
                if (unlockData != null && prop.ConstructionArea != null)
                {
                    prop.ConstructionArea.TotalUpgradeMoney = unlockData.UnlockCost;
                    prop.ConstructionArea.RefreshUI();
                }
            }
        }
    }

    /// <summary>
    /// 레벨에 따라 프랍을 언락합니다.
    /// _levelUnlockData에서 정의된 레벨별 프랍을 언락합니다.
    /// </summary>
    private void CheckAndUnlockPropsByLevel(int level)
    {
        if (_levelUnlockData == null || _levelUnlockData.Count == 0)
            return;

        // 현재 레벨에 해당하는 언락 데이터 찾기
        foreach (var unlockData in _levelUnlockData)
        {
            if (unlockData.Level == level)
            {
                UnlockableBase prop = GetOrCreateProp(unlockData);
                if (prop != null)
                {
                    // 프랍이 Hidden 상태면 ProcessingConstruction으로 변경
                    if (prop.State == EUnlockedState.Hidden)
                    {
                        prop.SetUnlockedState(EUnlockedState.ProcessingConstruction);
                        
                        // UI_ConstructionArea에 비용 설정
                        if (prop.ConstructionArea != null)
                        {
                            prop.ConstructionArea.TotalUpgradeMoney = unlockData.UnlockCost;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 언락 데이터에서 프랍을 가져오거나 생성합니다.
    /// </summary>
    private UnlockableBase GetOrCreateProp(LevelUnlockData unlockData)
    {
        // 기존 프랍 참조가 있으면 사용
        if (unlockData.Prop != null)
        {
            return unlockData.Prop;
        }

        // 프리팹이 있으면 위치에 생성
        if (unlockData.PropPrefab != null)
        {
            GameObject propObj = Instantiate(unlockData.PropPrefab, unlockData.Position, Quaternion.identity);
            UnlockableBase prop = propObj.GetComponent<UnlockableBase>();
            
            // 생성된 프랍을 Restaurant의 Props 리스트에 추가
            Restaurant restaurant = GetComponent<Restaurant>();
            if (restaurant != null && prop != null)
            {
                // SaveData에 UnlockableStateData 추가
                if (SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
                {
                    int stageNum = restaurant.StageNum;
                    if (stageNum >= 0 && stageNum < SaveManager.Instance.SaveData.Restaurants.Count)
                    {
                        RestaurantData restaurantData = SaveManager.Instance.SaveData.Restaurants[stageNum];
                        if (restaurantData.UnlockableStates == null)
                        {
                            restaurantData.UnlockableStates = new List<UnlockableStateData>();
                        }
                        restaurantData.UnlockableStates.Add(new UnlockableStateData
                        {
                            State = EUnlockedState.Hidden,
                            SpentMoney = 0
                        });
                        
                        // 프랍에 상태 데이터 설정
                        prop.SetInfo(restaurantData.UnlockableStates[restaurantData.UnlockableStates.Count - 1]);
                    }
                }
                
                restaurant.Props.Add(prop);
            }
            
            return prop;
        }

        return null;
    }
    #endregion
}
