using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class MorseCodeGenerator : MonoBehaviour
{

    public TOTP totp;

    //public InputHelpers.Button button = InputHelpers.Button.None;
    [SerializeField] private ActionBasedController controller = null;
    public InputActionReference buttonAction = null;

    const float dashTime = 0.5f;
    const float dotTime = 0.1f;
    const float digitSpacing = 0.5f;
    const float repeatDelay = 10.0f;
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

    private bool lockCodeRequest = false;
    private bool lockCodeRepeat = false;
    private bool isOTPinitialized = false;

    // ..-  -.  -.-.  --.
    private float[][] code = { new float[] { dotTime, dotTime, dashTime }, new float[] { dashTime, dotTime }, new float[] { dashTime, dotTime, dashTime, dotTime }, new float[] { dashTime, dashTime, dotTime } };

    public UnityEvent OnOTPInitiated;
    [System.Serializable] public class MyIntEvent : UnityEvent<int> { }
    public MyIntEvent OnOTPSegment;

    public UnityEvent OnOTPDelaySegment;
    public float ActionDelay { get => actionDelay; set => actionDelay = value; }

    public float RepeatDelay => repeatDelay;

    private void OnEnable()
    {
        lockCodeRequest = false;
        lockCodeRepeat = false;
        isOTPinitialized = false;
        ResetActionDelay();
    }

    private void Awake()
    {
        //controller = GetComponent<ActionBasedController>();
        buttonAction.action.started += keyDown;
        buttonAction.action.canceled += keyUp;
    }

    private void OnDestroy()
    {
        controller = null;
        buttonAction.action.started -= keyDown;
        buttonAction.action.canceled -= keyUp;
    }

    private void Update()
    {

        //Update key pressed time
        if (keyIsPressed)
        {
            timePressed += Time.deltaTime;
        }

        if (!lockCodeRepeat)
        {
            //Update action timer
            if (ActionDelay >= 0)
            {
                ActionDelay -= Time.deltaTime;
                if (ActionDelay < 0)
                {
                    action();
                }
            }
        }

        //Notify the detection of a long press using a long haptic pulse
        if (timePressed > longPressDetectionTime && longPressHapticRemaining)
        {
            longPressHapticRemaining = false;
            controller?.SendHapticImpulse(1.0f, longPressFeedbackTime);
        }

        //Debug haptics
        //counter++;
        //if(counter % 30 == 0 && keyIsPressed) {
        //    Debug.Log(controller.SendHapticImpulse(1.0f, 0.05f));
        //}
        InputAction.CallbackContext asd = new InputAction.CallbackContext();
        if (Input.GetKeyDown(KeyCode.A))
        {
            keyDown(asd);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            keyUp(asd);
        }
    }

    private void keyDown(InputAction.CallbackContext context)
    {
        if (actionDelay > 0) return;
        keyIsPressed = true;
        timePressed = 0;
        longPressHapticRemaining = true;
        ActionDelay = -1;

    }

    private void keyUp(InputAction.CallbackContext context)
    {
        //if (actionDelay > 0) return;

        keyIsPressed = false;

        if (lockCodeRequest) return;

        //Start code generation on long press
        if (timePressed > longPressDetectionTime && !isOTPinitialized)
        {
            segmentCounter = -1;  //Set to -1 so the first tap increments to 0
            digitCounter = 0;  //redundant
            ActionDelay = -1;  //Wait for tap to give first code
            repeatCounter = maxRepeats;  //redundant
            if (totp)
            {
                initFromString(totp.generateEmulatedCode());
            }

            isOTPinitialized = true;
            OnOTPInitiated?.Invoke();

        }
        else if (isOTPinitialized)
        {
            //Advance code segment on tap
            segmentCounter++;
            digitCounter = 0;
            ActionDelay = tapCodeDelay;
            repeatCounter = maxRepeats;

            lockCodeRequest = true;
            lockCodeRepeat = false;
            OnOTPSegment?.Invoke(segmentCounter);

        }

    }


    private void action()
    {
        //Debug.Log("test");
        if (segmentCounter >= 0 && segmentCounter < code.Length)
        {
            //A valid code segment can be returned

            if (digitCounter < code[segmentCounter].Length && repeatCounter > 0)
            {

                //A valid digit can be returned.  Trigger haptics, increment digit counter, add actionDelay
                controller?.SendHapticImpulse(1.0f, code[segmentCounter][digitCounter]);
                ActionDelay = code[segmentCounter][digitCounter] + digitSpacing;
                digitCounter++;
            }

            else if (digitCounter == 0 && repeatCounter == 0)
            {
                //The repeat delay after the last repreat has finished.
                //Attempt has timed out.  Show via long pulse, reset code counters.
                controller?.SendHapticImpulse(1.0f, timeOutPulseTime);
                segmentCounter = 10;  //Set segment counter past valid range so a fresh long press is required
            }

            else if (digitCounter == code[segmentCounter].Length)
            {
                //Out of digits.  Setup a repeat
                if (repeatCounter > 0)
                {
                    repeatCounter--;
                    ActionDelay = repeatDelay;
                    digitCounter = 0;
                    OnOTPDelaySegment?.Invoke();
                }
            }

        }
    }

    void initFromString(string codeString)
    {
        string[] segments = codeString.Split(" ");
        Debug.Assert(segments.Length == 4);
        for (int i = 0; i < 4; i++)
        {
            float[] segmentFloats = new float[segments[i].Length];
            for (int j = 0; j < segments[i].Length; j++)
            {
                if (segments[i].Substring(j, 1).Equals("."))
                {
                    segmentFloats[j] = dotTime;
                }
                else
                {
                    segmentFloats[j] = dashTime;
                }
            }
            code[i] = segmentFloats;
        }
    }

    public void UnlockCodeRequest()
    {
        lockCodeRequest = false;
    }

    public void LockCodeRepeat()
    {
        lockCodeRepeat = true;
    }

    public void ResetActionDelay()
    {
        ActionDelay = -1f;
    }
}