using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectWI.EditorTools
{
    public static class WIWindowsPackageBuilder
    {
        private const string MenuPath = "ProjectWI/Build/Package Windows Test Build";
        private const string BuildDirectory = "Builds/Windows";
        private const string PackageDirectory = "Builds/Packages";
        private const string ExecutableName = "ProjectWI.exe";

        // 현재 등록된 씬으로 Windows 개발 빌드를 만들고 배포용 ZIP 파일로 묶습니다.
        [MenuItem(MenuPath)]
        public static void PackageWindowsTestBuild()
        {
            if (BuildPipeline.isBuildingPlayer == true)
            {
                EditorUtility.DisplayDialog("ProjectWI 패키징", "이미 Player 빌드가 진행 중입니다.", "확인");
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildPath = ResolveProjectPath(projectRoot, BuildDirectory);
            string packagePath = ResolveProjectPath(projectRoot, PackageDirectory);
            string executablePath = Path.Combine(buildPath, ExecutableName);
            string[] scenes = GetEnabledScenes();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("ProjectWI 패키징", "Build Settings에 활성화된 씬이 없습니다.", "확인");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("ProjectWI 패키징", "기존 Windows 빌드를 정리하고 있습니다.", 0.1f);
                RecreateDirectory(projectRoot, buildPath);
                Directory.CreateDirectory(packagePath);

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                {
                    bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Standalone,
                        BuildTarget.StandaloneWindows64);
                    if (switched == false)
                    {
                        throw new InvalidOperationException("Windows 64비트 빌드 대상으로 전환하지 못했습니다.");
                    }
                }

                EditorUtility.DisplayProgressBar("ProjectWI 패키징", "Windows 개발 빌드를 생성하고 있습니다.", 0.35f);
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = executablePath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development | BuildOptions.CompressWithLz4
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"빌드 실패: {report.summary.result} · 오류 {report.summary.totalErrors}건");
                }

                EditorUtility.DisplayProgressBar("ProjectWI 패키징", "빌드 폴더를 ZIP 파일로 압축하고 있습니다.", 0.85f);
                string zipPath = CreatePackageZip(packagePath, buildPath);
                AssetDatabase.Refresh();

                string message =
                    $"Windows 테스트 패키징이 완료되었습니다.\n\n" +
                    $"빌드: {MakeRelativePath(projectRoot, executablePath)}\n" +
                    $"패키지: {MakeRelativePath(projectRoot, zipPath)}\n" +
                    $"크기: {report.summary.totalSize / (1024f * 1024f):F1} MB\n" +
                    $"시간: {report.summary.totalTime.TotalSeconds:F1}초";

                Debug.Log($"[WI_PACKAGE_SUCCESS] {message.Replace(Environment.NewLine, " | ")}");
                EditorUtility.DisplayDialog("ProjectWI 패키징 완료", message, "확인");
                EditorUtility.RevealInFinder(zipPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[WI_PACKAGE_FAIL] {exception}");
                EditorUtility.DisplayDialog("ProjectWI 패키징 실패", exception.Message, "확인");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // Build Settings에서 활성화된 실제 씬 경로만 순서대로 반환합니다.
        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled == true && File.Exists(scene.path) == true)
                .Select(scene => scene.path)
                .ToArray();
        }

        // 프로젝트 내부인지 검증한 뒤 기존 출력 폴더를 지우고 빈 폴더로 다시 만듭니다.
        private static void RecreateDirectory(string projectRoot, string targetPath)
        {
            EnsureInsideProject(projectRoot, targetPath);
            if (Directory.Exists(targetPath) == true)
            {
                Directory.Delete(targetPath, true);
            }

            Directory.CreateDirectory(targetPath);
        }

        // 현재 빌드 폴더 전체를 날짜가 포함된 단일 ZIP 패키지로 생성합니다.
        private static string CreatePackageZip(string packageDirectory, string buildPath)
        {
            string safeVersion = string.Join("_", PlayerSettings.bundleVersion.Split(Path.GetInvalidFileNameChars()));
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string zipPath = Path.Combine(packageDirectory, $"ProjectWI-Windows-v{safeVersion}-{timestamp}.zip");

            if (File.Exists(zipPath) == true)
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(
                buildPath,
                zipPath,
                System.IO.Compression.CompressionLevel.Optimal,
                true);
            return zipPath;
        }

        // 프로젝트 루트 기준 상대 경로를 절대 경로로 변환하고 범위를 검증합니다.
        private static string ResolveProjectPath(string projectRoot, string relativePath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            EnsureInsideProject(projectRoot, fullPath);
            return fullPath;
        }

        // 삭제와 출력 대상이 프로젝트 작업공간 밖으로 벗어나지 않도록 검사합니다.
        private static void EnsureInsideProject(string projectRoot, string targetPath)
        {
            string normalizedRoot = Path.GetFullPath(projectRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalizedTarget = Path.GetFullPath(targetPath);
            if (normalizedTarget.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new InvalidOperationException($"프로젝트 밖의 경로는 사용할 수 없습니다: {normalizedTarget}");
            }
        }

        // 완료 안내에 표시할 프로젝트 기준 상대 경로를 생성합니다.
        private static string MakeRelativePath(string projectRoot, string fullPath)
        {
            return Path.GetRelativePath(projectRoot, fullPath).Replace('\\', '/');
        }
    }
}
