using System.Collections.Generic;
using System.IO;
using TestTask.Config;
using TestTask.Gameplay.Presentation;
using UnityEditor;
using UnityEngine;

namespace TestTask.Editor
{
    public static class EnemyAnimationBakeTool
    {
        private const string OutputFolder = "Assets/_Project/Animations/BakedEnemies";
        private const float BakeFps = 30f;

        [MenuItem("Tools/TestTask/Bake Enemy Humanoid Clips")]
        public static void BakeEnemyHumanoidClips()
        {
            var config = Selection.activeObject as GameConfigAsset;
            if (config == null)
                config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>("Assets/_Project/Config/GameConfig.asset");

            if (config == null)
            {
                Debug.LogError("GameConfigAsset was not found. Select it or create prototype assets first.");
                return;
            }

            EnsureFolder(OutputFolder);

            for (var i = 0; i < config.EnemyTypes.Length; i++)
            {
                var enemyType = config.EnemyTypes[i];
                var prefab = enemyType.PresenterPrefabOverride != null
                    ? enemyType.PresenterPrefabOverride
                    : config.Prefabs.EnemyPresenterPrefab;

                if (prefab == null)
                {
                    Debug.LogWarning($"Enemy type '{enemyType.Id}' has no presenter prefab.");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null)
                    continue;

                try
                {
                    var presenter = instance.GetComponent<EnemyPresenter>();
                    var visual = FindVisual(instance.transform, presenter);
                    if (visual == null)
                    {
                        Debug.LogWarning($"Enemy type '{enemyType.Id}' prefab has no visual child.");
                        continue;
                    }

                    var avatar = FindAvatar(visual);
                    if (avatar == null)
                        avatar = TryGenerateHumanoidAvatar(visual);

                    if (avatar == null)
                    {
                        Debug.LogError($"Enemy type '{enemyType.Id}' visual has no Humanoid Avatar. Open the enemy FBX Rig tab, set Animation Type = Humanoid, Apply, then run bake again.");
                        continue;
                    }

                    var animator = visual.GetComponent<Animator>();
                    if (animator == null)
                        animator = visual.gameObject.AddComponent<Animator>();
                    animator.avatar = avatar;
                    animator.applyRootMotion = false;

                    enemyType.Animations.Idle = BakeClip(visual, enemyType.Animations.Idle, enemyType.Id, "Idle");
                    enemyType.Animations.Walk = BakeClip(visual, enemyType.Animations.Walk, enemyType.Id, "Walk");
                    enemyType.Animations.Run = BakeClip(visual, enemyType.Animations.Run, enemyType.Id, "Run");
                    enemyType.Animations.Attack = BakeClip(visual, enemyType.Animations.Attack, enemyType.Id, "Attack");
                    enemyType.Animations.Hit = BakeClip(visual, enemyType.Animations.Hit, enemyType.Id, "Hit");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = config;
            Debug.Log("Enemy animation bake finished. Runtime clips are now transform clips and do not need Animator.");
        }

        private static AnimationClip BakeClip(Transform visual, AnimationClip source, string enemyId, string stateName)
        {
            if (source == null)
            {
                Debug.LogWarning($"Enemy '{enemyId}' has no {stateName} clip assigned.");
                return null;
            }

            if (!HasHumanoidMuscleCurves(source))
            {
                Debug.Log($"Enemy '{enemyId}' {stateName} clip '{source.name}' already has transform curves. Keeping it as is.");
                return source;
            }

            var transforms = visual.GetComponentsInChildren<Transform>(true);
            var bindPositions = new Vector3[transforms.Length];
            var bindRotations = new Quaternion[transforms.Length];
            var bindScales = new Vector3[transforms.Length];
            var curves = new List<BakedTransformCurves>(transforms.Length);

            for (var i = 0; i < transforms.Length; i++)
            {
                bindPositions[i] = transforms[i].localPosition;
                bindRotations[i] = transforms[i].localRotation;
                bindScales[i] = transforms[i].localScale;

                if (transforms[i] == visual)
                    continue;

                curves.Add(new BakedTransformCurves
                {
                    Transform = transforms[i],
                    Path = AnimationUtility.CalculateTransformPath(transforms[i], visual)
                });
            }

            var frameCount = Mathf.Max(2, Mathf.CeilToInt(source.length * BakeFps) + 1);
            AnimationMode.StartAnimationMode();
            try
            {
                for (var frame = 0; frame < frameCount; frame++)
                {
                    var time = Mathf.Min(source.length, frame / BakeFps);
                    RestoreBindPose(transforms, bindPositions, bindRotations, bindScales);

                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(visual.gameObject, source, time);
                    AnimationMode.EndSampling();

                    for (var i = 0; i < curves.Count; i++)
                        curves[i].AddKey(time);
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
                RestoreBindPose(transforms, bindPositions, bindRotations, bindScales);
            }

            var baked = new AnimationClip
            {
                name = $"{enemyId}_{stateName}_Baked",
                frameRate = BakeFps,
                legacy = false
            };

            for (var i = 0; i < curves.Count; i++)
                curves[i].ApplyTo(baked);

            var path = $"{OutputFolder}/{baked.name}.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(baked, path);
            Debug.Log($"Baked '{source.name}' -> '{path}'.");
            return baked;
        }

        private static bool HasHumanoidMuscleCurves(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            for (var i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].type == typeof(Animator))
                    return true;
            }

            return false;
        }

        private static Transform FindVisual(Transform root, EnemyPresenter presenter)
        {
            var serialized = new SerializedObject(presenter);
            var visualProperty = serialized.FindProperty("visual");
            if (visualProperty?.objectReferenceValue is Transform assigned)
                return assigned;

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.GetComponent<ParticleSystem>() == null)
                    return child;
            }

