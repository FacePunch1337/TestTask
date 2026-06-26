using System.Collections.Generic;
using TestTask.Config;
using TestTask.Gameplay.Camera;
using TestTask.Gameplay.Road;
using TestTask.Gameplay.Vehicle;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestTask.Utils
{
    public sealed class SceneFactory
    {
        private readonly GameConfigAsset _config;

        public SceneFactory(GameConfigAsset config) => _config = config;

        public VehicleController CreateVehicle()
        {
            GameObject root;
            if (_config.Prefabs.VehiclePrefab != null)
            {
                root = Object.Instantiate(_config.Prefabs.VehiclePrefab);
                root.name = "Vehicle";
            }
            else
            {
                root = new GameObject("Vehicle");
                var model = CreateModelOrBox("Assets/A/car.fbx", "CarModel", new Color(0.9f, 0.82f, 0.35f), new Vector3(1.65f, 0.55f, 3f));
                model.SetParent(root.transform, false);
            }

            var controller = root.GetComponent<VehicleController>();
            if (controller == null)
                controller = root.AddComponent<VehicleController>();

            controller.Construct(_config);
            return controller;
        }

        public Transform CreateTurretTransform(Transform parent)
        {
            var root = _config.Prefabs.TurretPrefab != null
                ? Object.Instantiate(_config.Prefabs.TurretPrefab).transform
                : new GameObject("Turret").transform;

            root.name = "Turret";
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 1f, 0.55f);

            if (_config.Prefabs.TurretPrefab == null)
            {
                var model = CreateModelOrBox("Assets/A/turret.fbx", "TurretModel", new Color(1f, 0.78f, 0.18f), new Vector3(0.8f, 0.4f, 1.1f));
                model.SetParent(root, false);

                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                barrel.name = "Barrel";
                barrel.SetParent(root, false);
                barrel.localScale = new Vector3(0.08f, 0.72f, 0.08f);
                barrel.localPosition = new Vector3(0f, 0.05f, 0.88f);
                barrel.localRotation = Quaternion.Euler(90f, 0f, 0f);
                barrel.GetComponent<Renderer>().material = MakeMaterial("Barrel_Mat", new Color(1f, 0.62f, 0.08f));
            }

            return root;
        }

        public RoadLooper CreateRoad()
        {
            var segmentLength = Mathf.Max(1f, _config.RoadSegmentLength);
            var segmentCount = Mathf.Max(3, _config.RoadSegmentCount);
            var road = new List<Transform>();
            var sides = new List<Transform>();

            for (var i = 0; i < segmentCount; i++)
            {
                var roadSegment = _config.Prefabs.RoadSegmentPrefab != null
                    ? Object.Instantiate(_config.Prefabs.RoadSegmentPrefab).transform
                    : CreateModelOrBox("Assets/A/ground.fbx", $"RoadSegment_{i:00}", new Color(0.22f, 0.25f, 0.28f), new Vector3(_config.RoadWidth, 0.08f, segmentLength));

                roadSegment.name = $"RoadSegment_{i:00}";
                roadSegment.position = Vector3.forward * (segmentLength * i);
                road.Add(roadSegment);

                for (var side = -1; side <= 1; side += 2)
                {
                    var bank = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    bank.name = side < 0 ? $"LeftBank_{i:00}" : $"RightBank_{i:00}";
                    bank.position = new Vector3(side * _config.RoadWidth * 0.72f, -0.04f, segmentLength * i);
                    bank.localScale = new Vector3(2.2f, 0.05f, segmentLength);
                    bank.GetComponent<Renderer>().material = _config.Prefabs.RoadSideMaterial != null
                        ? _config.Prefabs.RoadSideMaterial
                        : MakeMaterial("RoadSide_Mat", new Color(0.58f, 0.55f, 0.48f));
                    sides.Add(bank);
                }
            }

            return new RoadLooper(road, sides, segmentLength);
        }

        public CameraFollow CreateCamera()
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                camera = cameraGo.AddComponent<UnityEngine.Camera>();
            }

            return new CameraFollow(_config, camera);
        }

        public static Transform CreateEnemyVisual(string name, Color color)
        {
            var visual = CreateModelOrCapsule("Assets/A/stickman.fbx", name, color);
            visual.localScale = Vector3.one * 0.95f;
            return visual;
        }

        public static Transform CreateModelOrCapsule(string assetPath, string name, Color fallbackColor)
        {
            var model = TryLoadModel(assetPath, name);
            if (model != null)
                return model;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            capsule.name = name;
            capsule.GetComponent<Renderer>().material = MakeMaterial($"{name}_Mat", fallbackColor);
            return capsule;
        }

        private static Transform CreateModelOrBox(string assetPath, string name, Color fallbackColor, Vector3 fallbackScale)
        {
            var model = TryLoadModel(assetPath, name);
            if (model != null)
                return model;

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            box.name = name;
            box.localScale = fallbackScale;
            box.GetComponent<Renderer>().material = MakeMaterial($"{name}_Mat", fallbackColor);
            return box;
        }

        private static Transform TryLoadModel(string assetPath, string name)
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            return instance.transform;
#else
            return null;
#endif
        }

        public static Material MakeMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader) { name = name, color = color };
            return material;
        }
    }
}
