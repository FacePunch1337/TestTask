using System.Collections.Generic;
using UnityEngine;

namespace TestTask.Gameplay.Road
{
    public sealed class RoadLooper
    {
        private readonly List<Transform> _roadSegments;
        private readonly List<Transform> _sideSegments;
        private readonly float _segmentLength;

        public RoadLooper(List<Transform> roadSegments, List<Transform> sideSegments, float segmentLength)
        {
            _roadSegments = roadSegments;
            _sideSegments = sideSegments;
            _segmentLength = segmentLength;
        }

        public void ResetRoad()
        {
            for (var i = 0; i < _roadSegments.Count; i++)
                _roadSegments[i].position = Vector3.forward * (_segmentLength * i);

            for (var i = 0; i < _sideSegments.Count; i++)
            {
                var segmentIndex = i / 2;
                var side = i % 2 == 0 ? -1f : 1f;
                var pos = _sideSegments[i].position;
                pos.x = Mathf.Abs(pos.x) * side;
                pos.z = segmentIndex * _segmentLength;
                _sideSegments[i].position = pos;
            }
        }

        public void Tick(float vehicleZ)
        {
            for (var i = 0; i < _roadSegments.Count; i++)
            {
                if (_roadSegments[i].position.z + _segmentLength < vehicleZ - _segmentLength)
                    _roadSegments[i].position += Vector3.forward * (_segmentLength * _roadSegments.Count);
            }

            for (var i = 0; i < _sideSegments.Count; i++)
            {
                if (_sideSegments[i].position.z + _segmentLength < vehicleZ - _segmentLength)
                    _sideSegments[i].position += Vector3.forward * (_segmentLength * (_sideSegments.Count / 2));
            }
        }
    }
}
