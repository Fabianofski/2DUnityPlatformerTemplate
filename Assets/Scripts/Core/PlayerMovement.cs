// /**
//  * This file is part of: 2D Unity Platformer Template
//  * Copyright (C) 2023 Fabian Friedrich
//  * Distributed under the terms of the MIT license (cf. LICENSE.md file)
//  **/

using System;
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
    
    [Header("Squash and Stretch")]
    [SerializeField] private GameObject spriteGameObject;
    [SerializeField] private Vector3 jumpSquash = new Vector3(0.6f, 1.3f, 0.4f);
    private Vector2 _size;

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
    private bool _isCoyoteGrounded;
    [SerializeField] private float coyoteJumpTime = 0.2f;

    private void Start()
    {
        _extraJumps = initialExtraJumps;
        _rb = GetComponent<Rigidbody2D>();

        _size = spriteGameObject.transform.localScale;
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
        var hit = Physics2D.Raycast(feet.position, Vector2.down, 1f,  1 << LayerMask.NameToLayer("Ground"));
        if (hit && Math.Abs(hit.normal.y) < 0.95)
        {
            _rb.velocity = new Vector2(maxMoveSpeed * _horizontalDirection, _rb.velocity.y);
            _rb.constraints = _horizontalDirection == 0
                ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                : RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
             _rb.AddForce(new Vector2(_horizontalDirection, 0) * movementAcceleration);
             _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        

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
        var wasGrounded = _isGrounded;
        _isGrounded = Physics2D.OverlapBox(new Vector2(feet.position.x, feet.position.y - 0.05f), 
                                                    new Vector2(0.95f, 0.1f), 0, groundLayer);
        
        if (!_isGrounded || _isCoyoteGrounded) return;
        _isCoyoteGrounded = true;
        _extraJumps = initialExtraJumps;
        StartCoroutine(ResetIsGrounded());
    }

    private IEnumerator ResetIsGrounded()
    {
        yield return new WaitForSeconds(coyoteJumpTime);
        _isCoyoteGrounded = Physics2D.OverlapBox(new Vector2(feet.position.x, feet.position.y - 0.05f), 
            new Vector2(0.95f, 0.1f), 0, groundLayer);;
    }

    private void ApplyJump()
    {
        if (_isCoyoteGrounded && _jumpingBuffer)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, 0);
            _rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
            StopCoroutine(ResetIsGrounded());
            _isCoyoteGrounded = false;
            _jumpingBuffer = false;

            SquashAndStretch(jumpSquash.x, jumpSquash.y, jumpSquash.z);
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
            
            SquashAndStretch(jumpSquash.x, jumpSquash.y, jumpSquash.z);
        }
        ApplyFallMultiplier();
    }

    private void SquashAndStretch(float x, float y, float time)
    {
        LeanTween.scale(spriteGameObject, new Vector2(_size.x * x, _size.y * y), time / 2).setEase(LeanTweenType.easeInQuad).setOnComplete(
            () =>
            {
                LeanTween.scale(spriteGameObject, _size, time / 2).setEase(LeanTweenType.easeOutQuad);
            });
    }

    private void ApplyFallMultiplier()
    {
        if (_rb.velocity.y < 0 && !_isGrounded)
            _rb.gravityScale = fallMultiplier;
        else if (_rb.velocity.y > 0 && !_jumpingInput)
            _rb.gravityScale = lowJumpFallMultiplier;
        else
            _rb.gravityScale = 2;
    }
    
    private void OnDrawGizmos()
    {
        var pos = feet.position;
        
        Gizmos.color = Color.black;
        Gizmos.DrawCube(new Vector2(pos.x, pos.y - extraJumpMinHeight / 2), 
            new Vector2(0.95f, extraJumpMinHeight));
        
        Gizmos.color = _isCoyoteGrounded ? Color.green : Color.red;
        Gizmos.DrawCube(new Vector2(pos.x, pos.y - 0.05f), new Vector2(0.95f, 0.1f));
    }
}