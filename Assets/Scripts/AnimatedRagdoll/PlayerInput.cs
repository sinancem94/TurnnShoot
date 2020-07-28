using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtmostInput;

public class PlayerInput : MonoBehaviour
{    
    public Vector3 InputForce;
    public bool pressingShift = false;
    public bool jump = false;
    public float jumpForce = 50f;


    Quaternion mouseRotation = new Quaternion();
    Quaternion movementKeysRotation = new Quaternion();

   
    void Start()
    {
        //currSpeed = defaultSpeed;

        mouseRotation = transform.rotation;
        movementKeysRotation = transform.rotation;

        //set input events
        InputEventManager.inputEvent.onMouseMoved += OnMouseMoved;
        InputEventManager.inputEvent.onKeyboardMove += OnMove;
        InputEventManager.inputEvent.onPressedShift += OnPressedSprint;
        InputEventManager.inputEvent.onReleasedShift += OnReleasedSprint;
        InputEventManager.inputEvent.onPressedSpace += OnPressedJump;
    }

    Vector3 SetInputForce(Vector2 moveVector)
    {
        //set input force according to forward of animated character

        //total magnitude force vector can get 1.0f 
        //if vertical and horizontal is full addForce will double the force from intended so we are deviding force between vertical and horizontal 
        float forwardForce = Mathf.Abs(moveVector.y);
        
        //set force if neither is zero
        if (Mathf.Approximately(moveVector.y, 0f))
        {
            forwardForce = Mathf.Abs(moveVector.x);
        }

        Vector3 force = new Vector3(0f, 0f, forwardForce);
       
        return force;
    }

    private void OnMouseMoved(Vector2 mouseAxis, float rotSpeed)
    {
        //Quaternion.AngleAxis(mouseAxis.x * rotSpeed * Time.deltaTime, Vector3.up);
        mouseRotation *= (Quaternion.AngleAxis(mouseAxis.x * rotSpeed * Time.deltaTime, Vector3.up));

        //transform.rotation = mouseRotation;// (Quaternion.AngleAxis(mouseAxis.x * rotSpeed * Time.deltaTime, Vector3.up));
        //transform.Rotate(0f, mouseAxis.x * rotSpeed * Time.deltaTime, 0f);
    }

    private void OnMove(Vector2 moveVec)
    {
        //Before moving set characters rotation first
        //going right or left only changes characters rotation. 

        var reverseAngle = (moveVec.y < 0f) ? 180f * moveVec.y : 0f;
        var direction = (moveVec.y < 0f) ? -1: 1;

        movementKeysRotation = Quaternion.AngleAxis(reverseAngle + (moveVec.x * ((moveVec.y != 0f) ? 45f : 90f) * direction), Vector3.up);
        //Debug.Log(moveVec.x);
        transform.rotation = mouseRotation * movementKeysRotation;
        //Input force will set force according to characters looking position
        InputForce = SetInputForce(moveVec);// * currSpeed;
    }

    private void OnPressedJump()
    {
        jump = true;
    }

    private void OnPressedSprint()
    {
        pressingShift = true;
        //currSpeed = runSpeed;
    }

    private void OnReleasedSprint()
    {
        pressingShift = false;
        //currSpeed = defaultSpeed;
    }
}
