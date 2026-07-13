using UnityEngine;

// 유니티 인스펙터가 인지할 수 있는 MMPROJ 에셋 데이터 타입 정의
public class MMProjAsset : ScriptableObject
{
    [HideInInspector] public string filePath;
}
