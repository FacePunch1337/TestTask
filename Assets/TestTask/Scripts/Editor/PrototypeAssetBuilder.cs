using TestTask.Bootstrap;
using TestTask.Config;
using TestTask.Gameplay.Presentation;
using TestTask.Gameplay.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TestTask.Editor
{
    public static class PrototypeAssetBuilder
    {
        private const string ConfigFolder = "Assets/_Project/Config";
        private const string PrefabFolder = "Assets/_Project/Prefabs";

        [MenuItem("Tools/TestTask/Create Prototype Assets")]
        public static void CreatePrototypeAssets()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder(ConfigFolder);
            EnsureFolder(PrefabFolder);

            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>($"{ConfigFolder}/GameConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfigAsset>();
                AssetDatabase.CreateAsset(config, $"{ConfigFolder}/GameConfig.asset");
            }

            config.Prefabs.VehiclePrefab ??= CreateVehiclePrefab();
            config.Prefabs.TurretPrefab ??= CreateModelPrefab("TurretPrefab", "Assets/A/turret.fbx");
            config.Prefabs.RoadSegmentPrefab ??= CreateModelPrefab("RoadSegmentPrefab", "Assets/A/ground.fbx");
            config.Prefabs.EnemyPresenterPrefab ??= CreateEnemyPresenterPrefab();
            config.Prefabs.BulletPresenterPrefab ??= CreatePrimitivePrefab("BulletPresenterPrefab", PrimitiveType.Sphere, Vector3.one * 0.22f, new Color(1f, 0.72f, 0.08f), typeof(BulletPresenter));
            config.Prefabs.CoinPickupPrefab ??= CreatePrimitivePrefab("CoinPickupPrefab", PrimitiveType.Sphere, Vector3.one * 0.32f, new Color(1f, 0.78f, 0.06f), typeof(PickupPresenter));
            config.Prefabs.ExperiencePickupPrefab ??= CreatePrimitivePrefab("ExperiencePickupPrefab", PrimitiveType.Sphere, Vector3.one * 0.25f, new Color(0.18f, 0.8f, 1f), typeof(PickupPresenter));
            config.Prefabs.BulletTrailMaterial ??= CreateMaterial("BulletTrail_Mat", Color.white);
            config.Prefabs.TurretAimLineMaterial ??= CreateMaterial("TurretAimLine_Mat", new Color(1f, 0.9f, 0.08f, 0.7f));
            config.Prefabs.EnemyMaterial ??= CreateMaterial("Enemy_Common_Mat", new Color(1f, 0.08f, 0.04f));
            config.Prefabs.EnemyDamageFlashMaterial ??= CreateMaterial("Enemy_DamageFlash_Mat", Color.white);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = config;
        }

        [MenuItem("Tools/TestTask/Setup Current Scene")]
        public static void SetupCurrentScene()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>($"{ConfigFolder}/GameConfig.asset");
            if (config == null)
            {
                Debug.LogError("GameConfig.asset was not found. Run Tools/TestTask/Create Prototype Assets once, then configure the scene.");
                return;
            }

            var sceneReference = Object.FindFirstObjectByType<GameSceneReference>();
            if (sceneReference == null)
                sceneReference = new GameObject("[SceneReference] Game").AddComponent<GameSceneReference>();

            var serializedReference = new SerializedObject(sceneReference);
            serializedReference.FindProperty("config").objectReferenceValue = config;
            serializedReference.ApplyModifiedPropertiesWithoutUndo();

            var bootstrap = Object.FindFirstObjectByType<GameBootstrapper>();
            if (bootstrap == null)
            {
                var go = new GameObject("[Bootstrap] Game Entry Point");
                bootstrap = go.AddComponent<GameBootstrapper>();
            }

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("config").objectReferenceValue = null;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeObject = sceneReference;
        }

        private static GameObject CreateVehiclePrefab()
        {
            var root = new GameObject("VehiclePrefab");
            root.AddComponent<VehicleController>();
            AddModelChild(root.transform, "Assets/A/car.fbx", "CarModel");
            return SavePrefab(root, "VehiclePrefab");
        }

        private static GameObject CreateEnemyPresenterPrefab()
        {
            var root = new GameObject("EnemyPresenterPrefab");
            root.AddComponent<EnemyPresenter>();
            var particlesGo = new GameObject("EnemyDeathParticles");
            particlesGo.transform.SetParent(root.transform, false);
            var particles = particlesGo.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 5f;
            main.startSize = 0.16f;
            main.maxParticles = 48;
            main.loop = false;
            main.playOnAwake = false;
            var emission = particles.emission;
            emission.enabled = false;

            return SavePrefab(root, "EnemyPresenterPrefab");
        }

        private static GameObject CreateModelPrefab(string name, string assetPath)
        {
            var root = new GameObject(name);
            AddModelChild(root.transform, assetPath, "Model");
            return SavePrefab(root, name);
        }

        private static GameObject CreatePrimitivePrefab(string name, PrimitiveType primitive, Vector3 scale, Color color, System.Type componentType)
        {
            var root = GameObject.CreatePrimitive(primitive);
            root.name = name;
            root.transform.localScale = scale;
            root.GetComponent<Renderer>().sharedMaterial = CreateMaterial($"{name}_Mat", color);
            root.AddComponent(componentType);
            return SavePrefab(root, name);
        }

        private static Transform AddModelChild(Transform parent, string assetPath, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
                return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            return instance.transform;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var path = $"{PrefabFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            var path = $"{PrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(root);
                return existing;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
