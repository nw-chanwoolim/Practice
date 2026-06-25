using UnityEngine;
using Practice.Base;
using UnityEditor.VersionControl;
using UnityEngine.SocialPlatforms;

namespace Practice.Play
{   
public class GameManager : ManagerBase<GameManager>
{
    [SerializeField] UgsManager ugsManager;
    [SerializeField] ConfigManager configManager;
    // [SerializeField] DataManager dataManager;
    // [SerializeField] UIManager uiManager;
    // [SerializeField] AssetManager assetManager;
    [SerializeField] LocalizationManager localizationManager;

    protected override void Awake()
    {
        base.Awake();
    }
}
}