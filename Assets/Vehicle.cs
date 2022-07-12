using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Photon.Pun;
//using XInputDotNetPure; // for controller vibration

public class Vehicle : MonoBehaviour
{
    //PlayerIndex playerIndex; //for controller vibration
    //GamePadState state;
    //GamePadState prevState;
    float vibrationMultiplyer = 0.2f;

    PhotonView view;

    [System.NonSerialized]
    public bool optionsMenu;
    [System.NonSerialized]
    public bool mine;

    bool OnGround;
    
    public GameObject skids;
    public GameObject wheel;
    [System.NonSerialized]
    public bool isNeutral = false;

    [System.NonSerialized]
    public float wheelSize = 1f;
    float turnMultiplyer = 35f;
    public float zLength = 2.26f;
    public float xWidth = 1.2f;
    float suspensionMultiplyer = 6200f;
    [System.NonSerialized]
    public float maxSuspensionDistance = 1f;
    float dampAmount = 200000f;
    [System.NonSerialized]
    public float minSlideSpeed = 2.5f;
    float stableGrip = 1.0f;
    float slipGrip = 0.6f;
    float engineBraking = 0f;
    float brakePressure = 800f;
    float enginePower = 165f;
    float downforce = 34f;

    [System.NonSerialized]
    public bool gearSwitching = false;
    bool canSwitchGears = true;
    float gearSwapSpeed = 0.2f;
    bool autoGearShifting = true;

    public float speed;
    bool brakeControl = true;
    public float wheelBrake;
    bool turnControl = true;
    bool tractionControl = true; //mfe
    public float wheelThrottle; //make for erach

    public bool reverse;

    float wheelRPM; //each wheel for each of these variables

    public int gearSelection = 0;
    float[] gears = {30f, 19.2f, 12.3f, 7.8f, 5f, 3.2f};
    public float currentGearRatio = 40f; //40 rotations of engine = 1 wheel rotation, change fotr geer shifting
     //if braked for 1 second then drive backwards, if braked for 0.1 and let go then stay braked, if braked for less than 0.1 and not holding anything than netral
    public float currentEngineRPM;
    float redLineRPM = 8000f;
    public float maxEngineRPM = 9000f;

    //make each wheel have individual speed and grip, for pressure for adding differencial in traction control, and slipping each wheel individually
    //downforce at position, tune front or back, tune com

    public float throttle;
    public float brakeStrength;
    float leftTurn;
    float righTurn;

    float turnAngle;
    
    VehicleControls controls;
    Rigidbody rb;

    public bool FLSlip;
    public bool FRSlip;
    public bool BLSlip;
    public bool BRSlip;

    float FLgrip;
    float FRgrip;
    float BLgrip;
    float BRgrip;

    float PushFL;
    float PushFR;
    float PushBL;
    float PushBR;

    float AverageSlipDirectionMagnitude;

    Vector3 FLSlipDirection;
    Vector3 FRSlipDirection;
    Vector3 BLSlipDirection;
    Vector3 BRSlipDirection;

    float FLSlipDirectionZ;
    float FRSlipDirectionZ;
    float BLSlipDirectionZ;
    float BRSlipDirectionZ;

    float FLSlipDirectionX;
    float FRSlipDirectionX;
    float BLSlipDirectionX;
    float BRSlipDirectionX;

    GameObject FLWheelSpawn;
    GameObject FRWheelSpawn;
    GameObject BLWheelSpawn;
    GameObject BRWheelSpawn;

    float FLPreviousPosition;
    float FRPreviousPosition;
    float BLPreviousPosition;
    float BRPreviousPosition;

    float FLVelocity;
    float FRVelocity;
    float BLVelocity;
    float BRVelocity;

