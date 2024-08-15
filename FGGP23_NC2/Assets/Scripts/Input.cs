using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FGNetworkProgramming
{
    public enum EInputState
    {
        NONE,
        PRESSED,
        HOLD,
        RELEASE,
    }

    public enum EGameInput
    {
        NONE,
        LEFT_MOUSE_BUTTON,
        RIGHT_MOUSE_BUTTON,
        SPACE_KEY,
        W,
        A,
        S,
        D
    }        

    public class Input : MonoBehaviour
    {
        struct TInputState
        {
            public EGameInput gameInput;
            public EInputState currentState;            

            public TInputState(EGameInput gameInput, EInputState inputState)
            {
                this.gameInput = gameInput;
                this.currentState = inputState;
            }
        }

        private static Input instance;    
        private Mouse mouse;
        private Keyboard keyboard;
        private Dictionary<EGameInput, TInputState> gameInputs;
        private EventSystem eventSystem;

        public static Input Instance 
        {
            get {
                if (instance == null) {
                    instance = FindObjectOfType<Input>();
                    instance?.Initialize();
                }
                if (instance == null) {
                    var go = new GameObject("Input");
                    instance = go.AddComponent<Input>();
                    instance.Initialize();
                }
                return instance;
            }
        }

        [HideInInspector] public UnityEvent<Vector2, EGameInput, EInputState> OnHandleMouseInput;
        [HideInInspector] public UnityEvent<EGameInput, EInputState> OnHandleKeyboardInput;        
        
        public void Initialize()
        {
            mouse = Mouse.current;
            keyboard = Keyboard.current;

            gameInputs = new Dictionary<EGameInput, TInputState>();
            OnHandleMouseInput = new UnityEvent<Vector2, EGameInput, EInputState>();
            OnHandleKeyboardInput = new UnityEvent<EGameInput, EInputState>();
            
            gameInputs.Add(EGameInput.LEFT_MOUSE_BUTTON, new TInputState(EGameInput.LEFT_MOUSE_BUTTON, EInputState.NONE));        
            gameInputs.Add(EGameInput.RIGHT_MOUSE_BUTTON, new TInputState(EGameInput.RIGHT_MOUSE_BUTTON, EInputState.NONE));        
            gameInputs.Add(EGameInput.SPACE_KEY, new TInputState(EGameInput.SPACE_KEY, EInputState.NONE));
            gameInputs.Add(EGameInput.W, new TInputState(EGameInput.W, EInputState.NONE));
            gameInputs.Add(EGameInput.A, new TInputState(EGameInput.A, EInputState.NONE));
            gameInputs.Add(EGameInput.S, new TInputState(EGameInput.S, EInputState.NONE));
            gameInputs.Add(EGameInput.D, new TInputState(EGameInput.D, EInputState.NONE));

            eventSystem = FindObjectOfType<EventSystem>();
        }

        void HandleInputs(EGameInput input, bool isPressed)
        {
            if (eventSystem == null) eventSystem = FindObjectOfType<EventSystem>();
            if (isPressed)
            {
                if (gameInputs[input].currentState == EInputState.NONE)
                {
                    if (input == EGameInput.LEFT_MOUSE_BUTTON || input == EGameInput.RIGHT_MOUSE_BUTTON)
                    {
                        if (eventSystem.IsPointerOverGameObject()) return; 
                        OnHandleMouseInput?.Invoke(mouse.position.value, input, EInputState.PRESSED);
                        // Debug.Log("[" + input + "]: (" + mouse.position.value + "), " + EInputState.PRESSED);
                    }
                    else
                    {
                        OnHandleKeyboardInput?.Invoke(input, EInputState.PRESSED);
                        // Debug.Log("[" + input + "]:" + EInputState.PRESSED);
                    }
                    gameInputs[input] = new TInputState(input, EInputState.PRESSED);
                } 
                else 
                {
                    if (input == EGameInput.LEFT_MOUSE_BUTTON || input == EGameInput.RIGHT_MOUSE_BUTTON)
                    {
                        if (eventSystem.IsPointerOverGameObject()) return;
                        OnHandleMouseInput?.Invoke(mouse.position.value, input, EInputState.HOLD);
                    }
                    else
                    {
                        OnHandleKeyboardInput?.Invoke(input, EInputState.HOLD);
                        // Debug.Log("[" + input + "]:" + EInputState.HOLD);
                    }
                    gameInputs[input] = new TInputState(input, EInputState.HOLD);                                        
                }
            }
            else
            {
                if (gameInputs[input].currentState == EInputState.HOLD)
                {
                    if (input == EGameInput.LEFT_MOUSE_BUTTON || input == EGameInput.RIGHT_MOUSE_BUTTON)
                    {
                        if (eventSystem.IsPointerOverGameObject()) return;
                        OnHandleMouseInput?.Invoke(mouse.position.value, input, EInputState.RELEASE);
                        // Debug.Log("[" + input + "]: (" + mouse.position.value + "), " + EInputState.RELEASE);
                    }
                    else
                    {
                        OnHandleKeyboardInput?.Invoke(input, EInputState.RELEASE);
                        // Debug.Log("[" + input + "]:" + EInputState.RELEASE);
                    }
                    gameInputs[input] = new TInputState(input, EInputState.NONE);
                }
            }
        }

        void Update()
        {                                           
           HandleInputs(EGameInput.LEFT_MOUSE_BUTTON, mouse.leftButton.isPressed);
           HandleInputs(EGameInput.SPACE_KEY, keyboard.spaceKey.isPressed);
           HandleInputs(EGameInput.W, keyboard.wKey.isPressed);
           HandleInputs(EGameInput.A, keyboard.aKey.isPressed);
           HandleInputs(EGameInput.S, keyboard.sKey.isPressed);
           HandleInputs(EGameInput.D, keyboard.dKey.isPressed);
        }
    }
}
