using UnityEngine;

namespace CompanyNine.Voxel.Controller
{
    public class FlyCamera : MonoBehaviour
    {
        /**
            EXTENDED FLYCAM
            Desi Quintans (CowfaceGames.com), 17 August 2012.
            Based on FlyThrough.js by Slin (http://wiki.unity3d.com/index.php/FlyThrough), 17 May 2011.

            LICENSE
            Free as in speech, and free as in beer.

            FEATURES
            WASD/Arrows: Movement
            Q: Climb
            E: Drop
            Shift: Move faster
            Alt: Move slower
            End: Toggle cursor locking to screen (you can also press Ctrl+P to toggle play mode on and off).
        */
        public float cameraSensitivity = 520;

        public float climbSpeed = 10;
        public float normalMoveSpeed = 10;
        public float slowMoveFactor = 0.25f;
        public float fastMoveFactor = 3;

        private float _rotationX;
        private float _rotationY;

        private Transform Transform => transform;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            _rotationX += Input.GetAxis("Mouse X") * cameraSensitivity *
                          Time.deltaTime;
            _rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity *
                          Time.deltaTime;
            _rotationY = Mathf.Clamp(_rotationY, -90, 90);


            var rotation = Quaternion.AngleAxis(_rotationX, Vector3.up);
            rotation *= Quaternion.AngleAxis(_rotationY, Vector3.left);

            Transform.rotation = rotation;

            var position = Transform.position;
            if (Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift))
            {
                position += transform.forward * (normalMoveSpeed *
                                                 fastMoveFactor *
                                                 Input.GetAxis("Vertical") *
                                                 Time.deltaTime);
                position += transform.right * (normalMoveSpeed *
                                               fastMoveFactor *
                                               Input.GetAxis("Horizontal") *
                                               Time.deltaTime);
            }
            else if (Input.GetKey(KeyCode.LeftAlt) ||
                     Input.GetKey(KeyCode.RightAlt))
            {
                position += transform.forward * (normalMoveSpeed *
                                                 slowMoveFactor *
                                                 Input.GetAxis("Vertical") *
                                                 Time.deltaTime);
                position += transform.right * (normalMoveSpeed *
                                               slowMoveFactor *
                                               Input.GetAxis("Horizontal") *
                                               Time.deltaTime);
            }
            else
            {
                position += transform.forward * (normalMoveSpeed *
                                                 Input.GetAxis("Vertical") *
                                                 Time.deltaTime);
                position += transform.right * (normalMoveSpeed *
                                               Input.GetAxis("Horizontal") *
                                               Time.deltaTime);
            }


            if (Input.GetKey(KeyCode.Q))
            {
                position += transform.up * (climbSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.E))
            {
                position -= transform.up * (climbSpeed * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.End))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
            }

            Transform.position = position;
        }
    }
}