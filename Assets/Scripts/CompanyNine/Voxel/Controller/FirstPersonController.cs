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
        private float _posZ;
        private float _negZ;
        private float _posX;
        private float _negX;

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
            _posX = _position.x + playerWidth;
            _negX = _position.x - playerWidth;
            _posZ = _position.z + playerWidth;
            _negZ = _position.z - playerWidth;

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

            if (Input.GetButton("Jump"))
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
                (velocity.z < 0 && BackMovement))
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
                (_world.IsVoxelSolid(_negX, downLocation, _negZ) &&
                 (!LeftMovement && !BackMovement)) ||
                (_world.IsVoxelSolid(_posX, downLocation, _negZ) &&
                 (!RightMovement && !BackMovement)) ||
                (_world.IsVoxelSolid(_negX, downLocation, _posZ) &&
                 (!LeftMovement && !ForwardMovement)) ||
                (_world.IsVoxelSolid(_posX, downLocation, _posZ) &&
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
                (_world.IsVoxelSolid(_negX, upLocation, _negZ) &&
                 (!LeftMovement && !BackMovement)) ||
                (_world.IsVoxelSolid(_posX, upLocation, _negZ) &&
                 (!RightMovement && !BackMovement)) ||
                (_world.IsVoxelSolid(_negX, upLocation, _posZ) &&
                 (!LeftMovement && !ForwardMovement)) ||
                (_world.IsVoxelSolid(_posX, upLocation, _posZ) &&
                 (!RightMovement && !ForwardMovement)))
            {
                _verticalMomentum = 0;
                return 0;
            }

            return upSpeed;
        }

        private bool ForwardMovement =>
            _world.IsVoxelSolid(_position.x, _position.y, _posZ) ||
            _world.IsVoxelSolid(_position.x, _position.y + 1f, _posZ);

        private bool BackMovement =>
            _world.IsVoxelSolid(_position.x, _position.y, _negZ) ||
            _world.IsVoxelSolid(_position.x, _position.y + 1f, _negZ);

        private bool LeftMovement =>
            _world.IsVoxelSolid(_negX, _position.y, _position.z) ||
            _world.IsVoxelSolid(_negX, _position.y + 1f, _position.z);

        private bool RightMovement =>
            _world.IsVoxelSolid(_posX, _position.y, _position.z) ||
            _world.IsVoxelSolid(_posX, _position.y + 1f, _position.z);
    }
}