using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public GameObject Msg;
    public Camera Cam;

    public float Mass;
    public float Moment;
    public float Torque = 1.0f;
    public float Force = 1.0f;
    public float Offset = 32.0f;
    public float TimeCoefficient = 1.005f;

    private Rigidbody rb;
    private bool AutoMode;
    private MoveModes MoveMode;
    private MoveModes AutoMoveMode;
    private float Distance;
    private string TargetName;
    private Vector3 TargetPoint;
    private Vector3 TargetAngle;
    private Transform objectHit;

    private float TimeStep = 0.02f;
    private float TimeWork = 0.0f;
    private float TimeTotal = 0.0f;
    private int ForceNumber = 0;
    private Dictionary<MoveModes, float> T;
    private Dictionary<MoveModes, float> Tk;

    private List<float> DisturbM1 = new List<float>() { 0.0f, 0.0005f, 0.0005f, 0.0005f, 0.0005f };
    private List<float> DisturbM3 = new List<float>() { 0.0f, 0.0000f, 0.0000f, -0.002f, -0.002f };
    private List<float> DisturbF1 = new List<float>() { 0.0f, -0.05f, 0.05f, 0.05f, -0.05f };
    private List<float> DisturbF3 = new List<float>() { 0.0f, 0.0f, 0.0f, -0.1f, 0.1f };
    private int TestCase = 0;
    private float DM1 = 0.0f;
    private float DM3 = 0.0f;
    private float DF1 = 0.0f;
    private float DF3 = 0.0f;
    private float tk = 0.0f;
    private float t = 0.0f;

    enum MoveModes
    {
        EMPTY,
        ROTATION,
        MOVEMENT
    }

    void SetTarget()
    {
        RaycastHit hit;
        Ray ray = new Ray(gameObject.transform.position, Cam.transform.forward);
        //Debug.DrawRay(ray.origin, ray.direction, Color.green, 1, false);

        if (Physics.Raycast(ray, out hit))
        {
            if (Tk.Count == 2)
            {
                return;
            }

            objectHit = hit.transform;
            Distance = hit.distance;
            TargetName = hit.collider.name;
            TargetPoint = hit.point;
            TargetAngle = Cam.transform.localRotation.eulerAngles;

            float Angle = TargetAngle.y;
            if (Angle > 180)
            {
                Angle = 360.0f - Angle;
            }
                        
            float tk_2 = 0.0f;
            float val = 0.0f;

            // Rotation
            val = Angle * Mathf.Deg2Rad * Moment / Torque;
            tk = 2 * Mathf.Sqrt(val);
            tk = tk * 1.005f;
            tk_2 = tk / 2.0f;
            t = tk_2 - Mathf.Sqrt(tk_2 * tk_2 - val);

            TimeTotal += tk;
            Tk.Add(MoveModes.ROTATION, tk);
            T.Add(MoveModes.ROTATION, t);
            //Debug.Log(string.Format("Target: {0}, Tk: {1}, t: {2}", MoveModes.ROTATION, tk, t));
            //Debug.Log(string.Format("val: {0}, Angle: {1}, Moment: {2}, Torque: {3}, TimeStep: {4}", val, Angle, Moment, Torque, TimeStep));

            // Rondevouz
            val = Distance * Mass / Force;
            tk = 2 * Mathf.Sqrt(val);
            tk = tk * TimeCoefficient;
            tk_2 = tk / 2.0f;
            t = tk_2 - Mathf.Sqrt(tk_2 * tk_2 - val);

            TimeTotal += tk;
            Tk.Add(MoveModes.MOVEMENT, tk);
            T.Add(MoveModes.MOVEMENT, t);
            Debug.Log(string.Format("Target: {0}, Tk: {1}, t: {2}", MoveModes.MOVEMENT, tk, t));
            //Debug.Log(string.Format("val: {0}, Distance: {1}, Mass: {2}, Force: {3}, TimeStep: {4}", val, Distance, Mass, Force, TimeStep));
            Debug.Log(string.Format("{0:0.0000}\t{1:0.0000}", tk, t));

            AutoMoveMode = MoveModes.ROTATION;
        }
    }

    void MadeOffsetImpulse()
    {   
        var dir = -objectHit.right;
        float offsetForce = Mass * Offset / (TimeTotal * TimeStep);
        rb.AddForce(offsetForce * dir, ForceMode.Force);
        Debug.Log(string.Format("Offset Impulse: {0}", offsetForce));
    }

    void UpdateTelemetry()
    {
        string msg = string.Format("Авто режим: {0}", AutoMode);
        msg += string.Format("\nРежим движения: {0}", MoveMode);
        msg += string.Format("\nЦель: {0}", TargetName);
        msg += string.Format("\nДистация до цели: {0}", Distance);
        msg += string.Format("\nЦелевая точка: {0}", TargetPoint);
        msg += string.Format("\nЦелевой угол: {0}", TargetAngle);
        msg += string.Format("\n");
        msg += string.Format("\nTest Case: {0}, M1: {1}, M3: {2}, F1: {3}, F3: {4}", TestCase, DM1, DM3, DF1, DF3);        
        msg += string.Format("\n");
        msg += string.Format("\nДистация до цели: {0:0.00}", (gameObject.transform.position - TargetPoint).magnitude);
        if (AutoMoveMode != MoveModes.EMPTY)
        {
            msg += string.Format("\nАвто режим: {0}", AutoMoveMode);
            //msg += string.Format("\nДистация до цели: {0:0.00}", (gameObject.transform.position - TargetPoint).magnitude);
            msg += string.Format("\nВремя маневра: {0:0.00}", Tk[AutoMoveMode]);
            msg += string.Format("\nВремя работы двигателя: {0:0.00}", T[AutoMoveMode]);
            msg += string.Format("\nВремя до конца маневра: {0:0.00}", TimeWork - Tk[AutoMoveMode]);
            msg += string.Format("\nНакопленный импульс: {0}", ForceNumber);
        }
        
        Text txt = Msg.GetComponent<Text>();
        if (txt != null)
        {
            txt.text = msg;
        }
        else
        {
            Debug.Log(msg);
        }
    }

    void AddForceTorque(MoveModes mode, float F, Vector3 dir)
    {
        if (mode == MoveModes.ROTATION)
        {
            rb.AddTorque(F * dir, ForceMode.Force);
        } else if (mode == MoveModes.MOVEMENT)
        {
            rb.AddForce(F * dir, ForceMode.Force);
        }
    }

    void AutomaticRondevouz()
    {
        float tk = 0.0f;
        float t = 0.0f;
        float F = 0.0f;
        float D1 = 0.0f;
        float D3 = 0.0f;
        Vector3 dir = new Vector3(0.0f, 0.0f, 0.0f);
        if (AutoMoveMode == MoveModes.ROTATION)
        {
            tk = Tk[AutoMoveMode];
            t = T[AutoMoveMode];
            F = Torque;
            dir = this.transform.up;
            D1 = DM1;
            D3 = DM3;
            //rb.velocity = new Vector3(0.0f, 0.0f, 0.0f);
        } else if (AutoMoveMode == MoveModes.MOVEMENT)
        {
            tk = Tk[AutoMoveMode];
            t = T[AutoMoveMode];
            F = Force;
            dir = this.transform.forward;
            D1 = DF1;
            D3 = DF3;
            rb.angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
        }

        if (AutoMoveMode != MoveModes.EMPTY && TimeWork < tk + 1)
        {
            var direction = "";
            TimeWork += Time.deltaTime;
            if (TimeWork >= 0 && TimeWork <= t)
            {
                AddForceTorque(AutoMoveMode, F + D1, dir);
                ++ForceNumber;
                direction = "UP";

            }
            else if (TimeWork >= tk - t && ForceNumber > 0)
            {
                AddForceTorque(AutoMoveMode, -(F + D3), dir);
                --ForceNumber;
                direction = "DOWN";
                if (AutoMoveMode == MoveModes.ROTATION && ForceNumber == 0)
                {
                    AutoMoveMode = MoveModes.MOVEMENT;
                    TimeWork = 0.0f;
                } else if (AutoMoveMode == MoveModes.MOVEMENT && ForceNumber == 0)
                {
                    AutoMoveMode = MoveModes.EMPTY;
                    TimeWork = 0.0f;
                    rb.velocity = new Vector3(0.0f, 0.0f, 0.0f);
                    rb.angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);

                    Debug.Log(string.Format("Position:"));
                    var pos = transform.position;
                    var ang = transform.rotation.eulerAngles;
                    Debug.Log(string.Format("{0:0.0000}\t{1:0.0000}\t{2:0.0000}\t{3:0.0000}\t{4:0.0000}\t{5:0.0000}", pos.x, pos.y, pos.z, ang.x, ang.y, ang.z));
                    // Функционал

                    var J = 0.4210f;
                    var F1 = 100.0f * J / Mathf.Sqrt(pos.x*pos.x + pos.z*pos.z);
                    Debug.Log(string.Format("Функционал 1: {0:0.0000}", F1));
                    var F2 = 100.0f * J / Mathf.Sqrt(pos.x*pos.x + J*J);
                    Debug.Log(string.Format("Функционал 2: {0:0.0000}", F2));
                }
            } else
            {
                direction = "NONE";
            }

            if (AutoMoveMode == MoveModes.MOVEMENT)
            {
                // начальные возмущения скорости
                //AddForceTorque(MoveModes.MOVEMENT, F, this.transform.right);
            }
            //Debug.Log(string.Format("{0}, TorqueNumber: {1}, TimeWork: {2}, tk: {3}, t: {4}", direction, ForceNumber, TimeWork, tk, t));
        }

    }

    void InputProcessor()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            AutoMode = !AutoMode;
            if (AutoMode && MoveMode == MoveModes.ROTATION)
            {
                SetTarget();
                //MadeOffsetImpulse();
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            MoveMode = MoveMode == MoveModes.ROTATION ? MoveModes.MOVEMENT : MoveModes.ROTATION;
        }

        
        AutoMode = Input.GetKey(KeyCode.JoystickButton5);
        MoveMode = Input.GetKey(KeyCode.JoystickButton6) ? MoveModes.MOVEMENT : MoveModes.ROTATION;
        if (AutoMode && MoveMode == MoveModes.ROTATION)
        {
            SetTarget();
        }
        

        float F = MoveMode == MoveModes.ROTATION ? Torque : Force;

        if (Input.GetKey(KeyCode.JoystickButton8))
        {
            // UP
	    Debug.Log("UP");
            AddForceTorque(MoveMode, F, this.transform.up);
        }
        if (Input.GetKey(KeyCode.JoystickButton9))
        {
            // DOWN
	    Debug.Log("DOWN");
            AddForceTorque(MoveMode, F, -this.transform.up);
        }
        if (Input.GetKey(KeyCode.JoystickButton14))
        {
            // RIGHT
	    Debug.Log(string.Format("RIGHT: {0}", F));
            AddForceTorque(MoveMode, F, this.transform.right);
        }
        if (Input.GetKey(KeyCode.JoystickButton15))
        {
            // LEFT
	    Debug.Log(string.Format("LEFT: {0}", F));	
            AddForceTorque(MoveMode, F, -this.transform.right);
        }
        if (Input.GetKey(KeyCode.JoystickButton0))
        {
            // FORWARD
	    Debug.Log("FORWARD");
            AddForceTorque(MoveMode, F, this.transform.forward);
        }
        if (Input.GetKey(KeyCode.JoystickButton1))
        {
            // BACKWARD
	    Debug.Log("BACKWARD");
            AddForceTorque(MoveMode, F, -this.transform.forward);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TestCase = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestCase = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestCase = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestCase = 3;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestCase = 4;
        }
        if (TestCase >= 0 && TestCase <= 4)
        {
            DM1 = DisturbM1[TestCase];
            DM3 = DisturbM3[TestCase];
            DF1 = DisturbF1[TestCase];
            DF3 = DisturbF3[TestCase];
        }
    }


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.inertiaTensor = new Vector3(Moment, Moment, Moment);
        rb.mass = Mass;
        AutoMode = false;
        MoveMode = MoveModes.ROTATION;
                
        TimeStep = Time.fixedDeltaTime;
        AutoMoveMode = MoveModes.EMPTY;
        T = new Dictionary<MoveModes, float>();
        Tk = new Dictionary<MoveModes, float>();
    }
        
    void FixedUpdate()
    {
        AutomaticRondevouz();
        InputProcessor();
        UpdateTelemetry();
    }

    void Update()
    {
        
    }
}
