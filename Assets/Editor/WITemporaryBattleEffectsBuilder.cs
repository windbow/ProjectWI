using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectWI.EditorTools
{
    // 전투 연결 없이 임시 이펙트 에셋과 검토용 이미지만 제작하는 편집기 도구입니다.
    public static class WITemporaryBattleEffectsBuilder
    {
        // 신규 에셋 전용 경로와 프리팹 목록입니다.
        private const string ArtPath = "Assets/Art/Battle/Effects/Temporary";
        private const string PrefabPath = "Assets/Prefabs/Battle/Effects/Temporary";
        private static readonly string[] Names = { "WI_Temp_Slash", "WI_Temp_Arrow", "WI_Temp_Magic", "WI_Temp_Hit" };

        // 기존 에셋은 덮어쓰지 않고 네 종류의 독립 이펙트 프리팹을 만듭니다.
        [MenuItem("WI/Effects/Create Temporary Battle Prefabs")]
        public static void Create()
        {
            foreach (string name in Names)
            {
                if (File.Exists(PrefabPath + "/" + name + ".prefab") == true)
                {
                    throw new InvalidOperationException("기존 임시 이펙트가 있어 제작을 중단합니다: " + name);
                }
            }
            Shader shader = Shader.Find("ProjectWI/Temporary Effect");
            if (shader == null)
            {
                throw new InvalidOperationException("ProjectWI/Temporary Effect 셰이더를 찾을 수 없습니다.");
            }
            Directory.CreateDirectory(ArtPath);
            Directory.CreateDirectory(PrefabPath);
            AssetDatabase.Refresh();
            Material material = new Material(shader) { name = "WI_TempEffect_Unlit" };
            AssetDatabase.CreateAsset(material, ArtPath + "/WI_TempEffect_Unlit.mat");
            Mesh slash = CreateArc("WI_TempSlashMesh", false);
            Mesh ring = CreateArc("WI_TempRingMesh", true);
            Mesh arrow = CreateArrow();
            Mesh spark = CreatePolygon("WI_TempSparkMesh", new[] {
                new Vector2(-0.5f, 0), new Vector2(0, -0.12f), new Vector2(0.5f, 0), new Vector2(0, 0.12f) });
            var points = new Vector2[32];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / points.Length;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.5f;
            }
            Mesh orb = CreatePolygon("WI_TempOrbMesh", points);
            for (int i = 0; i < Names.Length; i++)
            {
                GameObject root = new GameObject(Names[i]);
                try
                {
                    ParticleSystem clock = root.AddComponent<ParticleSystem>();
                    clock.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = clock.main;
                    main.duration = 0.9f;
                    main.loop = false;
                    main.playOnAwake = true;
                    main.startLifetime = 0.01f;
                    main.stopAction = ParticleSystemStopAction.None;
                    var emission = clock.emission;
                    emission.enabled = false;
                    root.GetComponent<ParticleSystemRenderer>().enabled = false;
                    if (i == 0)
                    {
                        Add(root, "SlashOuter", slash, material, new Color(0.30f, 0.73f, 1f, 0.5f), 0.28f, 1.2f, Vector2.zero, Vector2.zero, 0f, 1f, 1.25f);
                        Add(root, "SlashCore", slash, material, new Color(0.9f, 0.97f, 1f), 0.21f, 1f, Vector2.zero, Vector2.zero, 0f, 0.8f, 1.2f);
                        Add(root, "SlashEcho", slash, material, new Color(0.45f, 0.8f, 1f, 0.35f), 0.23f, 0.8f, new Vector2(-0.12f, 0), Vector2.zero, 0.055f, 0.9f, 1.2f);
                    }
                    else if (i == 1)
                    {
                        Add(root, "Arrow", arrow, material, new Color(0.9f, 0.95f, 1f), 0.65f, 0.8f, Vector2.zero, new Vector2(3f, 0));
                        Add(root, "ArrowWake", spark, material, new Color(0.55f, 0.75f, 1f, 0.5f), 0.65f, 0.85f, new Vector2(-0.6f, 0), new Vector2(3f, 0));
                        Add(root, "ArrowEcho", arrow, material, new Color(0.55f, 0.75f, 1f, 0.18f), 0.65f, 0.8f, new Vector2(-0.3f, 0), new Vector2(3f, 0));
                    }
                    else if (i == 2)
                    {
                        Add(root, "MagicHalo", orb, material, new Color(0.45f, 0.3f, 1f, 0.3f), 0.65f, 0.7f, Vector2.zero, new Vector2(2.5f, 0));
                        Add(root, "MagicCore", orb, material, new Color(0.82f, 0.92f, 1f), 0.65f, 0.27f, Vector2.zero, new Vector2(2.5f, 0));
                        Add(root, "MagicRing", ring, material, new Color(0.55f, 0.55f, 1f, 0.8f), 0.65f, 0.45f, Vector2.zero, new Vector2(2.5f, 0), 0f, 0.8f, 1.4f);
                        Add(root, "MagicWake", spark, material, new Color(0.55f, 0.4f, 1f, 0.6f), 0.65f, 1.2f, new Vector2(-0.5f, 0), new Vector2(2.5f, 0));
                    }
                    else
                    {
                        Add(root, "HitFlash", orb, material, Color.white, 0.12f, 0.4f, Vector2.zero, Vector2.zero, 0f, 1f, 0f);
                        Add(root, "HitRing", ring, material, new Color(0.65f, 0.85f, 1f, 0.8f), 0.32f, 0.7f, Vector2.zero, Vector2.zero, 0f, 0.1f, 1.4f);
                        for (int j = 0; j < 8; j++)
                        {
                            float angle = j * Mathf.PI / 4f;
                            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                            ParticleSystem particle = Add(root, "HitSpark" + j, spark, material, new Color(0.88f, 0.95f, 1f), 0.27f, 0.3f, direction * 0.08f, direction * 2.3f, 0f, 1f, 0.1f);
                            var sparkMain = particle.main;
                            sparkMain.startRotation = -angle;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath + "/" + Names[i] + ".prefab");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("임시 전투 이펙트 프리팹 4종 제작 완료. 게임 연결 없음: " + PrefabPath);
        }

        // 메시 한 개를 단발 재생하는 자식 파티클을 편집 시점에 구성합니다.
        private static ParticleSystem Add(GameObject root, string name, Mesh mesh, Material material, Color color,
            float lifetime, float size, Vector2 offset, Vector2 velocity, float delay = 0f, float startScale = 1f, float endScale = 1f)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = offset;
            ParticleSystem particle = child.AddComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.useAutoRandomSeed = false;
            particle.randomSeed = 17;
            var main = particle.main;
            main.duration = 0.7f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = lifetime;
            main.startDelay = delay;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var emission = particle.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var shape = particle.shape;
            shape.enabled = false;
            var movement = particle.velocityOverLifetime;
            movement.enabled = true;
            movement.space = ParticleSystemSimulationSpace.Local;
            movement.x = velocity.x;
            movement.y = velocity.y;
            movement.z = 0f;
            var fade = particle.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var scale = particle.sizeOverLifetime;
            scale.enabled = true;
            scale.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, startScale, 1f, endScale));
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.enableGPUInstancing = false;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color });
            renderer.alignment = ParticleSystemRenderSpace.Local;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 3000 + root.transform.childCount;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particle;
        }

        // 검격용 가늘어지는 원호 또는 피격용 원형 띠 메시를 생성합니다.
        private static Mesh CreateArc(string name, bool fullRing)
        {
            const int segments = 64;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = fullRing == true ? t * Mathf.PI * 2f : Mathf.Lerp(-1.2f, 1.2f, t);
                float width = fullRing == true ? 0.06f : 0.18f * Mathf.Sin(t * Mathf.PI);
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[i * 2] = direction * (0.8f - width);
                vertices[i * 2 + 1] = direction * 0.8f;
                if (i < segments)
                {
                    int p = i * 2;
                    int q = i * 6;
                    triangles[q] = p;
                    triangles[q + 1] = p + 1;
                    triangles[q + 2] = p + 2;
                    triangles[q + 3] = p + 1;
                    triangles[q + 4] = p + 3;
                    triangles[q + 5] = p + 2;
                }
            }
            return SaveMesh(name, vertices, triangles);
        }

        // 화살촉·몸통·깃을 겹치지 않는 삼각형으로 제작합니다.
        private static Mesh CreateArrow()
        {
            return SaveMesh("WI_TempArrowMesh", new[] {
                new Vector3(-0.6f, -0.026f), new Vector3(0.3f, -0.026f),
                new Vector3(0.3f, 0.026f), new Vector3(-0.6f, 0.026f),
                new Vector3(0.23f, -0.12f), new Vector3(0.58f, 0f), new Vector3(0.23f, 0.12f),
                new Vector3(-0.65f, 0.13f), new Vector3(-0.42f, 0.13f),
                new Vector3(-0.28f, 0.026f), new Vector3(-0.65f, -0.13f),
                new Vector3(-0.42f, -0.13f), new Vector3(-0.28f, -0.026f) },
                new[] { 0,1,2, 0,2,3, 4,5,6, 3,7,8, 3,8,9, 0,10,11, 0,11,12 });
        }

        // 파티클 전용 무조명 셰이더와 분리된 화살 메시를 기존 임시 에셋에 적용합니다.
        [MenuItem("WI/Effects/Apply Temporary Effect Visuals")]
        public static void ApplyVisuals()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "/WI_TempEffect_Unlit.mat");
            material.shader = Shader.Find("ProjectWI/Temporary Effect");
            EditorUtility.SetDirty(material);
            CreateArrow();
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { ArtPath }))
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                var colors = new Color32[mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color32(255, 255, 255, 255);
                }
                mesh.colors32 = colors;
                EditorUtility.SetDirty(mesh);
            }
            foreach (string name in Names)
            {
                string path = PrefabPath + "/" + name + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (ParticleSystemRenderer renderer in root.GetComponentsInChildren<ParticleSystemRenderer>())
                    {
                        renderer.enableGPUInstancing = false;
                        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color });
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            AssetDatabase.SaveAssets();
        }

        // 중심을 기준으로 화살·빛·파편용 평면 메시를 만듭니다.
        private static Mesh CreatePolygon(string name, Vector2[] outline)
        {
            var vertices = new Vector3[outline.Length + 1];
            var triangles = new int[outline.Length * 3];
            for (int i = 0; i < outline.Length; i++)
            {
                vertices[i + 1] = outline[i];
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % outline.Length + 1;
            }
            return SaveMesh(name, vertices, triangles);
        }

        // 셰이더가 사용할 흰색 정점과 UV를 포함해 메시를 프로젝트 에셋으로 저장합니다.
        private static Mesh SaveMesh(string name, Vector3[] vertices, int[] triangles)
        {
            var colors = new Color32[vertices.Length];
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                colors[i] = Color.white;
                uv[i] = new Vector2(0.5f, 0.5f);
            }
            Mesh mesh = new Mesh { name = name, vertices = vertices, triangles = triangles, colors32 = colors, uv = uv };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string path = ArtPath + "/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // 편집기 Simulate의 일시정지 상태 대신 실제 표시 중인 파티클 수를 집계합니다.
        private static int CountParticles(GameObject root)
        {
            int count = 0;
            foreach (ParticleSystem particle in root.GetComponentsInChildren<ParticleSystem>())
            {
                count += particle.particleCount;
            }
            return count;
        }

        // 저장된 프리팹의 참조·재생·종료를 검사하고 독립 미리보기 씬에서 시간별 이미지를 렌더링합니다.
        [MenuItem("WI/Effects/Verify Temporary Battle Prefabs")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = new RenderTexture(360, 280, 24);
            Texture2D sheet = new Texture2D(1440, 840, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            var report = new List<string>();
            try
            {
                GameObject cameraObject = new GameObject("WI_EffectPreviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(0.55f, 0f, -10f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.075f, 0.095f, 0.13f);
                camera.targetTexture = target;
                GameObject lightObject = new GameObject("WI_EffectPreviewLight", typeof(Light));
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                lightObject.GetComponent<Light>().type = LightType.Directional;
                float[] times = { 0.08f, 0.20f, 0.45f };
                for (int i = 0; i < Names.Length; i++)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + "/" + Names[i] + ".prefab");
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    try
                    {
                        ParticleSystem root = instance.GetComponent<ParticleSystem>();
                        foreach (Transform node in instance.GetComponentsInChildren<Transform>(true))
                        {
                            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0)
                            {
                                throw new InvalidOperationException("누락 스크립트: " + node.name);
                            }
                        }
                        foreach (ParticleSystemRenderer renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>())
                        {
                            if (renderer.enabled == true && (renderer.mesh == null || renderer.sharedMaterial == null || renderer.sharedMaterial.shader.isSupported == false))
                            {
                                throw new InvalidOperationException("렌더러 참조 오류: " + renderer.name);
                            }
                        }
                        for (int row = 0; row < times.Length; row++)
                        {
                            root.Simulate(times[row], true, true, false);
                            if (row == 0 && CountParticles(instance) == 0)
                            {
                                throw new InvalidOperationException("파티클 재생 실패: " + Names[i]);
                            }
                            camera.Render();
                            RenderTexture.active = target;
                            sheet.ReadPixels(new Rect(0, 0, 360, 280), i * 360, (2 - row) * 280);
                        }
                        root.Simulate(1.1f, true, true, false);
                        foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>())
                        {
                            if (particle.particleCount != 0 || particle.main.loop == true)
                            {
                                throw new InvalidOperationException("파티클 종료 실패: " + particle.name);
                            }
                        }
                        report.Add(Names[i] + ": 참조/단발 재생/1.1초 종료 확인");
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                }
                sheet.Apply();
                const string evidence = "GameDocuments/DesignAuditEvidence/TemporaryBattleEffects";
                File.WriteAllBytes(evidence + ".png", sheet.EncodeToPNG());
                File.WriteAllText(evidence + ".txt", string.Join("\r\n", report) + "\r\n열: 검격/화살/마법/피격. 행: 0.08/0.20/0.45초. 전투 미연결.\r\n", new System.Text.UTF8Encoding(false));
                Debug.Log(string.Join("\n", report));
            }
            finally
            {
                RenderTexture.active = previous;
                EditorSceneManager.ClosePreviewScene(scene);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }
    }
}
