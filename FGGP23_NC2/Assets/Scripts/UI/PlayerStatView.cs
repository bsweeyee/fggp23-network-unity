using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FGNetworkProgramming;

public class PlayerStatView : MonoBehaviour, IOnGameStateLose, IOnGameStateWin, IOnGameStateWaiting, IOnGameStateStart
{
    [SerializeField] private GameObject healtBar;
    [SerializeField] private Image amountFill;

    private GameView gameView;

    private int ownerConnectionIndex;

    private LinkedList<Canvas> damageTextInstances;  

    private float lastTimeDisplayTime;

    void Update()
    {
        if (lastTimeDisplayTime > 0 && Time.time - lastTimeDisplayTime > 2)
        {
            healtBar.gameObject.SetActive(false);
            lastTimeDisplayTime = -1;
        }

        var dcf = damageTextInstances.First;
        while (dcf != null)
        {
            var next = dcf.Next;
            dcf.Value.GetComponentInChildren<TextMeshProUGUI>().GetComponent<RectTransform>().anchoredPosition += Vector2.up * 10.0f * Time.deltaTime;
            if (dcf.Value.GetComponentInChildren<TextMeshProUGUI>().GetComponent<RectTransform>().anchoredPosition.y > 0) 
            {                
                Destroy(dcf.Value.gameObject);
                damageTextInstances.Remove(dcf);                
            }
            dcf = next;
        }
    }

    public void Initialize(GameView gameView)
    {
        this.gameView = gameView;
        healtBar.gameObject.SetActive(false);

        damageTextInstances = new LinkedList<Canvas>();
    }    

    public void EnterGamePlayState(int ownerConnectionIndex)
    {
        this.ownerConnectionIndex = ownerConnectionIndex;                
    }

    public void ExitGamePlayState()
    {
        healtBar.gameObject.SetActive(false);
        lastTimeDisplayTime = 0;
    }

    public void SetHealthValue(int ownerConnectionIndex, float oldHealth, float newHealth)
    {   
        if (newHealth == LocalGame.Instance.GameData.PlayerStartHealth) return;             
        
        if (this.ownerConnectionIndex == ownerConnectionIndex)
        {
            healtBar.gameObject.SetActive(true);
            amountFill.fillAmount = newHealth / LocalGame.Instance.GameData.PlayerStartHealth;
            lastTimeDisplayTime = Time.time;
                
            // spawn damage view
            var damageCounterInstance = Instantiate(LocalGame.Instance.GameData.DamageView, transform.position + transform.up, LocalGame.Instance.GameData.CameraRotation[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value]);                        
            var damageText = damageCounterInstance.GetComponentInChildren<TextMeshProUGUI>();
            float t = newHealth - oldHealth;
            damageText.text = (t > 0) ? $"+{Mathf.Abs(t)}" : (t < 0) ? $"{t}" : "No Damage";  
            damageText.GetComponent<RectTransform>().anchoredPosition += new Vector2(UnityEngine.Random.Range(-100, 100), 0);
            damageTextInstances.AddLast(damageCounterInstance);
        }
    }

    void ClearDamageCounters()
    {
        var dcf = damageTextInstances.First;
        while (dcf != null)
        {
            Destroy(dcf.Value.gameObject);                            
            var next = dcf.Next;            
            dcf = next;
        }
        damageTextInstances.Clear();
    }

    public void OnGameStateLose(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        ClearDamageCounters();
    }

    public void OnGameStateWin(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        ClearDamageCounters();
    }

    public void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        ClearDamageCounters();
    }

    public void OnGameStateStart(LocalGame myLocalGame)
    {
        ClearDamageCounters();
    }
}
