// /**
//  * This file is part of: 2D Unity Platformer Template
//  * Copyright (C) 2023 Fabian Friedrich
//  * Distributed under the terms of the MIT license (cf. LICENSE.md file)
//  **/

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Components")] 
    private Rigidbody2D _rb;
    [SerializeField] private Transform feet;
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Variables")] 
    [SerializeField] private float movementAcceleration = 70f;
    [SerializeField] private float maxMoveSpeed = 12f;
    [SerializeField] private float groundLinearDrag = 7f;
    [SerializeField] private float airLinearDrag = 2.5f;
    private float _horizontalDirection;

    [Header("Jumping")] 
    [SerializeField] private float jumpForce = 25;
    [SerializeField] private int initialExtraJumps = 1;
    private int _extraJumps;
    [SerializeField] private float fallMultiplier = 8;
    [SerializeField] private float lowJumpFallMultiplier = 14;

    [Header("Buffer Times")]
    private bool _jumpingInput;
    private bool _jumpingBuffer;
    [SerializeField] private float jumpInputBufferTime = 0.2f;
    [SerializeField] private float extraJumpMinHeight = 1;
    private bool _isGrounded;
    [SerializeField] private float coyoteJumpTime = 0.2f;

    private void Start()
    {
        _extraJumps = initialExtraJumps;
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnMove(InputValue inputValue)
    {
        var input = inputValue.Get<Vector2>();
        _horizontalDirection = input.x;
    }

    private void OnJump(InputValue inputValue)
    {
        _jumpingInput = inputValue.isPressed;
        if (!_jumpingInput || _jumpingBuffer) return;
        _jumpingBuffer = true;
        Invoke(nameof(ResetJump), jumpInputBufferTime);
    }

    private void ResetJump()
    {
        _jumpingBuffer = false;
    }
    
    private void FixedUpdate()
    {
        MoveCharacter();
        ApplyLinearDrag();
        CheckIfGrounded();
        ApplyJump();
    }
    
    private void MoveCharacter()
    {
        _rb.AddForce(new Vector2(_horizontalDirection, 0f) * movementAcceleration);

        if (Mathf.Abs(_rb.velocity.x) > maxMoveSpeed)
            _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * maxMoveSpeed, _rb.velocity.y);
    }
    
    private void ApplyLinearDrag()
    {
        var velocity = _rb.velocity;
        var isChangingDirection = (velocity.x > 0f && _horizontalDirection < 0f) ||
                                  (velocity.x < 0f && _horizontalDirection > 0f);
        if ((Mathf.Abs(_horizontalDirection) < 0.4f || isChangingDirection) && _isGrounded)
        {
            _rb.drag = groundLinearDrag;
        }
        else if (!_isGrounded)
        {
            _rb.drag = airLinearDrag;
        }
        else
        {
            _rb.drag = 0f;
        }
    }

    private void CheckIfGrounded()
    {
        var isOnGround = Physics2D.OverlapBox(new Vector2(feet.position.x, feet.position.y - 0.025f), 
                                                    new Vector2(0.95f, 0.05f), 0, groundLayer);
        if (!isOnGround || _isGrounded) return;
        _isGrounded = true;
        _extraJumps = initialExtraJumps;
        StartCoroutine(ResetIsGrounded());
    }

    private IEnumerator ResetIsGrounded()
    {
        yield return new WaitForSeconds(coyoteJumpTime);
        _isGrounded = false;
    }

    private void ApplyJump()
    {
        if (_isGrounded && _jumpingBuffer)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, 0);
            _rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
            StopCoroutine(ResetIsGrounded());
            _isGrounded = false;
            _jumpingBuffer = false;
        }
        else if (_jumpingBuffer && _extraJumps > 0)
        {
            if (Physics2D.OverlapBox(
                    new Vector2(feet.position.x, feet.position.y - extraJumpMinHeight / 2), 
                    new Vector2(0.95f, extraJumpMinHeight), 
                    0, 
                    groundLayer))
                return;
            _rb.velocity = new Vector2(_rb.velocity.x, 0);
            _rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
            _extraJumps--;
        }
        ApplyFallMultiplier();
    }

    private void ApplyFallMultiplier()
    {
        if (_rb.velocity.y < 0)
            _rb.gravityScale = fallMultiplier;
        else if (_rb.velocity.y > 0 && !_jumpingInput)
            _rb.gravityScale = lowJumpFallMultiplier;
        else
            _rb.gravityScale = 2f;
    }
    
    private void OnDrawGizmos()
    {
        var pos = feet.position;
        
        Gizmos.color = Color.black;
        Gizmos.DrawCube(new Vector2(pos.x, pos.y - extraJumpMinHeight / 2), 
            new Vector2(0.95f, extraJumpMinHeight));
        
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawCube(new Vector2(pos.x, pos.y - 0.025f), new Vector2(0.95f, 0.05f));
    }
}