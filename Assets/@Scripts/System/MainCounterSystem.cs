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
    #endregion
}
