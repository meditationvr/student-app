using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class SetupHeadband : MonoBehaviour {
    
    // Public members that connects to UI components
    public Dropdown museList;
    public GameObject connectButton;
    public GameObject messageBoxText;
    public GameObject loadingSpinner;

    private string userPickedMuse;

    // Use this for initialization
    void Start() {
        if (Application.isEditor)
        {
            SceneManager.LoadScene("PairStudentWithClassroom");
        }

#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif

        userPickedMuse = "";
        MuseWatcher.instance.museDevicesListToCall = receiveMuseDevicesList;
        MuseWatcher.instance.museConnectionPacketsToCall = receiveConnectionPacket;
        MuseWatcher.instance.startScanning();
        //InvokeRepeating("MuseWatcher.instance.startScanning", 1.0f, 5.0f);
    }


    // Update is called once per frame
    void Update()
    {
        if (MuseWatcher.instance.ConnectionStatus == MuseWatcher.ConnectionStatusType.Disconnected)
        {
            if (museList.options.Count > 0)
            {
                messageBoxText.SetActive(false);
                museList.gameObject.SetActive(true);
                connectButton.SetActive(true);
                loadingSpinner.SetActive(false);
            } else
            {
                messageBoxText.GetComponent<TextMeshProUGUI>().text = "Searching for Muse Headband...";
                messageBoxText.SetActive(true);
                loadingSpinner.SetActive(true);

                museList.gameObject.SetActive(false);
                connectButton.SetActive(false);
            }
        } else if (MuseWatcher.instance.ConnectionStatus == MuseWatcher.ConnectionStatusType.Connecting) {
            messageBoxText.GetComponent<TextMeshProUGUI>().text = "Connecting to Muse Headband...";
            messageBoxText.SetActive(true);
            loadingSpinner.SetActive(true);

            museList.gameObject.SetActive(false);
            connectButton.SetActive(false);
        } else if (MuseWatcher.instance.ConnectionStatus == MuseWatcher.ConnectionStatusType.Connected)
        {
            SceneManager.LoadScene("PairStudentWithClassroom");
        }
    }

    public void userSelectedMuse()
    {
        userPickedMuse = museList.options[museList.value].text;
        Debug.Log("Selected muse = " + userPickedMuse);
    }

    public void connect()
    {
        // If user just clicks connect without selecting a muse from the
        // dropdown menu, then connect to the one displayed in the dropdown.
        if (userPickedMuse == "")
            userPickedMuse = museList.options[0].text;

        MuseWatcher.instance.connect(userPickedMuse);
    }

    public void disconnect()
    {
        MuseWatcher.instance.disconnect();
    }

    void receiveMuseDevicesList(List<string> muses) {
        museList.ClearOptions();
        museList.AddOptions(muses);
    }

    void receiveConnectionPacket(MuseWatcher.ConnectionPacket connectionPacket)
    {
        Debug.Log("Connection package: " + connectionPacket);
        
        if (connectionPacket.CurrentConnectionState == "CONNECTED")
            SceneManager.LoadScene("PairStudentWithClassroom");
    }
}