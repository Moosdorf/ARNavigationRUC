using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneNavigation : MonoBehaviour
{
    public GameObject infoBox;
    private Button start;
    private string mainMenuPage = "Main Menu";
    private static int counter = 0;
    private UpdateARInfo info;
    
    public void Start()
    {
        info = GameObject.Find("UpdateInfo").GetComponent<UpdateARInfo>();
        if (MainManager.sceneNavigation == null) {
            MainManager.sceneNavigation = this;
        }

        if (counter == 0){
            StartCoroutine(LoadMainMenu());   
            counter++;
        }
    }

    public void Update()
    {
        if (MainManager.currentScene.name == "Destination")
        {
            var button = GameObject.Find("GoBTN");
            if (button == null) return;
            start = button.GetComponent<Button>();
        }
        else return;

        if (MainManager.SelectedDestination == null)
        {
            if (start.enabled) start.enabled = false;
        }
        else
        {
            if (!start.enabled) start.enabled = true;
        }
    }
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAdditively(sceneName));
    }

    public IEnumerator LoadSceneAdditively(string sceneName)
    {
        // load navigaion page on top of main menu
        AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive); // load navigation scene for data
        while (!sceneLoad.isDone)
            yield return null;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        
        if (scene.IsValid())
        { // check if new scene is valid
            
            SceneManager.SetActiveScene(scene); // activate it and set it as current scene

            if (MainManager.currentScene.IsValid())
            { // if there is an active valid scene, unload it
                Scene oldScene = MainManager.currentScene;
                SceneManager.UnloadSceneAsync(oldScene); 
            } 
            MainManager.currentScene = scene;
        } else
        {
            Console.WriteLine("new scene is not valid");
        }
    }

    public IEnumerator LoadMainMenu() {
        info.UIOverlay.SetActive(false); // deactivate the ui buttons from AR, it would have overlapped shortly while loading

        // load main menu
        AsyncOperation mainMenuLoad = SceneManager.LoadSceneAsync(mainMenuPage, LoadSceneMode.Additive);
        while (!mainMenuLoad.isDone)
            yield return null;

        // set it as the active scene and store it in current scene variable
        Scene mainMenuScene = SceneManager.GetSceneByName(mainMenuPage); // there might be a way to save the scene when we load it? 
        SceneManager.SetActiveScene(mainMenuScene);
        MainManager.currentScene = mainMenuScene;

    }



    public void UnloadScene() {
        if (MainManager.SelectedDestination != null)
        {
            // we must create route and set the state to route
            MainManager.routeController.CreateRoute();
            info.SetState(UpdateARInfo.State.Route);
        } else info.SetState(UpdateARInfo.State.NoRoute);


        // activate overlay
        info.UIOverlay.SetActive(true);
        // unload the crrent page, this will show the AR scene, which will update
        SceneManager.UnloadSceneAsync(MainManager.currentScene); // and unload the selection scene
    }
}
