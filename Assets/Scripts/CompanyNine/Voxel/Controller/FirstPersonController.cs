using System;
using NaughtyAttributes;
using UnityEngine;
using Cursor = UnityEngine.Cursor;

namespace CompanyNine.Voxel.Controller
{
    public class FirstPersonController : MonoBehaviour
    {
        [BoxGroup("Movement Variables:")] public float rotationSpeed = 2;
        [BoxGroup("Movement Variables:")] public float movementSpeed = 3;
        [BoxGroup("Movement Variables:")] public float sprintMultiplier = 1.5f;
        [BoxGroup("Movement Variables:")] public float jumpForce = 5f;

        [BoxGroup("Movement Variables:")] [ReadOnly]
        public Vector3 velocity;

        [BoxGroup("Character Attributes")] public float playerWidth = .4f;
        [BoxGroup("Character Attributes")] public float playerHeight = 1.8f;

        [BoxGroup("Character Attributes")] [ReadOnly]
        public bool isSprinting;

        [BoxGroup("Character Attributes")] [ReadOnly]
        public bool isGrounded;

        private const int MaximumLookAngle = 90;

        // movement tracking variables
        private float _horizontal;
        private float _vertical;
        private float _rotationX;
        private float _rotationY;
        private float _verticalMomentum;
        private bool _jumpRequest;

        // position tracking variables
        private Vector3 _position;
        private float _forward;
        private float _backward;
        private float _right;
        private float _left;

        // game object references
        private Transform _camera;
        private World _world;

        private void Start()
        {
            _camera = GameObject.Find("Main Camera").transform;
            _world = GameObject.Find("World").GetComponent<World>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            GetPlayerInputs();
        }

        private void FixedUpdate()
        {
            _position = transform.position;
            _right = _position.x + playerWidth;
            _left = _position.x - playerWidth;
            _forward = _position.z + playerWidth;
            _backward = _position.z - playerWidth;

            if (_jumpRequest)
            {
                Jump();
            }

            CalculateRotation();
            CalculateVelocity();

            transform.Translate(velocity, Space.World);
            _camera.localRotation = Quaternion.Euler(-_rotationY, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, _rotationX, 0f);
        }

        private void GetPlayerInputs()
        {
            _rotationX += Input.GetAxis("Mouse X") * rotationSpeed;
            _rotationY += Input.GetAxis("Mouse Y") * rotationSpeed;
            _vertical = Input.GetAxis("Vertical");
            _horizontal = Input.GetAxis("Horizontal");

            isSprinting = Input.GetButton("Sprint");

            if (Input.GetButtonDown("Jump"))
            {
                _jumpRequest = true;
            }
        }

        private void CalculateRotation()
        {
            _rotationY = Mathf.Clamp(_rotationY, -MaximumLookAngle,
                MaximumLookAngle);
        }

        private void CalculateVelocity()
        {
            var playerTransform = transform;

            // Apply Gravity
            if (_verticalMomentum > World.Gravity)
            {
                _verticalMomentum += Time.fixedDeltaTime * World.Gravity;
            }

            var currentSpeed = isSprinting
                ? movementSpeed * sprintMultiplier
                : movementSpeed;

            velocity = ((playerTransform.forward * _vertical) +
                         (playerTransform.right * _horizontal)) *
                        (Time.fixedDeltaTime * currentSpeed);

            velocity += Vector3.up * (_verticalMomentum * Time.fixedDeltaTime);

            if ((velocity.z > 0 && ForwardMovement) ||
                (velocity.z < 0 && BackwardMovement))
            {
                velocity.z = 0;
            }

            if ((velocity.x > 0 && RightMovement) ||
                (velocity.x < 0 && LeftMovement))
            {
                velocity.x = 0;
            }

            if (velocity.y < 0)
            {
                velocity.y = CheckDownSpeed(velocity.y);
            }
            else if (velocity.y > 0)
            {
                velocity.y = CheckUpSpeed(velocity.y);
            }
        }

        private void Jump()
        {
            _verticalMomentum = jumpForce;
            isGrounded = false;
            _jumpRequest = false;
        }

        private float CheckDownSpeed(float downSpeed)
        {
            var downLocation = _position.y + downSpeed;

            if (
                (_world.IsVoxelSolid(_left, downLocation, _backward) &&
                 (!LeftMovement && !BackwardMovement)) ||
                (_world.IsVoxelSolid(_right, downLocation, _backward) &&
                 (!RightMovement && !BackwardMovement)) ||
                (_world.IsVoxelSolid(_left, downLocation, _forward) &&
                 (!LeftMovement && !ForwardMovement)) ||
                (_world.IsVoxelSolid(_right, downLocation, _forward) &&
                 (!RightMovement && !ForwardMovement)))
            {
                isGrounded = true;
                return 0;
            }

            isGrounded = false;
            return downSpeed;
        }

        private float CheckUpSpeed(float upSpeed)
        {
            var upLocation = _position.y + upSpeed + 2f;

            if (
                (_world.IsVoxelSolid(_left, upLocation, _backward) &&
                 (!LeftMovement && !BackwardMovement)) ||
                (_world.IsVoxelSolid(_right, upLocation, _backward) &&
                 (!RightMovement && !BackwardMovement)) ||
                (_world.IsVoxelSolid(_left, upLocation, _forward) &&
                 (!LeftMovement && !ForwardMovement)) ||
                (_world.IsVoxelSolid(_right, upLocation, _forward) &&
                 (!RightMovement && !ForwardMovement)))
            {
                _verticalMomentum = 0;
                return 0;
            }

            return upSpeed;
        }

        private bool ForwardMovement =>
            _world.IsVoxelSolid(_position.x, _position.y, _forward) ||
            _world.IsVoxelSolid(_position.x, _position.y + 1f, _forward);

        private bool BackwardMovement =>
            _world.IsVoxelSolid(_position.x, _position.y, _backward) ||
            _world.IsVoxelSolid(_position.x, _position.y + 1f, _backward);

        private bool LeftMovement =>
            _world.IsVoxelSolid(_left, _position.y, _position.z) ||
            _world.IsVoxelSolid(_left, _position.y + 1f, _position.z);

        private bool RightMovement =>
            _world.IsVoxelSolid(_right, _position.y, _position.z) ||
            _world.IsVoxelSolid(_right, _position.y + 1f, _position.z);
    }
}