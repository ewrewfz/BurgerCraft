using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 1. 손님의 주문 텍스트를 저장하는 리스트 생성
// 2. 텍스트 == 실제 주문
//  - 주문 실패 시 UI 표출 (3회 실패 시 손님 퇴장)
//    - 추가 요소 : 손님 명성도에 따른 시스템
// 3. 랜덤요소 (빵은 유지 하되 다른 재료들은 손님들에 따라 랜덤하게 부여)
// 4. 주문 완료 후 제작 시 완성된 데이터를 구조체를 통한 제작 진행
//  - 버거 완성본에 따라 버거 모델링 변경 및 고유 값 부여
//  - 불일치 시 실패 UI와 표출
//  - 3회 이상 불일치 시 해당 손님 떠남
// 5. 손님이 요구하는 버거와 일치 시 판매 완료

// 6. 버거를 가지고 테이블로 이동
// 7. 테이블 청소 시스템 
// -> 제한 시간 내 테이블 위에 있는 쓰레기 터치 하기 (쟁반 쌓기)
public class UI_OrderSystem : GameManager
{
    // 현재 제작 중인 레시피를 초기화하는 헬퍼 메서드
    public static Define.BurgerRecipe CreateEmptyRecipe()
    {
        return new Define.BurgerRecipe
        {
            Bread = Define.EBreadType.Plain,
            Patty = Define.EPattyType.None,
            PattyCount = 0,
            Veggies = new List<Define.EVeggieType>(),
            Sauce1Count = 0,
            Sauce2Count = 0,
        };
    }
    
    public static Define.BurgerRecipe GenerateRandomRecipe()
    {
        // 빵 2개 고정: Bread는 종류만 선택, 개수는 고정 컨벤션
        int pattyCount = GetRandomCount(0, Define.ORDER_MAX_PATTY_COUNT);
        var recipe = new Define.BurgerRecipe
        {
            Bread = GetRandomBread(),
            Patty = pattyCount > 0 ? GetRandomPatty() : Define.EPattyType.None, // 개수가 0이면 None
            PattyCount = pattyCount,
            Veggies = GenerateRandomVeggiesMulti(),
            Sauce1Count = GetRandomCount(0, Define.ORDER_MAX_SAUCE1_COUNT),
            Sauce2Count = GetRandomCount(0, Define.ORDER_MAX_SAUCE2_COUNT),
        };
        
        // 안전장치: pattyCount와 Patty 값의 일관성 보장
        if (recipe.PattyCount == 0)
        {
            recipe.Patty = Define.EPattyType.None;
        }
        else if (recipe.Patty == Define.EPattyType.None)
        {
            // pattyCount > 0인데 Patty가 None이면 랜덤 패티로 설정
            recipe.Patty = Define.EPattyType.Beef;
        }
        
        // 야채 총합 상한 보정
        if (recipe.Veggies.Count > Define.ORDER_MAX_VEGGIES_TOTAL)
            recipe.Veggies = recipe.Veggies.Take(Define.ORDER_MAX_VEGGIES_TOTAL).ToList();
        return recipe;
    }

    public static bool IsMatch(Define.BurgerRecipe crafted, Define.BurgerRecipe requested)
    {
        // 빵 비교
        if (crafted.Bread != requested.Bread)
        {
            Debug.LogWarning($"[매칭 실패] 빵 불일치: crafted={crafted.Bread}, requested={requested.Bread}");
            return false;
        }
        
        // 패티 비교
        if (crafted.PattyCount != requested.PattyCount)
        {
            Debug.LogWarning($"[매칭 실패] 패티 개수 불일치: crafted={crafted.PattyCount}, requested={requested.PattyCount}");
            return false;
        }
        if (crafted.PattyCount > 0 && crafted.Patty != requested.Patty)
        {
            Debug.LogWarning($"[매칭 실패] 패티 종류 불일치: crafted={crafted.Patty}, requested={requested.Patty}");
            return false;
        }
        
        // 소스 비교
        if (crafted.Sauce1Count != requested.Sauce1Count)
        {
            Debug.LogWarning($"[매칭 실패] 소스1 개수 불일치: crafted={crafted.Sauce1Count}, requested={requested.Sauce1Count}");
            return false;
        }
        if (crafted.Sauce2Count != requested.Sauce2Count)
        {
            Debug.LogWarning($"[매칭 실패] 소스2 개수 불일치: crafted={crafted.Sauce2Count}, requested={requested.Sauce2Count}");
            return false;
        }
        
        // 야채 비교: null 체크 및 None 값 제거 후 정렬하여 비교
        var craftedVeggies = (crafted.Veggies ?? new List<Define.EVeggieType>())
            .Where(v => v != Define.EVeggieType.None)
            .OrderBy(v => v)
            .ToList();
        var requestedVeggies = (requested.Veggies ?? new List<Define.EVeggieType>())
            .Where(v => v != Define.EVeggieType.None)
            .OrderBy(v => v)
            .ToList();
        
        if (craftedVeggies.Count != requestedVeggies.Count)
        {
            Debug.LogWarning($"[매칭 실패] 야채 개수 불일치: crafted={craftedVeggies.Count}개 ({string.Join(", ", craftedVeggies)}), requested={requestedVeggies.Count}개 ({string.Join(", ", requestedVeggies)})");
            return false;
        }
        
        for (int i = 0; i < craftedVeggies.Count; i++)
        {
            if (craftedVeggies[i] != requestedVeggies[i])
            {
                Debug.LogWarning($"[매칭 실패] 야채 종류 불일치: crafted[{i}]={craftedVeggies[i]}, requested[{i}]={requestedVeggies[i]}");
                return false;
            }
        }
        
        return true;
    }

 

