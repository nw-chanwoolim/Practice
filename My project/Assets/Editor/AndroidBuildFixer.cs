using UnityEditor.Android;
using System.IO;
using UnityEngine;

public class AndroidBuildFixer : IPostGenerateGradleAndroidProject
{
    // 빌드 후처리 실행 순서
    public int callbackOrder => 0;

    // Gradle 프로젝트 생성 완료 후 호출됨
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log($"[AndroidBuildFixer] Gradle 프로젝트 생성 감지: {path}");

        // 1. unityLibrary/build.gradle 수정
        string unityLibraryGradle = Path.Combine(path, "build.gradle");
        ApplyKotlinFix(unityLibraryGradle);

        // 2. 루트 build.gradle 및 launcher/build.gradle 수정
        string rootDir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(rootDir))
        {
            // 루트 build.gradle
            string rootGradle = Path.Combine(rootDir, "build.gradle");
            if (File.Exists(rootGradle))
            {
                ApplyKotlinFix(rootGradle);
            }
            
            // launcher/build.gradle (실제 Duplicate 에러가 발생하는 타겟)
            string launcherGradle = Path.Combine(rootDir, "launcher", "build.gradle");
            if (File.Exists(launcherGradle))
            {
                ApplyKotlinFix(launcherGradle);
            }
        }
    }

    // Gradle 파일 맨 뒤에 코틀린 중복 해결 코드 주입
    private void ApplyKotlinFix(string gradlePath)
    {
        if (!File.Exists(gradlePath)) return;

        string content = File.ReadAllText(gradlePath);
        if (!content.Contains("kotlin-stdlib:1.8.22"))
        {
            string fixCode = @"

// 중복 코틀린 클래스 충돌 해결을 위한 버전 강제 설정
configurations.all {
    resolutionStrategy {
        force 'org.jetbrains.kotlin:kotlin-stdlib:1.8.22'
        force 'org.jetbrains.kotlin:kotlin-stdlib-jdk7:1.8.22'
        force 'org.jetbrains.kotlin:kotlin-stdlib-jdk8:1.8.22'
    }
}
";
            content += fixCode;
            File.WriteAllText(gradlePath, content);
            Debug.Log($"[AndroidBuildFixer] 코틀린 해결 코드가 주입되었습니다: {gradlePath}");
        }
    }
}
