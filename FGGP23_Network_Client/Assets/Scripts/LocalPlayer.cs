using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class LocalPlayer : Player
{
    public Transform cameraTransform;
    bool lockCursor = true;
    public override void Awake()
    {
        base.Awake();
        cameraTransform = GetComponentInChildren<Camera>().transform;
    
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    protected override void Update() {
        
        if (Input.GetKey(KeyCode.LeftAlt)) {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            lockCursor = false;
        }
        if (Input.GetKeyUp(KeyCode.LeftAlt)) {
            lockCursor = true;        
        }

        if (lockCursor) {
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = Input.GetAxisRaw("Mouse Y");
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            transform.Rotate(200 * mouseX * Time.deltaTime * Vector3.up);
            Vector3 rotation = cameraTransform.rotation.eulerAngles + new Vector3(-mouseY,0,0);
            rotation.x = ClampAngle(rotation.x, -90, 90);
            cameraTransform.eulerAngles = rotation;

            bool[] inputs = new bool[] {
                Input.GetKey(KeyCode.W),
                Input.GetKey(KeyCode.S),
                Input.GetKey(KeyCode.A),
                Input.GetKey(KeyCode.D),
            };

            Packet movePacket = new Packet();
            movePacket.Add((byte)PacketID.C_playerMovement);
            movePacket.Add(inputs.Length);
            foreach(var input in inputs) {
                movePacket.Add(input);
            }
            movePacket.Add(transform.rotation);
            Client.Instance.udp.SendData(movePacket);         
        }

        base.Update();        
    }

    private float ClampAngle(float angle, float min, float max) {
        angle = (angle < 0) ? angle = 360 + angle : angle;
        angle = (angle > 180) ? Mathf.Max(angle, 360 + min) : angle;

        return Mathf.Min(angle, max);        
    }
}
