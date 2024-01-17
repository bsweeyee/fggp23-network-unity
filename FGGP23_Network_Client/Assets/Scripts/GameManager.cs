using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public GameObject LocalPlayerPrefab; 

    public Dictionary<byte, PacketHandle> Handlers = new();
    public delegate void PacketHandle(Packet packet);
    public static GameManager Instance;

    public Dictionary<int, Player> PlayerList = new();

    public List<Action> actions = new();
    public List<Action> actionsCopy = new();

    bool hasAction = false;

    Camera temporaryCamera;

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
        Handlers.Add((byte)PacketID.S_playerPosition, PlayerPosition);
        Handlers.Add((byte)PacketID.S_playerRotation, PlayerRotation);

        temporaryCamera = FindObjectOfType<Camera>();
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

        Client.Instance.udp.Connect();            
    }

    public void SpawnPlayer(Packet packet) {
        int id = packet.GetInt();
        string name = packet.GetString();
        Vector3 pos = packet.GetVector3();
        Quaternion rot = packet.GetQuaternion();

        GameObject prefabToSpawn = (id == Client.Instance.Id) ? LocalPlayerPrefab : PlayerPrefab;

        lock(actions) {
            hasAction = true;
            actions.Add(() => {
                Debug.Log(name + ": " + pos);
                Player newPlayer = Instantiate(prefabToSpawn, pos, rot).GetComponent<Player>();
                newPlayer.playerName = name;
                // Debug.Log("added: " + id);
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
                        // Debug.Log("remove: " + id);                                                
                        PlayerList.Remove(id);
                        Destroy(player.gameObject);
                    });
                });
            }
        }
    }

    public void PlayerPosition(Packet packet) {
        int id = packet.GetInt();
        Vector3 position = packet.GetVector3();
        if (PlayerList.TryGetValue(id, out Player player)) {
            lock(actions) {
                hasAction = true;
                actions.Add(() => {                                        
                    player.targetPosition = position;
                    // Debug.Log(player.playerName + ": " + position);
                });
            }
        }
    }

    public void PlayerRotation(Packet packet) {
        int id = packet.GetInt();
        if (id == Client.Instance.Id) {
            return;
        }

        Quaternion rotation = packet.GetQuaternion();

        if (PlayerList.TryGetValue(id, out Player player)) {
            lock(actions) {
                hasAction = true;
                actions.Add(() => {                    
                    player.transform.rotation = rotation;
                });
            }
        }
    } 

    // #if UNITY_EDITOR
    void OnGUI() {
        if (GUILayout.Button("Connect")) {
            temporaryCamera?.gameObject.SetActive(false);
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
            temporaryCamera.gameObject.SetActive(true);
        }
    }
    // #endif
}
