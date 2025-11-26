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
public class UI_OrderSystem : MonoBehaviour
{
    public static Define.BurgerRecipe GenerateRandomRecipe()
    {
        // 빵 2개 고정: Bread는 종류만 선택, 개수는 고정 컨벤션
        var recipe = new Define.BurgerRecipe
        {
            Bread = GetRandomBread(),
            Patty = GetRandomPatty(),
            PattyCount = GetRandomCount(0, Define.ORDER_MAX_PATTY_COUNT),
            Cheese = GetRandomCheese(),
            CheeseCount = GetRandomCount(0, Define.ORDER_MAX_CHEESE_COUNT),
            Veggies = GenerateRandomVeggiesMulti(),
            Sauce1Count = GetRandomCount(0, Define.ORDER_MAX_SAUCE1_COUNT),
            Sauce2Count = GetRandomCount(0, Define.ORDER_MAX_SAUCE2_COUNT),
        };
        // 야채 총합 상한 보정
        if (recipe.Veggies.Count > Define.ORDER_MAX_VEGGIES_TOTAL)
            recipe.Veggies = recipe.Veggies.Take(Define.ORDER_MAX_VEGGIES_TOTAL).ToList();
        return recipe;
    }

    public static bool IsMatch(Define.BurgerRecipe crafted, Define.BurgerRecipe requested)
    {
        if (crafted.Bread != requested.Bread) return false; // 빵 종류만 비교 (수량은 2개 고정 컨벤션)
        if (crafted.Patty != requested.Patty) return false;
        if (crafted.PattyCount != requested.PattyCount) return false;
        if (crafted.Cheese != requested.Cheese) return false;
        if (crafted.CheeseCount != requested.CheeseCount) return false;
        if (crafted.Sauce1Count != requested.Sauce1Count) return false;
        if (crafted.Sauce2Count != requested.Sauce2Count) return false;

        if (crafted.Veggies == null || requested.Veggies == null) return false;
        if (crafted.Veggies.Count != requested.Veggies.Count) return false;

        // 야채: 멀티셋 비교(중복 개수 포함)
        var temp = new List<Define.EVeggieType>(crafted.Veggies);
        foreach (var v in requested.Veggies)
        {
            if (!temp.Remove(v))
                return false;
        }
        return temp.Count == 0;
    }

    public static string GetOrderText(Define.BurgerRecipe recipe)
    {
        var parts = new List<string>();
        parts.Add($"빵:{recipe.Bread} x2");
        parts.Add($"패티:{(recipe.Patty == Define.EPattyType.None || recipe.PattyCount == 0 ? "없음" : $"{recipe.Patty} x{recipe.PattyCount}")}");
        parts.Add($"치즈:{(recipe.Cheese == Define.ECheeseType.None || recipe.CheeseCount == 0 ? "없음" : $"{recipe.Cheese} x{recipe.CheeseCount}")}");

        if (recipe.Veggies != null && recipe.Veggies.Count > 0)
        {
            var grouped = recipe.Veggies
                .Where(v => v != Define.EVeggieType.None)
                .GroupBy(v => v)
                .Select(g => $"{g.Key} x{g.Count()}");
            parts.Add($"야채:{string.Join(",", grouped)}");
        }
        else
        {
            parts.Add("야채:없음");
        }

        if (recipe.Sauce1Count > 0 || recipe.Sauce2Count > 0)
        {
            parts.Add($"소스1 x{recipe.Sauce1Count}");
            parts.Add($"소스2 x{recipe.Sauce2Count}");
        }
        else
        {
            parts.Add("소스:없음");
        }
        return string.Join(" / ", parts);
    }

    // 가격 계산 및 영수증 생성 ------------------
    public static int CalculatePrice(Define.BurgerRecipe recipe)
    {
        int total = 0;
        // 빵 2개 고정
        total += Define.PRICE_BREAD * 2;
        // 패티
        total += recipe.PattyCount * Define.PRICE_PATTY;
        // 치즈: 치즈 타입이 None이면 0원
        if (recipe.Cheese != Define.ECheeseType.None)
            total += recipe.CheeseCount * 0; // 치즈 가격이 정의되어 있지 않음 -> 필요 시 추가
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
                    case Define.EVeggieType.Onion:
                        // 양파 가격 미정 -> 0 처리 또는 상수 추가 가능
                        break;
                    case Define.EVeggieType.Pickle:
                        // 피클 가격 미정 -> 0 처리 또는 상수 추가 가능
                        break;
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

    public static Receipt BuildReceipt(Define.BurgerRecipe recipe)
    {
        var receipt = new Receipt();
        receipt.id = System.Guid.NewGuid().ToString("N");
        receipt.createdAt = System.DateTime.Now;
        receipt.burger = BuildCraftedBurger(recipe);
        return receipt;
    }

    private static Define.EBreadType GetRandomBread()
    {
        Define.EBreadType[] pool = { Define.EBreadType.Sesame, Define.EBreadType.Plain };
        return pool[Random.Range(0, pool.Length)];
    }

    private static Define.EPattyType GetRandomPatty()
    {
        Define.EPattyType[] pool = { Define.EPattyType.None, Define.EPattyType.Beef, Define.EPattyType.Chicken };
        return pool[Random.Range(0, pool.Length)];
    }

    private static Define.ECheeseType GetRandomCheese()
    {
        Define.ECheeseType[] pool = { Define.ECheeseType.None, Define.ECheeseType.Cheddar, Define.ECheeseType.Swiss };
        return pool[Random.Range(0, pool.Length)];
    }

    private static int GetRandomCount(int minInclusive, int maxInclusive)
    {
        return Random.Range(minInclusive, maxInclusive + 1);
    }

    // 야채 다중/중복 허용 생성 (각 야채를 0~2장 랜덤으로 추가, 총합 상한 별도 보정)
    private static List<Define.EVeggieType> GenerateRandomVeggiesMulti()
    {
        var veggies = new List<Define.EVeggieType>();
        var types = new[] { Define.EVeggieType.Lettuce, Define.EVeggieType.Tomato, Define.EVeggieType.Onion, Define.EVeggieType.Pickle };
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
