using Facebook.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PairStudentWithClassroom : MonoBehaviour
{
    public GameObject messageBoxText;
    public GameObject loadingSpinner;
    public GameObject yesButton;
    public GameObject notNowButton;

    // Update is called once per frame
    void Start()
    {
        // AccessToken class will have session details
        var aToken = AccessToken.CurrentAccessToken;

        FBAccessToken fbAccessToken = new FBAccessToken();
        fbAccessToken.TokenString = aToken.TokenString;

        StartCoroutine(
            HTTPClient.Post("/hasCalibration", JsonUtility.ToJson(fbAccessToken), statusCode => {
                if (statusCode == "200")
                {
                    StartCoroutine(
                        VRHelper.SwitchToVR(
                            "cardboard", // none
                            () => SceneManager.LoadScene("MeditationRoom")
                        )
                    );
                } else {
                    messageBoxText.GetComponent<TextMeshProUGUI>().text = "Before joining a classroom, lets do a quick meditation to calibrate your device.";
                    loadingSpinner.SetActive(false);
                    yesButton.SetActive(true);
                    notNowButton.SetActive(true);
                }
                    
            })
        );

        messageBoxText.GetComponent<TextMeshProUGUI>().text = "Checking for calibrations...";
        loadingSpinner.SetActive(true);
        messageBoxText.SetActive(true);
        yesButton.SetActive(false);
        notNowButton.SetActive(false);
    }

    public void Yes()
    {
        messageBoxText.GetComponent<TextMeshProUGUI>().text = "Loading Meditation Room...";
        messageBoxText.SetActive(true);
        loadingSpinner.SetActive(true);
        yesButton.SetActive(false);
        notNowButton.SetActive(false);

        StartCoroutine(
            VRHelper.SwitchToVR(
                "cardboard", // none
                () => SceneManager.LoadScene("Calibration")
            )
        );
    }

    public void LogOut()
    {
        FB.LogOut();
        MuseWatcher.instance.disconnect();
        SceneManager.LoadScene("Authentication");
    }
}
