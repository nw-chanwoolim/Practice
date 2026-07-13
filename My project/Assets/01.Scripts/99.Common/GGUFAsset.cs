using UnityEngine;

// 유니티 인스펙터가 인지할 수 있는 GGUF 에셋 데이터 타입 정의
public class GGUFAsset : ScriptableObject
{
    // 필요 시 모델의 상대 경로를 저장해둡니다.
    [HideInInspector] public string filePath;
}