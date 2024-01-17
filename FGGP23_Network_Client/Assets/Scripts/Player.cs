using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public Vector3 targetPosition;
    public CharacterController characterController;
    
    public virtual void Awake() {
        characterController = GetComponent<CharacterController>();
    }

    protected virtual void Update() {
        characterController.Move(targetPosition * Time.deltaTime);
    }
}
