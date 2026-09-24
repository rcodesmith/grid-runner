using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Research spike (issue #8). Bootstrap.Init (AfterSceneLoad) destroys every
/// root GameObject in the active scene — and in a PlayMode test run that
/// includes the Unity Test Framework's "Code-based tests runner" object, whose
/// Start() coroutine drives the whole run. Without this the run hangs forever.
///
/// Moves the runner into the DontDestroyOnLoad scene as soon as the first scene
/// loads, before Bootstrap's AfterSceneLoad sweep can reach it.
/// </summary>
static class ProtectTestRunner
{
    const string RunnerName = "Code-based tests runner";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            var runner = GameObject.Find(RunnerName);
            if (runner != null && runner.scene.name != "DontDestroyOnLoad")
            {
                Object.DontDestroyOnLoad(runner);
                Debug.Log("[SPIKE] moved test runner to DontDestroyOnLoad on sceneLoaded");
            }
        };
    }
}