    // 자연어 주문 텍스트 생성 (이미지 스타일) - 문장 리스트 반환
    public static List<string> GenerateNaturalOrderPhrases(Define.BurgerRecipe recipe)
    {
        var phrases = new List<string>();
        
        // 인사말 시작
        string[] greetings = { 
            "햄버거 주문하고 싶은데요",
            "햄버거 주문할게요",
        };
        phrases.Add(greetings[Random.Range(0, greetings.Length)]);
        
        // 빵 관련 (기본적인 햄버거 모양)
        phrases.Add("기본적인 햄버거 모양이었으면 좋겠고");
        
        // 패티 관련
        if (recipe.PattyCount > 0)
        {
            string pattyName = recipe.Patty == Define.EPattyType.Beef ? "소고기" : "닭고기";
            if (recipe.PattyCount == 1)
            {
                phrases.Add($"{pattyName} 패티 하나 넣어주세요");
            }
            else
            {
                phrases.Add($"{pattyName} 패티 {recipe.PattyCount}개 넣어주세요");
            }
        }
        else
        {
            phrases.Add("고기는 빼고");
        }
        
        // 야채 관련 (프레시함)
        if (recipe.Veggies != null && recipe.Veggies.Count > 0)
        {
            var veggieGroups = recipe.Veggies
                .Where(v => v != Define.EVeggieType.None)
                .GroupBy(v => v)
                .ToList();
            
            if (veggieGroups.Count > 0)
            {
                var veggieList = new List<string>();
                foreach (var group in veggieGroups)
                {
                    string veggieName = group.Key == Define.EVeggieType.Lettuce ? "양상추" : "토마토";
                    int count = group.Count();
                    // 항상 개수를 정확하게 표시
                    if (count == 1)
                    {
                        veggieList.Add($"{veggieName} 1개");
                    }
                    else
                    {
                        veggieList.Add($"{veggieName} {count}개");
                    }
                }

                string[] randomvaggie = new string[]
                {
                    "약간의 프레쉬함을 위해",
                    "약간 양심 없어보이니까",
                    "양심상"
                };
                string randomPrefix = randomvaggie[Random.Range(0, randomvaggie.Length)];
                phrases.Add($"{randomPrefix} {string.Join("와 ", veggieList)} 추가해주세요");
            }
        }
        
        // 소스 관련 (감칠맛)
        if (recipe.Sauce1Count > 0 || recipe.Sauce2Count > 0)
        {
            var sauceList = new List<string>();
            if (recipe.Sauce1Count > 0)
            {
                // 항상 개수를 정확하게 표시
                if (recipe.Sauce1Count == 1)
                    sauceList.Add("소스1 1개");
                else
                    sauceList.Add($"소스1 {recipe.Sauce1Count}개");
            }
            if (recipe.Sauce2Count > 0)
            {
                // 항상 개수를 정확하게 표시
                if (recipe.Sauce2Count == 1)
                    sauceList.Add("소스2 1개");
                else
                    sauceList.Add($"소스2 {recipe.Sauce2Count}개");
            }
            phrases.Add($"감칠맛을 위해 {string.Join("와 ", sauceList)} 넣어주세요");
        }
        
        // 마무리 (너무 헤비하지 않은)
        if (recipe.PattyCount <= 1 && (recipe.Veggies == null || recipe.Veggies.Count <= 2))
        {
            phrases.Add("너무 헤비하지 않은 햄버거가 먹고싶네요");
        }
        else
        {
            phrases.Add("이렇게 만들어주세요");
        }
        
        return phrases;
    }
   

