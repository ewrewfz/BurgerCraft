using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

[RequireComponent(typeof(WorkerInteraction))]
public class UI_ConstructionArea : MonoBehaviour
{
    [SerializeField]
    Slider _slider;

    [SerializeField]
    TextMeshProUGUI _moneyText;

    public UnlockableBase Owner;
    public long TotalUpgradeMoney;
    public long MoneyRemaining => TotalUpgradeMoney - SpentMoney;

    public long SpentMoney
    {
        get { return Owner.SpentMoney; }
        set { Owner.SpentMoney = value; }
    }

    void Start()
    {
        GetComponent<WorkerInteraction>().OnInteraction = OnWorkerInteraction;
        GetComponent<WorkerInteraction>().InteractInterval = Define.CONSTRUCTION_UPGRADE_INTERVAL;

        // TODO : 데이터 참고해서 업그레이드 비용 설정.
        TotalUpgradeMoney = 50;
    }

    public void OnWorkerInteraction(WorkerController wc)
    {
        if (Owner == null)
            return;

        long money = (long)(TotalUpgradeMoney / (1 / Define.CONSTRUCTION_UPGRADE_INTERVAL));
        if (money == 0)
            money = 1;

        if (GameManager.Instance.Money < money)
            return;

        GameManager.Instance.Money -= money;
        SpentMoney += money;

        if (SpentMoney >= TotalUpgradeMoney)
        {
            SpentMoney = TotalUpgradeMoney;

            // 해금 완료.
            Owner.SetUnlockedState(EUnlockedState.Unlocked);

            GameManager.Instance.BroadcastEvent(EEventType.UnlockProp);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        _slider.value = SpentMoney / (float)TotalUpgradeMoney;
        _moneyText.text = Utils.GetMoneyText(MoneyRemaining);
    }
}
