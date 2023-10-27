using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TheSleepyKoala
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            Application.targetFrameRate = 60;

            if (continueButton != null)
            {
                if (!SaveManager.instance.HasSaveAvailable())
                {
                    continueButton.interactable = false;
                }
                else
                {
                    eventSystem.SetSelectedGameObject(continueButton.gameObject);
                }
            }
        }

        public void NewGame()
        {
            if (SaveManager.instance.HasSaveAvailable())
            {
                SaveManager.instance.DeleteSave();
            }

            SaveManager.instance.CurrentFloor = 1;
            SceneManager.LoadScene("Dungeon");
        }

        public void ContinueGame()
        {
            SaveManager.instance.LoadGame();
        }

        public void QuitToMenu()
        {
            SceneManager.LoadScene("Main Menu");
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}