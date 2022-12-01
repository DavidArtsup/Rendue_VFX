using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class PlayerControllerJerem : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _rigid;
    [SerializeField]
    float _speed;
    [SerializeField] float gravity;

    [SerializeField] float jumpForce;

    [SerializeField] bool isGrounded;
    new CapsuleCollider collider;
    [SerializeField]  LayerMask layerMaskGround;

    [SerializeField]  float jumpDuration = 1f;
    float jumpTimer = 0f;

    public InputAction horizontal;
    public InputAction vertical;
    public InputAction jump;

    private Collider[] buffer = new Collider[8];

    public LayerMask groundMask;
    [Range(0, 90)] public float maxGroundAngle = 45f;

    public AnimationCurve jumpCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    Vector3 verticalDirection = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        horizontal.Enable();
        vertical.Enable();
        jump.Enable();
        _rigid = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckGround();
        Movement();
        Jump();

        ApplyGravity();

        MoveSweepTestRecurs(verticalDirection * Time.deltaTime, 3);
        verticalDirection = Vector3.zero;

        ExtractFromColliders();
    }

    void CheckGround()
    {
        float halfHeight = (collider.height * .5f) - collider.radius;
        Vector3 bottom = collider.bounds.center + (Vector3.down * halfHeight);
        bool _isGrounded = Physics.SphereCast(bottom + transform.up * Physics.defaultContactOffset, collider.radius, -transform.up, out RaycastHit hit, Physics.defaultContactOffset * 3, groundMask) && (Vector3.Angle(transform.up, hit.normal) < maxGroundAngle);

        if(_isGrounded != isGrounded)
        {
            OnGroundChange(_isGrounded);
        }
    }

    void OnGroundChange(bool _isGrounded)
    {
        isGrounded = _isGrounded;
    }

    private void ExtractFromColliders()
    {
        float halfHeight = (collider.height * .5f) - collider.radius;

        Vector3 bottom = collider.bounds.center + (Vector3.down * halfHeight);
        Vector3 top = collider.bounds.center + (Vector3.up * halfHeight);


        int amount = Physics.OverlapCapsuleNonAlloc(bottom, top, collider.radius, buffer);
        for (int i = 0; i < amount; i++)
        {
            if (buffer[i] == collider)
            {
                continue;
            }

            if (Physics.ComputePenetration(collider, _rigid.position, _rigid.rotation,
                                       buffer[i], buffer[i].transform.position, buffer[i].transform.rotation,
                                       out Vector3 direction, out float distance))
            {
                _rigid.MovePosition(_rigid.position + (direction * (distance + Physics.defaultContactOffset)));
            }
        }

        amount = Physics.OverlapCapsuleNonAlloc(bottom, top, collider.radius, buffer);
        Debug.Log("Amount => " + amount);
    }

    private void Jump()
    {
        if(jumpTimer>0f)
        {
            jumpTimer = Mathf.Max(0f, jumpTimer - Time.deltaTime);

            Vector3 direction = transform.up * jumpForce;

            float percent = (jumpDuration - jumpTimer) / jumpDuration;
            float jump = jumpCurve.Evaluate(percent);

            verticalDirection = direction * jump;
            MoveSweepTestRecurs(direction, 3);

            if(jumpTimer == 0f)
            {
                ResetVerticalVelocity();
            }
        }
        else if (isGrounded && jump.WasPerformedThisFrame())
        {
            jumpTimer = jumpDuration;
            verticalDirection = Vector3.zero;
        }
    }

    void ResetVerticalVelocity()
    {
        verticalDirection = Vector3.zero;
    }

    private void FixedUpdate()
    {
        return;

       /* isGrounded = Physics.Raycast(_rigid.position, Vector3.down, collider.height/2+Physics.defaultContactOffset,layerMaskGround );
        Debug.Log(isGrounded);
        if(!isGrounded)
        {
            Gravity();
            gravity *= 1+Time.deltaTime;
        }
        else
        {
            jumpTime = 0.5f;
            gravity = 10;
        }

        if(isJumping && jumpTime > 0)
        {
            jumpTime -= Time.deltaTime;
            MoveSweepTestRecurs(Vector3.up * jumpForce * Time.deltaTime *jumpTime, 2);
        }*/
    }

    private void Movement()
    {
        Vector3 direction = new Vector3(horizontal.ReadValue<float>(), 0f, vertical.ReadValue<float>()).normalized * _speed * Time.deltaTime;
        MoveSweepTestRecurs(direction, 3);
    }

    private void MoveSweepTestRecurs(Vector3 velocity, int recurs)
    {
        float distance = velocity.magnitude;

        if(_rigid.SweepTest(velocity.normalized, out RaycastHit hit, distance, QueryTriggerInteraction.Ignore))
        {
            //ClampDistance with contact offset;
            distance = Mathf.Max(0f, hit.distance - Physics.defaultContactOffset);
        }

        Vector3 displacement = velocity.normalized * distance;
        _rigid.MovePosition(_rigid.position + displacement);
        velocity -= displacement;

        velocity -= hit.normal * Vector3.Dot(velocity, hit.normal);

        //recursivity
        if((--recurs != 0) && (velocity != Vector3.zero))
        {
            MoveSweepTestRecurs(velocity, recurs);
        }
    }

    private void ApplyGravity()
    {
        if (!isGrounded && jumpTimer <= 0f)
        {
            verticalDirection += transform.up * gravity * Time.deltaTime;
        }
    }
}
