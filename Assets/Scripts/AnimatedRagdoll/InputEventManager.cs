using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UtmostInput;

public class InputEventManager : MonoBehaviour
{
    public static InputEventManager inputEvent;

    InputX inputX;

    [SerializeField]
    float mouseRotationSpeed = 0f;

    private void Awake()
    {
        if (inputEvent == null)
            inputEvent = this;
        else
            Destroy(this);
    }

    void Start()
    {
        inputX = new InputX();
    }

    //Control input events
    private void Update()
    {
        MouseMoved(mouseRotationSpeed);

        //if (Mathf.Abs(inputX.Vertical()) >= .01f || Mathf.Abs(inputX.Horizontal()) >= .01f)
            KeyboardMovePressed();

        if (inputX.isSpacePressed())
            PressedSpace();

        if (inputX.isSpaceReleased())
            ReleasedSpace();

        if (inputX.isShiftPressed())
            PressedShift();

        if (inputX.isShiftReleased())
            ReleasedShift();
    }

    public event Action<Vector2,float> onMouseMoved;
    public void MouseMoved(float mouseRot)
    {
        if(onMouseMoved != null)
        {
            onMouseMoved(inputX.MouseAxis(),mouseRot);
        }
    }

    public event Action<Vector2> onKeyboardMove;
    public void KeyboardMovePressed()
    {
        if(onKeyboardMove != null)
        {
            Vector2 moveVec = new Vector2(inputX.Horizontal(), inputX.Vertical());

            onKeyboardMove(moveVec);
        }
    }

    public event Action onPressingSpace;
    public void PressingSpace()
    {
        if(onPressingSpace != null)
        {
            onPressingSpace();
        }
    }

    public event Action onPressedShift;
    public void PressedShift()
    {
        if (onPressedShift != null)
        {
            onPressedShift();
        }
    }

    public event Action onReleasedShift;
    public void ReleasedShift()
    {
        if (onReleasedShift != null)
        {
            onReleasedShift();
        }
    }

    public event Action onPressedSpace;
    public void PressedSpace()
    {
        if (onPressedSpace != null)
        {
            onPressedSpace();
        }
    }

    public event Action onReleasedSpace;
    public void ReleasedSpace()
    {
        if (onReleasedSpace != null)
        {
            onReleasedSpace();
        }
    }

}
