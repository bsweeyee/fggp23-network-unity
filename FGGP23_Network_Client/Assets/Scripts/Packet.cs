using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using UnityEngine;

public enum PacketID
{
    S_welcome = 1,
    S_spawnPlayer = 2,
    S_playerPosition = 3,
    S_playerRotation = 4,
    S_playerShoot = 5,
    S_playerDisconnected = 6,
    S_playerHealth = 7,
    S_playerDead = 8,
    S_playerRespawned = 9,


    C_spawnPlayer = 126,
    C_welcomeReceived = 127,
    C_playerMovement = 128,
    C_playerShoot = 129,
    C_playerHit = 130,
}

public class Packet
{
    List<byte> writeableData = new List<byte>();
    byte[] data;
    int readPosition = 0;    

    private int packetLength = 0;
    public Packet() { }

    public Packet(byte[] data) {
        this.data = data;
        packetLength = GetInt(); // this initializes the packet size at the start
    }

    public byte[] ToArray() {
        writeableData.InsertRange(0, BitConverter.GetBytes(writeableData.Count));
        return writeableData.ToArray();
    }

    public byte GetByte() {
        byte value = data[readPosition];
        readPosition += 4;
        return value;
    }

    public byte[] GetBytes(int len) {        
        byte[] value = new byte[len];
        Array.Copy(value, readPosition, value, 0, len);
        readPosition += len;
        return value;
    }

    public int GetInt() {
        int value = BitConverter.ToInt32(data, readPosition);        
        readPosition += 4;
        return value;
    }
    
    public string GetString() {
        int length = GetInt();
        string value = Encoding.ASCII.GetString(data, readPosition, length);
        readPosition += length;
        return value;
    }

    public void Add(byte data) {
        writeableData.Add(data);
    }
    public void Add(byte[] data) {
        writeableData.AddRange(data);
    }
    public void Add(int data) {
        writeableData.AddRange(BitConverter.GetBytes(data));
    }        
}
