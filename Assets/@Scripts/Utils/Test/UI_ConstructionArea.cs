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
    
    private float _lastSoundPlayTime = 0f;
    private const float SOUND_PLAY_INTERVAL = 0.1f; 

    void Start()
    {
        GetComponent<WorkerInteraction>().OnInteraction = OnWorkerInteraction;
        GetComponent<WorkerInteraction>().InteractInterval = Define.CONSTRUCTION_UPGRADE_INTERVAL;

        // TODO : 데이터 참고해서 업그레이드 비용 설정.
        TotalUpgradeMoney = 300;
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

        // 돈이 충분할 때만 사운드 재생 (사운드 재생 간격 제한)
        float currentTime = Time.time;
        if (currentTime - _lastSoundPlayTime >= SOUND_PLAY_INTERVAL)
        {
            SoundManager.Instance.PlaySFX("SFX_Stack");
            _lastSoundPlayTime = currentTime;
        }

        GameManager.Instance.Money -= money;
        SpentMoney += money;

        if (SpentMoney >= TotalUpgradeMoney)
        {
            SpentMoney = TotalUpgradeMoney;

            // 해금 완료 사운드 재생
            SoundManager.Instance.PlaySFX("SFX_Levelup");
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
