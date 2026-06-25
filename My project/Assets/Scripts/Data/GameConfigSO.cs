using UnityEngine;
using Practice.Play;

namespace Practice.Data
{   
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Practice /Game Config")]
    public class GameConfigSO : ScriptableObject
    {
    [Header("Game Config")]
    public float lastPlayTime; //unix timestamp of the last time the game was played
    public int totalPlaycount; //total play time in seconds
    public bool isFirstPlay; //whether this is the first time the game is being played
    public LocalizationManager.Language currentLanguage; //the current language of the game
    }
}