using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitStatView : MonoBehaviour, INetworkUnitHealth
{
    [SerializeField] private GameObject healthObject;
    [SerializeField] private Image amountFill;
    [SerializeField] private TextMeshProUGUI damageCounterMaster;
    [SerializeField] private Image heartFillImage;

    private int unitID;
    private GameView gameView;

    private float lastHealthDisplayTime;

    public int UnitID { get { return unitID; } }

    private LinkedList<TextMeshProUGUI> damageCounterInstances;

    public void Initialize(GameView gameView, int unitID, int ownerConnectionIndex)
    {
        this.gameView = gameView;
        this.unitID = unitID;

        amountFill.fillAmount = 1;
        damageCounterMaster.gameObject.SetActive(false);

        damageCounterInstances = new LinkedList<TextMeshProUGUI>();
        healthObject.gameObject.SetActive(false);

        if (ownerConnectionIndex == LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value)
        {
            heartFillImage.color = LocalGame.Instance.GameData.UnitColors[0];
        }
        else
        {
            heartFillImage.color = LocalGame.Instance.GameData.UnitColors[1];
        }
    }

    void Update()
    {
        var dcf = damageCounterInstances.First;
        while (dcf != null)
        {
            var next = dcf.Next;
            dcf.Value.transform.GetComponent<RectTransform>().anchoredPosition += Vector2.up * 10.0f * Time.deltaTime;
            if (dcf.Value.GetComponent<RectTransform>().anchoredPosition.y > 0) 
            {                
                Destroy(dcf.Value.gameObject);
                damageCounterInstances.Remove(dcf);                
            }
            dcf = next;
        }
        if (lastHealthDisplayTime > 0 && Time.time - lastHealthDisplayTime > LocalGame.Instance.GameData.UnitAttackIntervalSeconds + 0.5f)
        {
            healthObject.gameObject.SetActive(false);
            lastHealthDisplayTime = -1;
        }
    }

    public void OnNetworkUnitHealthChange(int unitID, float oldValue, float newValue)
    {
        if (newValue == LocalGame.Instance.GameData.UnitMaxHealth) return;
        if (this.unitID == unitID)
        {            
            float ratio = newValue / LocalGame.Instance.GameData.UnitMaxHealth;
            amountFill.fillAmount = ratio;

            float delta = newValue - oldValue;            
            var damageCounterInstance = Instantiate(damageCounterMaster, transform);
            damageCounterInstance.gameObject.SetActive(true);
            damageCounterInstance.GetComponent<RectTransform>().anchoredPosition += new Vector2(UnityEngine.Random.Range(-50, 50), 0);

            if (delta > 0) damageCounterInstance.text = $"+{Mathf.FloorToInt(delta)}";
            else if (delta < 0) damageCounterInstance.text = $"-{Mathf.FloorToInt(Mathf.Abs(delta))}";            
            else damageCounterInstance.text = $"No Damage";

            // Debug.Log($"anchor position: {damageCounterInstance.GetComponent<RectTransform>().anchoredPosition}");
            
            damageCounterInstances.AddLast(damageCounterInstance);
            lastHealthDisplayTime = Time.time;

            healthObject.gameObject.SetActive(true);
        }
    }
}