    // 가격 계산 및 영수증 생성 ------------------
    public static int CalculatePrice(Define.BurgerRecipe recipe)
    {
        int total = 0;
        // 빵 2개 고정
        total += Define.PRICE_BREAD * 2;
        // 패티
        total += recipe.PattyCount * Define.PRICE_PATTY;

        // 야채
        if (recipe.Veggies != null)
        {
            foreach (var v in recipe.Veggies)
            {
                switch (v)
                {
                    case Define.EVeggieType.Tomato:
                        total += Define.PRICE_TOMATO; break;
                    case Define.EVeggieType.Lettuce:
                        total += Define.PRICE_LETTUCE; break;
                }
            }
        }
        // 소스
        total += recipe.Sauce1Count * Define.PRICE_SAUCE1;
        total += recipe.Sauce2Count * Define.PRICE_SAUCE2;
        return total;
    }

    public static CraftedBurger BuildCraftedBurger(Define.BurgerRecipe recipe)
    {
        var crafted = new CraftedBurger();
        crafted.recipe = recipe;
        // 라인 구성
        crafted.lines.Add(new ReceiptLine { name = "빵", unitPrice = Define.PRICE_BREAD, quantity = 2, amount = Define.PRICE_BREAD * 2 });
        if (recipe.PattyCount > 0)
            crafted.lines.Add(new ReceiptLine { name = "고기", unitPrice = Define.PRICE_PATTY, quantity = recipe.PattyCount, amount = Define.PRICE_PATTY * recipe.PattyCount });
        if (recipe.Veggies != null)
        {
            int tomato = recipe.Veggies.Count(v => v == Define.EVeggieType.Tomato);
            int lettuce = recipe.Veggies.Count(v => v == Define.EVeggieType.Lettuce);
            if (tomato > 0)
                crafted.lines.Add(new ReceiptLine { name = "토마토", unitPrice = Define.PRICE_TOMATO, quantity = tomato, amount = Define.PRICE_TOMATO * tomato });
            if (lettuce > 0)
                crafted.lines.Add(new ReceiptLine { name = "양상추", unitPrice = Define.PRICE_LETTUCE, quantity = lettuce, amount = Define.PRICE_LETTUCE * lettuce });
            // 필요 시 양파/피클 가격 추가
        }
        if (recipe.Sauce1Count > 0)
            crafted.lines.Add(new ReceiptLine { name = "소스1", unitPrice = Define.PRICE_SAUCE1, quantity = recipe.Sauce1Count, amount = Define.PRICE_SAUCE1 * recipe.Sauce1Count });
        if (recipe.Sauce2Count > 0)
            crafted.lines.Add(new ReceiptLine { name = "소스2", unitPrice = Define.PRICE_SAUCE2, quantity = recipe.Sauce2Count, amount = Define.PRICE_SAUCE2 * recipe.Sauce2Count });

        crafted.totalPrice = 0;
        foreach (var l in crafted.lines) crafted.totalPrice += l.amount;
        return crafted;
    }

    private static Define.EBreadType GetRandomBread()
    {
        Define.EBreadType[] pool = {Define.EBreadType.Plain };
        return pool[Random.Range(0, pool.Length)];
    }

    private static Define.EPattyType GetRandomPatty()
    {
        // 일단 소고기로 고정
        return Define.EPattyType.Beef;
        // Define.EPattyType[] pool = { Define.EPattyType.None, Define.EPattyType.Beef, Define.EPattyType.Chicken };
        // return pool[Random.Range(0, pool.Length)];
    }

  

    private static int GetRandomCount(int minInclusive, int maxInclusive)
    {
        return Random.Range(minInclusive, maxInclusive + 1);
    }

    // 야채 다중/중복 허용 생성 (각 야채를 0~2장 랜덤으로 추가, 총합 상한 별도 보정)
    private static List<Define.EVeggieType> GenerateRandomVeggiesMulti()
    {
        var veggies = new List<Define.EVeggieType>();
        var types = new[] { Define.EVeggieType.Lettuce, Define.EVeggieType.Tomato};
        foreach (var t in types)
        {
            // 50% 확률로 채택
            if (Random.value < 0.5f)
            {
                int cnt = Random.Range(1, 3); // 1~2장
                for (int i = 0; i < cnt; i++) veggies.Add(t);
            }
        }
        return veggies;
    }
}
