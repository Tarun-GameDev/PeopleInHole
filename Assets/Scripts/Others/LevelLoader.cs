using DG.Tweening;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
/*using LionStudios.Suite.SplashScreen;*/
using Sirenix.OdinInspector;


public class LevelLoader : MonoBehaviour
{
    public static bool _firstTime = true;
    public static bool alreadylogged = false;

    private static readonly int[] ExcludedLevels = { 3, 5, 8, 11, 16 }; // Add all levels to exclude here
    private const int MaxRandomLevel = 25; // Because 

    private void Awake()
    {
        Application.targetFrameRate = 120;
        DOTween.defaultAutoKill = true;
        DOTween.SetTweensCapacity(1000, 50);
        /*Debug.Log($"Level No Loading:{LoadLevelNo()}");*/
        //LoadingSceneManager.SetSceneToLoad(LoadLevelNo());
        LoadLevel();

        /*if (alreadylogged)
        {
            LoadLevel();
            return;
        }

        LoadingSceneManager.SetSceneToLoad(LoadLevelNo());
        alreadylogged = true;*/
    }

    private void Start()
    {
        //LoadLevel();
        //if (!_firstTime)
        //LoadLevel();    
    }


    #region Old Code

    private static readonly int DefaultLevel = 1;
    private static readonly int MinPlayableLevel = 1;
    private static readonly int MaxPlayableLevel = 27;
    private static readonly int MinRandomLevel = 5;

    public static int LoadLevelNo()
    {
        _firstTime = false;
        return GetLevelToLoad();
    }

    public static void LoadLevel()
    {
        _firstTime = false;
        int levelToLoad = GetLevelToLoad();
        SceneManager.LoadScene(levelToLoad);
    }

    private static int GetLevelToLoad()
    {
        int storedLevel = PlayerPrefs.GetInt("NextLevel", DefaultLevel);

        if (storedLevel >= MinPlayableLevel && storedLevel <= MaxPlayableLevel)
        {
            return storedLevel;
        }

        if (storedLevel <= 0)
        {
            PlayerPrefs.SetInt("NextLevel", DefaultLevel);
            return DefaultLevel;
        }

        return GetRandomLevel();
    }

    private static int GetRandomLevel()
    {
        int randomLevel;
        do
        {
            randomLevel = Random.Range(MinRandomLevel, MaxRandomLevel + 1);
        } while (ExcludedLevels.Contains(randomLevel));

        return randomLevel;
    }

    #endregion


    public int currentLevel = 0;
    public int newCurrentLevel = 1;
    
    [Button("Fetch Current Level")]
    public void FetchCurrenLevel()
    {
        currentLevel = PlayerPrefs.GetInt("NextLevel", DefaultLevel);
    }

    [Button("Set Current Level")]
    public void SetCurrentLevel()
    {
        PlayerPrefs.SetInt("NextLevel", newCurrentLevel);
        PlayerPrefs.Save(); 
        FetchCurrenLevel();
    }
}