using UnityEngine;
using Practice.Base;
using Practice.UI;
using Practice.Common;

namespace Practice.Game
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