            return null;
        }

        private static Avatar FindAvatar(Transform visual)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
            var path = source != null ? AssetDatabase.GetAssetPath(source) : AssetDatabase.GetAssetPath(visual.gameObject);
            if (string.IsNullOrEmpty(path))
                path = FindModelAssetPathFromRenderer(visual);

            return FindAvatarAtPath(path);
        }

        private static Avatar TryGenerateHumanoidAvatar(Transform visual)
        {
            var path = FindModelAssetPathFromRenderer(visual);
            if (string.IsNullOrEmpty(path))
                return null;

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                return null;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                Debug.Log($"Reimported '{path}' as Humanoid to generate Avatar for enemy clip baking.");
            }

            return FindAvatarAtPath(path);
        }

        private static Avatar FindAvatarAtPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                    return avatar;
            }

            return null;
        }

        private static string FindModelAssetPathFromRenderer(Transform visual)
        {
            var skinnedMeshRenderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                var path = AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            var meshFilter = visual.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var path = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            return null;
        }

        private static void RestoreBindPose(Transform[] transforms, Vector3[] positions, Quaternion[] rotations, Vector3[] scales)
        {
            for (var i = 0; i < transforms.Length; i++)
            {
                transforms[i].localPosition = positions[i];
                transforms[i].localRotation = rotations[i];
                transforms[i].localScale = scales[i];
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private sealed class BakedTransformCurves
        {
            public Transform Transform;
            public string Path;

            private readonly AnimationCurve _posX = new();
            private readonly AnimationCurve _posY = new();
            private readonly AnimationCurve _posZ = new();
            private readonly AnimationCurve _rotX = new();
            private readonly AnimationCurve _rotY = new();
            private readonly AnimationCurve _rotZ = new();
            private readonly AnimationCurve _rotW = new();
            private readonly AnimationCurve _scaleX = new();
            private readonly AnimationCurve _scaleY = new();
            private readonly AnimationCurve _scaleZ = new();

            public void AddKey(float time)
            {
                var position = Transform.localPosition;
                var rotation = Transform.localRotation;
                var scale = Transform.localScale;

                _posX.AddKey(time, position.x);
                _posY.AddKey(time, position.y);
                _posZ.AddKey(time, position.z);
                _rotX.AddKey(time, rotation.x);
                _rotY.AddKey(time, rotation.y);
                _rotZ.AddKey(time, rotation.z);
                _rotW.AddKey(time, rotation.w);
                _scaleX.AddKey(time, scale.x);
                _scaleY.AddKey(time, scale.y);
                _scaleZ.AddKey(time, scale.z);
            }

            public void ApplyTo(AnimationClip clip)
            {
                clip.SetCurve(Path, typeof(Transform), "m_LocalPosition.x", _posX);
                clip.SetCurve(Path, typeof(Transform), "m_LocalPosition.y", _posY);
                clip.SetCurve(Path, typeof(Transform), "m_LocalPosition.z", _posZ);
                clip.SetCurve(Path, typeof(Transform), "m_LocalRotation.x", _rotX);
                clip.SetCurve(Path, typeof(Transform), "m_LocalRotation.y", _rotY);
                clip.SetCurve(Path, typeof(Transform), "m_LocalRotation.z", _rotZ);
                clip.SetCurve(Path, typeof(Transform), "m_LocalRotation.w", _rotW);
                clip.SetCurve(Path, typeof(Transform), "m_LocalScale.x", _scaleX);
                clip.SetCurve(Path, typeof(Transform), "m_LocalScale.y", _scaleY);
                clip.SetCurve(Path, typeof(Transform), "m_LocalScale.z", _scaleZ);
            }
        }
    }
}
