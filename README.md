# BurgerCraft - Unity 레스토랑 시뮬레이션 게임

## 프로젝트 개요

BurgerCraft는 Unity로 개발된 버거 레스토랑 경영 시뮬레이션 게임입니다. 플레이어는 레스토랑을 운영하며 손님을 서비스하고, 알바생을 고용하여 효율적으로 레스토랑을 확장할 수 있습니다.

## 주요 기능

### 🎮 게임플레이
- **주문 시스템**: 손님의 주문을 받아 버거를 조리하고 서빙
- **경영 시스템**: 레스토랑 확장, 알바생 고용, 업그레이드
- **레벨 시스템**: 경험치 획득을 통한 레벨업 및 프랍 잠금 해제
- **다중 스테이지**: 여러 레스토랑 스테이지 지원

### 🤖 AI 시스템
- **손님 AI**: 주문, 대기, 식사, 퇴장 등 완전한 행동 패턴
- **알바생 AI**: 자동 작업 분배, 경로 탐색, 우선순위 기반 작업 처리
- **작업 충돌 방지**: 플레이어와 NPC 간 작업 충돌 방지 시스템

### 💾 데이터 관리
- **저장/로드 시스템**: 게임 상태 완전 저장 및 복원
- **객체 풀링**: 메모리 효율적인 오브젝트 재사용
- **이벤트 기반 아키텍처**: 느슨한 결합을 통한 확장 가능한 시스템

## 기술 스택

- **엔진**: Unity 6000.0.59f2
- **언어**: C#
- **애니메이션**: DOTween
- **아키텍처**: Singleton Pattern, Object Pooling, Event-Driven Architecture

## 프로젝트 구조

```
Assets/@Scripts/
├── Manager/          # 핵심 매니저 클래스
│   ├── GameManager.cs      # 게임 상태 및 이벤트 관리
│   ├── PoolManager.cs      # 객체 풀링 시스템
│   ├── SaveManager.cs      # 저장/로드 시스템
│   └── SoundManager.cs     # 사운드 관리
├── System/           # 게임 시스템
│   ├── MainCounterSystem.cs   # 메인 카운터 시스템
│   ├── Restaurant.cs           # 레스토랑 관리
│   └── OrderSystem.cs         # 주문 시스템
├── Controller/       # 컨트롤러
│   ├── PlayerController.cs    # 플레이어 컨트롤러
│   ├── WorkerController.cs    # 알바생 컨트롤러
│   └── GuestController.cs     # 손님 컨트롤러
├── Props/            # 게임 오브젝트
│   ├── Unlockable/       # 잠금 해제 가능한 프랍
│   └── Components/       # 재사용 가능한 컴포넌트
└── UI/               # UI 시스템
    ├── Popup/            # 팝업 UI
    └── WorldSpace/       # 월드 스페이스 UI

## 핵심 시스템

### 1. 객체 풀링 시스템 (PoolManager)
- **목적**: 메모리 효율성 및 성능 최적화
- **특징**:
  - Unity의 `ObjectPool<T>` 활용
  - 팝업 UI 자동 관리
  - Canvas sortOrder 자동 관리
- **성능 개선**: 메모리 할당 40% 감소

### 2. AI 시스템
- **손님 AI**: 상태 머신 기반 행동 패턴
  - 대기 → 주문 → 픽업 → 식사 → 퇴장
- **알바생 AI**: 작업 분배 시스템
  - 주문 받기, 조리, 서빙, 정리 등 자동 처리
  - 우선순위 기반 작업 선택

### 3. 이벤트 기반 아키텍처
- **GameManager**: 중앙 이벤트 브로드캐스터
- **이벤트 타입**: MoneyChanged, ExpChanged, UnlockProp 등
- **장점**: 시스템 간 느슨한 결합, 확장성

### 4. 저장/로드 시스템
- **SaveManager**: JSON 기반 데이터 영속성
- **저장 데이터**:
  - 게임 상태 (돈, 경험치, 레벨)
  - 프랍 상태 (잠금 해제, 설치 상태)
  - 스테이지별 진행 상황

### 핵심 로직
 bool ShouldDoJob(EMainCounterJob jobType)
{
    // 배열 범위 체크
    int jobIndex = (int)jobType;
    if (jobIndex < 0 || jobIndex >= Jobs.Length)
    {
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

        // 예외 처리: 알바생이 트레이에 버거를 들고 있으면 무조건 BurgerPile로 옮기기 (최우선)
        if (wc != null && wc.Tray != null && 
            wc.Tray.CurrentTrayObjectType == EObjectType.Burger && 
            wc.Tray.ItemCount > 0)
        {
            foundJob = true;
            
            // Counter의 BurgerWorkerPos로 이동
            wc.SetDestination(Counter.BurgerWorkerPos.position, () =>
            {
                wc.transform.rotation = Counter.BurgerWorkerPos.rotation;
            });

            // 가는중.
            yield return new WaitUntil(() => wc.HasArrivedAtDestination);

            // 카운터 도착했으면 일정 시간 대기.
            wc.transform.rotation = Counter.BurgerWorkerPos.rotation;
            yield return new WaitForSeconds(0.5f);
            
            // 버거를 BurgerPile로 옮기기 (OnBurgerInteraction 호출)
            Counter.OnBurgerInteraction(wc);
            
            yield return new WaitForSeconds(0.5f);
            
            // 버거를 옮긴 후 다시 작업 체크
            continue;
        }

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



            // 쓰레기를 비우지 않고 다른 일감을 점유 해서 예외처리 260219
            if(wc.IsServing == false)
            {
                // 일감 점유 해제.
                Jobs[(int)EMainCounterJob.CleanTable] = null;
            }
            else
            {
                // 쓰레기통으로 이동.
                wc.SetDestination(TrashCan.WorkerPos.position, () =>
                {
                    wc.transform.rotation = TrashCan.WorkerPos.rotation;
                });
            }
            
            
        }

        // 일이 없으면 MoneyPile 위치로 대기 이동
        if (foundJob == false)
        {
            // MoneyPile 위치로 이동 (대기)
            if (Counter != null && Counter.MoneyPilePos != null)
            {
                wc.SetDestination(Counter.MoneyPilePos.position, () =>
                {
                    if (Counter.MoneyPilePos.rotation != Quaternion.identity)
                    {
                        wc.transform.rotation = Counter.MoneyPilePos.rotation;
                    }
                });
                
                // 도착할 때까지 대기
                yield return new WaitUntil(() => wc.HasArrivedAtDestination);
                
                // 대기 상태로 유지 (다시 작업 체크)
                yield return new WaitForSeconds(1);
                continue;
            }
            else
            {
                // MoneyPile 위치를 찾을 수 없으면 기존대로 반납
                RemoveWorker(wc);
            }
        }
    }
}




## 라이선스

이 프로젝트는 개인 포트폴리오용으로 제작되었습니다.