    void Awake ()
    {
        view = GetComponent<PhotonView>();

        if (SceneManager.GetSceneByName("MultiplayerTrack1").buildIndex != SceneManager.GetActiveScene().buildIndex)
        {
            mine = true;
        } else if (view.IsMine)
        {
            mine = true;
        } else
        {
            mine = false;
        }

        transform.parent.GetChild(2).GetComponent<CameraFollow>().mine = mine;

        if (mine)
        {
            controls = new VehicleControls();

            controls.Gameplay.Forward.performed += ctx => throttle = ctx.ReadValue<float>();
            controls.Gameplay.Forward.canceled += ctx => throttle = 0f;

            controls.Gameplay.Backwards.performed += ctx => brakeStrength = ctx.ReadValue<float>();
            controls.Gameplay.Backwards.canceled += ctx => brakeStrength = 0f;

            controls.Gameplay.Left.performed += ctx => leftTurn = ctx.ReadValue<float>();
            controls.Gameplay.Left.canceled += ctx => leftTurn = 0f;

            controls.Gameplay.Right.performed += ctx => righTurn = ctx.ReadValue<float>();
            controls.Gameplay.Right.canceled += ctx => righTurn = 0f;

            if (autoGearShifting == false)
            {
                controls.Gameplay.GearUp.performed += ctx => StartCoroutine(GearSwitchUp());
                controls.Gameplay.GearDown.performed += ctx => StartCoroutine(GearSwitchDown());
            }

            controls.Gameplay.Back.performed += ctx => optionsMenuOn();
        }
    }

    void Start ()
    {
        rb = GetComponent<Rigidbody>();

        rb.centerOfMass = new Vector3(0, -0.6f, 0);

        FLWheelSpawn = Instantiate(wheel, this.transform);
        FRWheelSpawn = Instantiate(wheel, this.transform);
        BLWheelSpawn = Instantiate(wheel, this.transform);
        BRWheelSpawn = Instantiate(wheel, this.transform);

        FindObjectOfType<AudioManager>().Play("Car"); // sound of engine
    }

    void Update ()
    {
        if (!gearSwitching) { isNeutral = false; } else if (gearSwitching) { isNeutral = true; }

        if (((throttle > brakeStrength) && (rb.velocity.magnitude < 0.5f)) || (transform.InverseTransformDirection(rb.velocity).z > 0.1f)) {
            reverse = false;
        } else if (((brakeStrength > throttle) && (rb.velocity.magnitude < 0.5f)) || (transform.InverseTransformDirection(rb.velocity).z < -0.1f))
        {
            reverse = true;
        }

        currentGearRatio = gears[gearSelection];

        if ((gearSelection < (gears.Length - 1)) && autoGearShifting) {
            if ((currentEngineRPM > redLineRPM) && autoGearShifting && canSwitchGears && (AverageSlipDirectionMagnitude < minSlideSpeed)) {
                canSwitchGears = false;
                StartCoroutine(GearSwitchUp());
            }
        }
        
        if ((gearSelection > 0) && autoGearShifting) {
            if (((redLineRPM - (maxEngineRPM / 10)) > (wheelRPM * gears[gearSelection - 1])) && autoGearShifting && canSwitchGears) {
                canSwitchGears = false;
                StartCoroutine(GearSwitchDown());
            }
        }      

        AverageSlipDirectionMagnitude = (FLSlipDirection.magnitude + FRSlipDirection.magnitude + BLSlipDirection.magnitude + BRSlipDirection.magnitude) / 4;

        if ((rb.velocity.magnitude > 3f) && tractionControl && (AverageSlipDirectionMagnitude > (minSlideSpeed * 0.95)) && (transform.InverseTransformDirection(rb.velocity).z > 0) && (Mathf.Abs(transform.InverseTransformDirection(rb.velocity).z) > Mathf.Abs(transform.InverseTransformDirection(rb.velocity).x)) && OnGround) {
            wheelThrottle = Mathf.Lerp(wheelThrottle, 0, 0.1f);
        } else if (throttle == 0) {
            wheelThrottle = 0f;
        } else if (tractionControl && OnGround)
        {
            wheelThrottle = Mathf.Lerp(wheelThrottle, throttle, 0.15f);
        } else {wheelThrottle = throttle; }

        if ((rb.velocity.magnitude > 3f) && brakeControl && (AverageSlipDirectionMagnitude > (minSlideSpeed * 0.95)) && (transform.InverseTransformDirection(rb.velocity).z > 0) && (Mathf.Abs(transform.InverseTransformDirection(rb.velocity).z) > Mathf.Abs(transform.InverseTransformDirection(rb.velocity).x)) && OnGround) {
            wheelBrake = Mathf.Lerp(wheelBrake, 0, 0.1f);
        } else if (brakeStrength == 0) {
            wheelBrake = 0f;
        } else if (brakeControl && OnGround)
        {
            wheelBrake = Mathf.Lerp(wheelBrake, brakeStrength, 0.15f);
        } else {wheelBrake = brakeStrength; }
    }

