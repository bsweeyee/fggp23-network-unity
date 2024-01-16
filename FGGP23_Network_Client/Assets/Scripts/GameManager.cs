using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject PlayerPrefab;

    public Dictionary<byte, PacketHandle> Handlers = new();
    public delegate void PacketHandle(Packet packet);
    public static GameManager Instance;

    public Dictionary<int, Player> PlayerList = new();

    public List<Action> actions = new();
    public List<Action> actionsCopy = new();

    bool hasAction = false;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else if (Instance != null) {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }

        Handlers.Add((byte)PacketID.S_welcome, WelcomeReceived);
        Handlers.Add((byte)PacketID.S_spawnPlayer, SpawnPlayer);
        Handlers.Add((byte)PacketID.S_playerDisconnected, PlayerDisconnected);
    }

    public void Update() {
        if (hasAction) {
            actionsCopy.Clear();
            lock(actions) {
                actionsCopy.AddRange(actions);
                actions.Clear();
                hasAction = false;
            }
            foreach(Action action in actionsCopy) {
                action.Invoke();                
            }
            if (actions.Count > 0) {
                Debug.Log("here");
                hasAction = true;
            }
        }
    }

    public void WelcomeReceived(Packet packet) {
        string msg = packet.GetString();
        Debug.Log(msg);
        int yourID = packet.GetInt();

        Packet welcomeReceived = new Packet();
        welcomeReceived.Add((byte)PacketID.C_welcomeReceived);
        welcomeReceived.Add(yourID);
        Client.Instance.tcp.SendData(welcomeReceived);
        Client.Instance.Id = yourID;            
    }

    public void SpawnPlayer(Packet packet) {
        int id = packet.GetInt();
        string name = packet.GetString();
        Vector3 pos = packet.GetVector3();
        Quaternion rot = packet.GetQuaternion();

        lock(actions) {
            hasAction = true;
            actions.Add(() => {
                Player newPlayer = Instantiate(PlayerPrefab, pos, rot).GetComponent<Player>();
                newPlayer.playerName = name;
                Debug.Log("added: " + id);
                if (PlayerList.ContainsKey(id)) {
                    PlayerList[id] = newPlayer;
                } else {
                    PlayerList.Add(id, newPlayer);                
                }
            });
        }
    }

    public void PlayerDisconnected(Packet packet) {
        int id = packet.GetInt();
        if (PlayerList.TryGetValue(id, out Player player)) {
            lock(actions) {
                hasAction = true;
                actions.Add(() => {
                    actions.Add(() => {                        
                        Debug.Log("remove: " + id);                                                
                        PlayerList.Remove(id);
                        Destroy(player.gameObject);
                    });
                });
            }
        }
    } 

    // #if UNITY_EDITOR
    void OnGUI() {
        if (GUILayout.Button("Connect")) {
            Client.Instance.Connect();
        }
        using (new GUILayout.HorizontalScope()) {
            Client.Instance.playerName = GUILayout.TextField(Client.Instance.playerName);
            
            if (GUILayout.Button("Join game")) {
                Client.Instance.JoinGame();
            }
        }        
        if (GUILayout.Button("Disconnect")) {
            Client.Instance.Disconnect();
        }
    }
    // #endif
}
