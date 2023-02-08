using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class MorseCodeGenerator : MonoBehaviour {

    //public InputHelpers.Button button = InputHelpers.Button.None;
    private ActionBasedController controller = null;
    public InputActionReference buttonAction = null;

    const float dashTime = 0.5f;
    const float dotTime = 0.1f;
    const float digitSpacing = 0.5f;
    const float repeatDelay = 5.0f;
    const float tapCodeDelay = 0.5f;
    const float timeOutPulseTime = 1.0f;
    const float longPressDetectionTime = 0.8f;
    const float longPressFeedbackTime = 1.0f;
    const int maxRepeats = 3;

    private bool keyIsPressed = false;
    private float timePressed = 0;
    private bool longPressHapticRemaining = false;

    private int counter = 0;

    private float actionDelay = -1.0f;
    private int segmentCounter = 20;
    private int digitCounter = -1;
    private int repeatCounter = -1;


    // ..-  -.  -.-.  --.
    private float[][] code = {new float[] {dotTime,dotTime,dashTime},new float[] {dashTime,dotTime},new float[] {dashTime,dotTime,dashTime,dotTime},new float[] {dashTime,dashTime,dotTime}};

    private void Awake() {
        controller = GetComponent<ActionBasedController>();
        buttonAction.action.started += keyDown;
        buttonAction.action.canceled += keyUp;
    }

    private void OnDestroy() {
        controller = null;
        buttonAction.action.started -= keyDown;
        buttonAction.action.canceled -= keyUp;
    }

    private void Update() {

        //Update key pressed time
        if(keyIsPressed) {
            timePressed += Time.deltaTime;
        }

        //Update action timer
        if(actionDelay >= 0) {
            actionDelay -= Time.deltaTime;
            if(actionDelay < 0) {
                action();
            }
        }

        //Notify the detection of a long press using a long haptic pulse
        if(timePressed > longPressDetectionTime && longPressHapticRemaining) {
            longPressHapticRemaining = false;
            controller?.SendHapticImpulse(1.0f, longPressFeedbackTime);
        }
        
        //Debug haptics
        //counter++;
        //if(counter % 30 == 0 && keyIsPressed) {
        //    Debug.Log(controller.SendHapticImpulse(1.0f, 0.05f));
        //}
    }

    private void keyDown(InputAction.CallbackContext context) {
        keyIsPressed = true;
        timePressed = 0;
        longPressHapticRemaining = true;
        actionDelay = -1;
    }

    private void keyUp(InputAction.CallbackContext context) {
        keyIsPressed = false;

        //Start code generation on long press
        if(timePressed > longPressDetectionTime) {
            segmentCounter = -1;  //Set to -1 so the first tap increments to 0
            digitCounter = 0;  //redundant
            actionDelay = -1;  //Wait for tap to give first code
            repeatCounter = maxRepeats;  //redundant
        } else {
            //Advance code segment on tap
            segmentCounter++;
            digitCounter = 0;
            actionDelay = tapCodeDelay;
            repeatCounter = maxRepeats;
        }

    }


    private void action() {
        //Debug.Log("test");
        if(segmentCounter >= 0 && segmentCounter < code.Length) {
            //A valid code segment can be returned

            if(digitCounter < code[segmentCounter].Length && repeatCounter>0) {
                //A valid digit can be returned.  Trigger haptics, increment digit counter, add actionDelay
                controller?.SendHapticImpulse(1.0f, code[segmentCounter][digitCounter]);
                actionDelay = code[segmentCounter][digitCounter] + digitSpacing;
                digitCounter++;
            } 
            
            else if(digitCounter==0 && repeatCounter==0) {
                //The repeat delay after the last repreat has finished.
                //Attempt has timed out.  Show via long pulse, reset code counters.
                controller?.SendHapticImpulse(1.0f, timeOutPulseTime);
                segmentCounter = 10;  //Set segment counter past valid range so a fresh long press is required
            }
            
            else if(digitCounter == code[segmentCounter].Length) {
                //Out of digits.  Setup a repeat
                if(repeatCounter > 0) {
                    repeatCounter--;
                    actionDelay = repeatDelay;
                    digitCounter = 0;
                }
            }

        }
    }
}