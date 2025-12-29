using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Define
{
    public enum EEventType
    {
        MoneyChanged,
        HireWorker,
        UnlockProp,
        WorkerBoosterUpgraded,
        WorkerSpeedUpgraded,

        MaxCount
    }

    public enum EAnimState
    {
        None,
        Idle,
        Move,
        Eating,
    }

    public enum EObjectType
    {
        None,
        Trash,
        Burger,
        Money,
    }

    public enum EGuestState
    {
        None,
        Queuing,
        Serving,
        Eating,
        Leaving,
    }

    public enum ETableState
    {
        None,
        Reserved,
        Eating,
        Dirty,
    }

    public enum EMainCounterJob
    {
        MoveBurger,
        CounterCashier,
        CleanTable,
        CookBurger,

        MaxCount,
    }

    public enum ESoundType
    {
        None,
        BGM,
        SFX,
    }


    public const float GRILL_SPAWN_BURGER_INTERVAL = 3f;
    public const int GRILL_MAX_BURGER_COUNT = 2;
    public const int MAX_BURGER_ADD_COUNT = 2;

    public const float CONSTRUCTION_UPGRADE_INTERVAL = 0.01f;
    public const float MONEY_SPAWN_INTERVAL = 0.1f;
    public const float TRASH_SPAWN_INTERVAL = 0.1f;
    public const float GUEST_SPAWN_INTERVAL = 1f;
    public const int GUEST_MAX_ORDER_BURGER_COUNT = 2;
    public const int MAX_WORKER_COUNT = 3;

    // Worker Booster 관련 상수
    public const int MAX_WORKER_BOOSTER_LEVEL = 3; // 최대 부스터 레벨
    public const float WORKER_BOOSTER_TIME_REDUCTION = 5f; // 레벨당 시간 감소량 (초)
    public const float BASE_WORKER_WORK_DURATION = 20f; // 기본 작업 시간 (초)
    
    // Worker Speed 관련 상수
    public const int MAX_WORKER_SPEED_LEVEL = 3; // 최대 스피드 레벨
    public const float WORKER_SPEED_INCREASE = 0.3f; // 레벨당 속도 증가량
    public const float BASE_WORKER_MOVE_SPEED = 3f; // 기본 이동 속도
    
    public static Vector3 WORKER_SPAWN_POS = new Vector3(24.33f, 0, 21.42f);
    public static Vector3 GUEST_LEAVE_POS = new Vector3(0, 0, 0);


    // 애니메이션 관련
    public static int IDLE = Animator.StringToHash("Idle");
    public static int MOVE = Animator.StringToHash("Move");
    public static int SERVING_IDLE = Animator.StringToHash("ServingIdle");
    public static int SERVING_MOVE = Animator.StringToHash("ServingMove");
    public static int EATING = Animator.StringToHash("Eating");

    // 주문 관련 상수
    public const int ORDER_MAX_PATTY_COUNT = 2;
    public const int ORDER_MAX_CHEESE_COUNT = 2;
    public const int ORDER_MAX_VEGGIES_TOTAL = 4;
    public const int ORDER_MAX_SAUCE1_COUNT = 2;
    public const int ORDER_MAX_SAUCE2_COUNT = 2;
    public const float ORDER_TIME_LIMIT = 10f; // 주문 시간 제한 (초)

    // 가격(단가)
    public const int PRICE_BREAD = 3;   // 빵 1개
    public const int PRICE_TOMATO = 3;   // 토마토 1장
    public const int PRICE_PATTY = 10;   // 고기 1장
    public const int PRICE_LETTUCE = 3;  // 양상추 1장
    public const int PRICE_SAUCE1 = 2;   // 소스1 1회
    public const int PRICE_SAUCE2 = 2;   // 소스2 1회

    // 주문/재료 정의
    public enum EBreadType
    {
        None,
        Plain,
    }

    public enum EPattyType
    {
        None,
        Beef,
        Chicken,
    }

    public enum EVeggieType
    {
        None,
        Lettuce,
        Tomato,
    }

    public enum ESauceType
    {
        None,
        Sauce1,
        Sauce2,
    }

    public struct BurgerRecipe
    {
        // 빵은 상/하단 2개 고정으로 간주 (Bread는 종류만 표시)
        public EBreadType Bread;
        public EPattyType Patty;
        public int PattyCount;   // 0 ~ ORDER_MAX_PATTY_COUNT
        // 야채는 중복 원소 허용하여 개수 표현
        public List<EVeggieType> Veggies;
        // 소스 수량
        public int Sauce1Count;  // 0 ~ ORDER_MAX_SAUCE1_COUNT
        public int Sauce2Count;  // 0 ~ ORDER_MAX_SAUCE2_COUNT
    }
}