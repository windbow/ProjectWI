using ProjectWI.Battle;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.EditorTools
{
    public static class WIBattleEffectsSetup
    {
        // 실제 테스트 전투 컨트롤러를 고정 간격으로 진행하여 효과가 보이는 순간을 캡처합니다.
        [MenuItem("WI/Effects/Capture Test Battle Effects")]
        public static void CaptureTestBattle()
        {
            var controller = Object.FindFirstObjectByType<WIBattleRuntimeController>();
            var service = ProjectWI.Systems.WICampaignRuntimeService.Instance;
            if (Application.isPlaying == false || service == null || service.IsTestBattle == false || controller == null || controller.Runtime == null)
            {
                Debug.LogWarning("초기화된 전투 테스트 랩에서 실행하세요.");
                return;
            }
            EditorApplication.isPaused = true;
            controller.StartBattle();
            controller.IsPaused = false;
            string[] names = { "Slash", "Arrow", "Magic", "Hit" };
            var captured = new System.Collections.Generic.HashSet<string>();
            var report = new System.Collections.Generic.List<string>();
            RenderTexture previous = RenderTexture.active;
            Camera camera = Camera.main;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture target = new RenderTexture(1600, 900, 24);
            Texture2D image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                for (int step = 0; step < 900 && controller.Runtime.Finished == false && captured.Count < 4; step++)
                {
                    controller.SimulateStep(1f / 30f);
                    ParticleSystem[] particles = controller.GetComponentsInChildren<ParticleSystem>();
                    foreach (string name in names)
                    {
                        if (captured.Contains(name) == true)
                        {
                            continue;
                        }
                        bool visible = false;
                        foreach (ParticleSystem particle in particles)
                        {
                            if (particle.particleCount > 0 && particle.transform.parent != null && particle.transform.parent.name.StartsWith("WI_Temp_" + name) == true)
                            {
                                visible = true;
                                break;
                            }
                        }
                        if (visible == false)
                        {
                            continue;
                        }
                        camera.Render();
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                        image.Apply();
                        System.IO.File.WriteAllBytes("GameDocuments/DesignAuditEvidence/BattleEffects-" + name + ".png", image.EncodeToPNG());
                        captured.Add(name);
                        report.Add(name + " 실제 전투 표시: " + controller.Runtime.ElapsedSeconds.ToString("F2") + "초");
                    }
                }
                report.Add("확인 종류: " + captured.Count + "/4. 테스트 전투 고정 간격 진행, 카메라 직접 렌더링. UI 클릭 검증 제외.");
                System.IO.File.WriteAllText("GameDocuments/DesignAuditEvidence/BattleEffects-Integration.txt", string.Join("\r\n", report) + "\r\n", new System.Text.UTF8Encoding(false));
                Debug.Log(string.Join("\n", report));
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        // 테스트 전투에서만 비활성 창의 플레이 루프를 재개하여 실제 화면 검증을 지원합니다.
        [MenuItem("WI/Effects/Resume Test Battle For Verification")]
        public static void ResumeTestBattle()
        {
            if (Application.isPlaying == false || ProjectWI.Systems.WICampaignRuntimeService.Instance == null ||
                ProjectWI.Systems.WICampaignRuntimeService.Instance.IsTestBattle == false)
            {
                Debug.LogWarning("전투 테스트 랩 실행 중에만 사용할 수 있습니다.");
                return;
            }
            Application.runInBackground = true;
            WIBattleRuntimeController controller = Object.FindFirstObjectByType<WIBattleRuntimeController>();
            if (controller != null && controller.Runtime != null)
            {
                controller.StartBattle();
                controller.IsPaused = false;
            }
            EditorApplication.isPaused = false;
            EditorApplication.QueuePlayerLoopUpdate();
            Debug.Log("테스트 전투의 백그라운드 실행을 활성화했습니다. 프로젝트 설정 에셋은 저장하지 않습니다.");
        }

        // 제작된 네 프리팹을 전투 설정에 연결하고 기존 밸런스 값은 유지합니다.
        [MenuItem("WI/Effects/Connect Temporary Battle Effects")]
        public static void Connect()
        {
            var config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            var serialized = new SerializedObject(config);
            string[] fields = { "slashEffectPrefab", "arrowEffectPrefab", "magicEffectPrefab", "hitEffectPrefab" };
            string[] names = { "Slash", "Arrow", "Magic", "Hit" };
            for (int i = 0; i < fields.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Battle/Effects/Temporary/WI_Temp_" + names[i] + ".prefab");
                if (prefab == null || prefab.GetComponent<ParticleSystem>() == null)
                {
                    throw new System.InvalidOperationException("전투 이펙트 프리팹 누락: " + names[i]);
                }
                serialized.FindProperty(fields[i]).objectReferenceValue = prefab;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("검격·화살·마법·피격 이펙트를 BattleConfig에 연결했습니다.");
        }
    }
}