    void FixedUpdate()
    {
        rb.AddForce(-transform.up * rb.velocity.magnitude * downforce);

        speed = rb.velocity.magnitude;

        RaycastHit FLhit;
        RaycastHit FRhit;
        RaycastHit BLhit;
        RaycastHit BRhit;

        OnGround = true;

        if (Physics.Raycast((transform.TransformPoint(-xWidth, 0f, zLength)), -transform.up, out FLhit, maxSuspensionDistance)) {
            FLVelocity = (FLhit.distance - FLPreviousPosition);
            FLPreviousPosition = FLhit.distance;
            PushFL = (Mathf.Clamp(-FLVelocity * dampAmount, 0, Mathf.Infinity) + suspensionMultiplyer) * ((maxSuspensionDistance) - FLhit.distance);
            rb.AddForceAtPosition(transform.up * PushFL, transform.TransformPoint(-xWidth, 0f, zLength));
            FLWheelSpawn.transform.position = FLhit.point + (wheelSize / 2 * transform.up);
            rb.AddForceAtPosition(transform.TransformDirection(FLSlipDirection) * FLgrip, FLhit.point);
            FLWheelSpawn.GetComponent<WheelSkid>().grounded = true;
        } else { FLWheelSpawn.GetComponent<WheelSkid>().grounded = false; OnGround = false; }
        FLWheelSpawn.GetComponent<WheelSkid>().slipping = FLSlip;
        FLWheelSpawn.GetComponent<WheelSkid>().hit = FLhit;
        FLWheelSpawn.GetComponent<WheelSkid>().skidmarksController = skids.GetComponent<Skidmarks>();
        FLWheelSpawn.transform.localScale = new Vector3(wheelSize, wheelSize, wheelSize);
        FLWheelSpawn.transform.GetChild(0).Rotate(0, -wheelRPM * 360 / 50 / 60, 0);
        FLSlipDirectionZ = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(-xWidth, 0f, zLength))).z;
        FLSlipDirectionX = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(-xWidth, 0f, zLength))).x;
        FLSlipDirection = Quaternion.AngleAxis(turnAngle, transform.up) * ((new Vector3(0, 0, Mathf.PI * wheelSize * wheelRPM / 60))) - new Vector3(FLSlipDirectionX, 0, FLSlipDirectionZ);
        if (FLSlipDirection.magnitude > minSlideSpeed) { FLgrip = slipGrip * PushFL / FLSlipDirection.magnitude; FLSlip = true; } else { FLgrip = stableGrip * PushFL; FLSlip = false; }

        if (Physics.Raycast((transform.TransformPoint(xWidth, 0f, zLength)), -transform.up, out FRhit, maxSuspensionDistance)) {
            FRVelocity = (FRhit.distance - FRPreviousPosition);
            FRPreviousPosition = FRhit.distance;
            PushFR = (Mathf.Clamp(-FRVelocity * dampAmount, 0, Mathf.Infinity) + suspensionMultiplyer) * ((maxSuspensionDistance) - FRhit.distance);
            rb.AddForceAtPosition(transform.up * PushFR, transform.TransformPoint(xWidth, 0f, zLength));
            FRWheelSpawn.transform.position = FRhit.point + (wheelSize / 2 * transform.up);
            rb.AddForceAtPosition(transform.TransformDirection(FRSlipDirection) * FRgrip, FRhit.point);
            FRWheelSpawn.GetComponent<WheelSkid>().grounded = true;
        } else { FRWheelSpawn.GetComponent<WheelSkid>().grounded = false; OnGround = false; }
        FRWheelSpawn.GetComponent<WheelSkid>().slipping = FRSlip;
        FRWheelSpawn.GetComponent<WheelSkid>().hit = FRhit;
        FRWheelSpawn.GetComponent<WheelSkid>().skidmarksController = skids.GetComponent<Skidmarks>();
        FRWheelSpawn.transform.localScale = new Vector3(wheelSize, wheelSize, wheelSize);
        FRWheelSpawn.transform.GetChild(0).Rotate(0, -wheelRPM * 360 / 50 / 60, 0);
        FRSlipDirectionZ = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(xWidth, 0f, zLength))).z;
        FRSlipDirectionX = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(xWidth, 0f, zLength))).x;
        FRSlipDirection = Quaternion.AngleAxis(turnAngle, transform.up) * ((new Vector3(0, 0, Mathf.PI * wheelSize * wheelRPM / 60))) - new Vector3(FRSlipDirectionX, 0, FRSlipDirectionZ);
        if (FRSlipDirection.magnitude > minSlideSpeed) { FRgrip = slipGrip * PushFR / FRSlipDirection.magnitude; FRSlip = true; } else { FRgrip = stableGrip * PushFR; FRSlip = false; }

        if (Physics.Raycast((transform.TransformPoint(-xWidth, 0f, -zLength)), -transform.up, out BLhit, maxSuspensionDistance)) {
            BLVelocity = (BLhit.distance - BLPreviousPosition);
            BLPreviousPosition = BLhit.distance;
            PushBL = (Mathf.Clamp(-BLVelocity * dampAmount, 0, Mathf.Infinity) + suspensionMultiplyer) * ((maxSuspensionDistance) - BLhit.distance);
            rb.AddForceAtPosition(transform.up * PushBL, transform.TransformPoint(-xWidth, 0f, -zLength));
            BLWheelSpawn.transform.position = BLhit.point + (wheelSize / 2 * transform.up);
            rb.AddForceAtPosition(transform.TransformDirection(BLSlipDirection) * BLgrip, BLhit.point);
            BLWheelSpawn.GetComponent<WheelSkid>().grounded = true;
        } else { BLWheelSpawn.GetComponent<WheelSkid>().grounded = false; OnGround = false; }
        BLWheelSpawn.GetComponent<WheelSkid>().slipping = BLSlip;
        BLWheelSpawn.GetComponent<WheelSkid>().hit = BLhit;
        BLWheelSpawn.GetComponent<WheelSkid>().skidmarksController = skids.GetComponent<Skidmarks>();
        BLWheelSpawn.transform.localScale = new Vector3(wheelSize, wheelSize, wheelSize);
        BLWheelSpawn.transform.GetChild(0).Rotate(0, -wheelRPM * 360 / 50 / 60, 0);
        BLSlipDirectionZ = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(-xWidth, 0f, -zLength))).z;
        BLSlipDirectionX = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(-xWidth, 0f, -zLength))).x;
        BLSlipDirection = ((new Vector3(0, 0, Mathf.PI * wheelSize * wheelRPM / 60))) - new Vector3(BLSlipDirectionX, 0, BLSlipDirectionZ);
        if (BLSlipDirection.magnitude > minSlideSpeed) { BLgrip = slipGrip * PushBL / BLSlipDirection.magnitude; BLSlip = true; } else { BLgrip = stableGrip * PushBL; BLSlip = false; }

        if (Physics.Raycast((transform.TransformPoint(xWidth, 0f, -zLength)), -transform.up, out BRhit, maxSuspensionDistance)) {
            BRVelocity = (BRhit.distance - BRPreviousPosition);
            BRPreviousPosition = BRhit.distance;
            PushBR = (Mathf.Clamp(-BRVelocity * dampAmount, 0, Mathf.Infinity) + suspensionMultiplyer) * ((maxSuspensionDistance) - BRhit.distance);
            rb.AddForceAtPosition(transform.up * PushBR, transform.TransformPoint(xWidth, 0f, -zLength));
            BRWheelSpawn.transform.position = BRhit.point + (wheelSize / 2 * transform.up);
            rb.AddForceAtPosition(transform.TransformDirection(BRSlipDirection) * BRgrip, BRhit.point);
            BRWheelSpawn.GetComponent<WheelSkid>().grounded = true;
        } else { BRWheelSpawn.GetComponent<WheelSkid>().grounded = false; OnGround = false; }
        BRWheelSpawn.GetComponent<WheelSkid>().slipping = BRSlip;
        BRWheelSpawn.GetComponent<WheelSkid>().hit = BRhit;
        BRWheelSpawn.GetComponent<WheelSkid>().skidmarksController = skids.GetComponent<Skidmarks>();
        BRWheelSpawn.transform.localScale = new Vector3(wheelSize, wheelSize, wheelSize);
        BRWheelSpawn.transform.GetChild(0).Rotate(0, -wheelRPM * 360 / 50 / 60, 0);
        BRSlipDirectionZ = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(xWidth, 0f, -zLength))).z;
        BRSlipDirectionX = this.transform.InverseTransformDirection(rb.GetPointVelocity(transform.TransformPoint(xWidth, 0f, -zLength))).x;
        BRSlipDirection = ((new Vector3(0, 0, Mathf.PI * wheelSize * wheelRPM / 60))) - new Vector3(BRSlipDirectionX, 0, BRSlipDirectionZ);
        if (BRSlipDirection.magnitude > minSlideSpeed) { BRgrip = slipGrip * PushBR / BRSlipDirection.magnitude; BRSlip = true; } else { BRgrip = stableGrip * PushBR; BRSlip = false; }

        if (turnControl && ((FLSlipDirection.magnitude > (minSlideSpeed * 0.9f)) || (FRSlipDirection.magnitude > (minSlideSpeed * 0.9f))) && (Mathf.Abs(transform.InverseTransformDirection(rb.velocity).z) / 10) > Mathf.Abs(transform.InverseTransformDirection(rb.velocity).x)) {
            turnAngle = Mathf.Lerp(turnAngle, 0, 0.3f);
        } else if (turnControl) {
            turnAngle = Mathf.Lerp(turnAngle, (righTurn - leftTurn) / (Mathf.Clamp(1 + rb.velocity.magnitude / 17, 1, 10)) * turnMultiplyer, 0.1f);
        } else {
            turnAngle = (righTurn - leftTurn) / (Mathf.Clamp(1 + rb.velocity.magnitude / 17, 1, 10)) * turnMultiplyer;
        }

        FLWheelSpawn.transform.localEulerAngles = new Vector3(0, turnAngle, 0);
        FRWheelSpawn.transform.localEulerAngles = new Vector3(0, turnAngle, 0);

        //this is when not neutral
        if (!isNeutral) {
            if (!reverse) {
                currentEngineRPM = Mathf.Lerp(currentEngineRPM, Mathf.Clamp(currentEngineRPM + (wheelThrottle * enginePower * currentGearRatio) - (wheelBrake * brakePressure) - engineBraking + (-(FLSlipDirection.z + FRSlipDirection.z + BLSlipDirection.z + BRSlipDirection.z) * 100), 0, maxEngineRPM), 0.1f);
                wheelRPM = currentEngineRPM / currentGearRatio;
            } else if (reverse) {
                currentEngineRPM = Mathf.Lerp(currentEngineRPM, Mathf.Clamp(currentEngineRPM + (wheelBrake * enginePower * currentGearRatio) - (wheelThrottle * brakePressure) + engineBraking + ((FLSlipDirection.z + FRSlipDirection.z + BLSlipDirection.z + BRSlipDirection.z) * 100), 0, maxEngineRPM), 0.1f);
                wheelRPM = currentEngineRPM / -currentGearRatio;
            }
        }

        //this is when neutral
        if (isNeutral) { // miror for forward and reverse  //one of each for each to make different wheel speeds, at least the slipdirection resistance thing
            if (!reverse) {
                currentEngineRPM = Mathf.Abs(wheelRPM) * gears[gearSelection];
                wheelRPM = wheelRPM + (-(FLSlipDirection.z + FRSlipDirection.z + BLSlipDirection.z + BRSlipDirection.z) * 100f / (currentGearRatio * 10)) - ((wheelBrake * brakePressure) / (currentGearRatio * 10));
            }
            else if (reverse) {
                currentEngineRPM = Mathf.Abs(wheelRPM) * gears[gearSelection];
                wheelRPM = wheelRPM + (-(FLSlipDirection.z + FRSlipDirection.z + BLSlipDirection.z + BRSlipDirection.z) * 100f / (currentGearRatio * 10)) + ((wheelThrottle * brakePressure) / (currentGearRatio * 10));
            }
        }
        /*if (mine && OnGround)
        {
            //controller vibaration
            GamePad.SetVibration(playerIndex, (AverageSlipDirectionMagnitude - minSlideSpeed * 0.5f) / 8 * vibrationMultiplyer, Mathf.Clamp((throttle - wheelThrottle / 10) + (brakeStrength - wheelBrake / 10) + ((AverageSlipDirectionMagnitude - minSlideSpeed) * 100), 0, 0.4f) * vibrationMultiplyer); // (controller, left vibrator, right vibrator) ranges from 0 to 1
        }
        else if (mine)
        {
            GamePad.SetVibration(playerIndex, 0.015f * vibrationMultiplyer, 0.015f * vibrationMultiplyer);
        }
        else { GamePad.SetVibration(playerIndex, 0, 0); }
        */
    }
    /*
    void OnApplicationQuit()
    {//stop vibration when quitting
        GamePad.SetVibration(playerIndex, 0, 0);
    }*/

    //right now the torgue and poewr curves are stright, they dont peak at max rpm but change with gears
    //engines donw work at 0rpm, time for switching gears, automatic, add in so more rpm is mroe power

    IEnumerator GearSwitchUp()
    {
        if ((gearSelection < (gears.Length - 1)))
        {
            gearSelection++;
        }
        gearSwitching = true;
        StartCoroutine(GearSwitchTimeBuffer());
        yield return new WaitForSeconds(gearSwapSpeed);
        GearSwitchTimeBuffer();
        gearSwitching = false;
    }

    IEnumerator GearSwitchDown()
    {
        FindObjectOfType<AudioManager>().Play("Pop"); // sound of pops
        if ((gearSelection > 0))
        {
            gearSelection--;
        }
        gearSwitching = true;
        StartCoroutine(GearSwitchTimeBuffer());
        yield return new WaitForSeconds(gearSwapSpeed);
        GearSwitchTimeBuffer();
        gearSwitching = false;
    }

    IEnumerator GearSwitchTimeBuffer()
    {
        yield return new WaitForSeconds(gearSwapSpeed + 0.3f);
        canSwitchGears = true;
    }

    void optionsMenuOn() {
        optionsMenu = !optionsMenu;
    }

    private void OnEnable() { controls.Enable(); }
    private void OnDisable() { controls.Disable(); }
}