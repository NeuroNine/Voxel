using System;
using UnityEngine;
using UnityEngine.UI;

namespace CompanyNine.Voxel.UI
{
    public class DebugScreen : MonoBehaviour
    {
        private World _world;
        private Text _textComponent;
        private float _timer;
        private float _framerate;

        public void Start()
        {
            _world = GameObject.Find(ObjNames.World.Name)
                .GetComponent<World>();
            _textComponent = GetComponent<Text>();
        }

        private void Update()
        {
            CalculateFrameRate();
            var playerPosition = ComposePlayerPosition();
            var text = "Debug Screen:\n\n" +
                       $"Current Chunk Coordinate: {_world.CurrentPlayerChunk}\n\n" +
                       $"FPS: {_framerate}\n\n" +
                       $"{playerPosition}";

            _textComponent.text = text;
        }

        private void CalculateFrameRate()
        {
            if (_timer > 1f)
            {
                _framerate = (int) (1f / Time.unscaledDeltaTime);
                _timer = 0;
            }
            else
            {
                _timer += Time.deltaTime;
            }
        }

        private string ComposePlayerPosition()
        {
            var position =
                Vector3Int.FloorToInt(_world.player.transform.position);

            return $"x: {position.x}, y: {position.y}, z: {position.z}";
        }
    }
}