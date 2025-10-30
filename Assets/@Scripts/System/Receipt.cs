using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ReceiptLine
{
    public string name;      // 항목명 (예: 빵, 토마토, 고기 등)
    public int unitPrice;    // 단가
    public int quantity;     // 수량
    public int amount;       // 금액 = 단가 * 수량
}

[Serializable]
public class CraftedBurger
{
    // 제작 완료된 버거 스냅샷 (직렬화용)
    public Define.BurgerRecipe recipe;
    public int totalPrice;                    // 총액
    public List<ReceiptLine> lines = new List<ReceiptLine>();
}

[Serializable]
public class Receipt
{
    public string id;                         // 영수증 ID (세션 고유값)
    public DateTime createdAt;                // 생성 시각
    public CraftedBurger burger;              // 제작 버거 정보(레시피+가격 내역)

    public int GetTotal() => burger != null ? burger.totalPrice : 0;
}
