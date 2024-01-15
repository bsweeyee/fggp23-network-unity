using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using UnityEditor.UI;
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

    public void Close() {
        socket?.Close();
        stream?.Close();
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
    public string ip = "127.0.0.1";
    public int port = 25565;

    public ClientTCP tcp = new();

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(this);
        }        
    }

    void Start() {
        tcp.Connect(ip, port);
    }

    void OnDestroy() {
        tcp.Close();
    }
}
