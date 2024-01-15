using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Dictionary<byte, PacketHandle> Handlers = new();
    public delegate void PacketHandle(Packet packet);
    public static GameManager Instance;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else if (Instance != null) {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }

        Handlers.Add((byte)PacketID.S_welcome, WelcomeReceived);
    }

    public void WelcomeReceived(Packet packet) {
        string msg = packet.GetString();
        Debug.Log(msg);
        int yourID = packet.GetInt();

        Packet welcomeReceived = new Packet();
        welcomeReceived.Add((byte)PacketID.C_welcomeReceived);
        welcomeReceived.Add(yourID);
        Client.Instance.tcp.SendData(welcomeReceived);            
    }
}
