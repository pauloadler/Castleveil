using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string GameplayScene = "Assets/_Game/Scenes/Milestone1.unity";
        private bool loading;

        public void NewGame()
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Debug.LogError("Main Menu: enable Milestone1 in the active build scene list.", this);
                return;
            }
            loading = true;
            SceneManager.LoadScene(GameplayScene);
        }

        public void OpenOptions() => Debug.Log("Opções ainda não implementadas.", this);

        public void ExitGame()
        {
            if (Application.isEditor)
            {
                Debug.Log("Sair: Application.Quit funciona no aplicativo compilado.", this);
                return;
            }
            Application.Quit();
        }
    }
}
