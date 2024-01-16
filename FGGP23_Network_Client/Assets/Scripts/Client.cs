using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Sockets;
using System.Security.Cryptography;
using UnityEngine;

public class ClientTCP {
    private TcpClient socket;
    private NetworkStream stream;
    private byte[] receiveBuffer;

    public const int dataBufferSize = 4096;

    public void Connect(string ip, int port) {
        socket = new TcpClient{
            ReceiveBufferSize = dataBufferSize,
            SendBufferSize = dataBufferSize
        };

        receiveBuffer = new byte[dataBufferSize];
        socket.BeginConnect(ip, port, ConnectionCallback, socket);    
    }

    public void Disconnect() {
        stream?.Close();
        socket?.Close();
    }

    public void SendData(Packet packet) {
        byte[] packaetBytes = packet.ToArray();
        try {
            if (socket != null) {
                stream.BeginWrite(packaetBytes, 0, packaetBytes.Length, null, null);
            }
        } catch(Exception ex) {
            Debug.Log($"Error sending data to server via TCP: {ex}");
        }
    }

    public void ConnectionCallback(IAsyncResult result) {
        socket.EndConnect(result);
        if(!socket.Connected) {
            return;
        }

        stream = socket.GetStream();

        stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult result) {
        try {
            Debug.Log("receive callback");                        
            int byteLen = stream.EndRead(result);
            if (byteLen <= 0) {
                return;
            }

            byte[] data = new byte[byteLen];
            Array.Copy(receiveBuffer, data, byteLen);
            HandleData(data);

            // keep reading from stream until end stream?
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }
        catch (System.ObjectDisposedException ode) {
            Debug.LogWarning(ode);
        }
        catch(Exception ex) {
            Debug.LogError(ex);
        }
    }

    private void HandleData(byte[] data) {
        Packet packet = new (data);
        byte packetID = packet.GetByte();
        GameManager.Instance.Handlers[packetID](packet);
    }    
}

public class Client : MonoBehaviour
{
    public static Client Instance;
    public string playerName = "Player1";
    public string ip = "127.0.0.1";
    public int port = 25565;
    public int Id = 0;

    public ClientTCP tcp = new();

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(this);
        }        
    }

    void Start() {
    }

    void OnDestroy() {
        tcp.Disconnect();
    }

    public void Connect() {
        tcp.Connect(ip, port);        
    }

    public void Disconnect() {
        tcp?.Disconnect();

        foreach(var g in GameManager.Instance.PlayerList.Values) {
            Destroy(g.gameObject);
        }
        GameManager.Instance.PlayerList.Clear();
        // if (GameManager.Instance.PlayerList.TryGetValue(Id, out Player player)) {
        //     GameManager.Instance.PlayerList.Remove(Id);
        //     Destroy(player.gameObject);
        // }
    }

    public void JoinGame() {
        Packet packet = new Packet();
        packet.Add((byte)PacketID.C_spawnPlayer);        
        packet.Add(playerName);
        tcp.SendData(packet);
    }
}
