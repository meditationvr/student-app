using UnityEngine;
using SocketIO;
using UnityEngine.Video;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class Calibration : MonoBehaviour
{
    private SocketIOComponent socket;
    private bool meditationInProgress;
    private bool authenticated;
    private float clipLenghtInSeconds;
    private float clipLenghtPerLabelInSeconds;
    private List<string> eegLabels;
    public GameObject cubeRoom;
    public GameObject videoSphere;
    public GameObject gvrPointer;

    [Serializable]
    public class LabelledDataPacket : MuseWatcher.DataPacket
    {
        public string Label;
    }

    // Start is called before the first frame update
    void Start()
    {
        clipLenghtInSeconds = videoSphere.GetComponent<AudioSource>().clip.length;

        eegLabels = new List<string>(new string[] { "Shallow", "Medium", "Deep" });
        clipLenghtPerLabelInSeconds = clipLenghtInSeconds / eegLabels.Count;
        meditationInProgress = false;
        authenticated = false;

        GameObject go = GameObject.Find("SocketIO");
        socket = go.GetComponent<SocketIOComponent>();

        socket.On("open", OpenSocket);
        socket.On("error", ErrorSocket);
        socket.On("close", CloseSocket);
        socket.On("authenticate", AuthenticateSocket);
        socket.On("authenticated", AuthenticatedSocket);
        socket.On("eeg-calibration-started", EEGCalibrationStarted);
        socket.On("eeg-calibration-done", EEGCalibrationDone);

        socket.Connect();
    }

    void Update()
    {
        if (meditationInProgress)
        {
            if (!videoSphere.GetComponent<AudioSource>().isPlaying ||
                MuseWatcher.instance.ConnectionStatus == MuseWatcher.ConnectionStatusType.Disconnected)
            {
                meditationInProgress = false;
                socket.Emit("eeg-calibration-end");
            }
        } else if (MuseWatcher.instance.ConnectionStatus == MuseWatcher.ConnectionStatusType.Disconnected)
        {
            StartCoroutine(
                VRHelper.SwitchToVR(
                    "none",
                    () => SceneManager.LoadScene("Authentication")
                )
            );
        }
    }

    public void startMeditation()
    {
        if (authenticated)
        {
            gvrPointer.SetActive(false);
            socket.Emit("eeg-calibration-start");
        }
    }

    void EEGCalibrationStarted(SocketIOEvent e)
    {
        Destroy(cubeRoom);
        videoSphere.GetComponent<VideoPlayer>().Play();
        videoSphere.GetComponent<AudioSource>().Play();

        meditationInProgress = true;
        MuseWatcher.instance.museDataPacketsToCall = museDataPackets;
    }

    void EEGCalibrationDone(SocketIOEvent e)
    {
        StartCoroutine(
            VRHelper.SwitchToVR(
                "none",
                () => SceneManager.LoadScene("PairStudentWithClassroom")
            )
        );
    }

    void museDataPackets(MuseWatcher.DataPacket data)
    {
        if (meditationInProgress && data.DataPacketType == "EEG")
        {
            float playBackTimeInSeconds = videoSphere.GetComponent<AudioSource>().time;
            
            int eegLabelIndex = 0;

            /*while (playBackTimeInSeconds > 0)
            {
                playBackTimeInSeconds -= clipLenghtPerLabelInSeconds;
                ++eegLabelIndex;
            }
            if (eegLabelIndex > 0)
            {
                --eegLabelIndex;
            }*/

            float minute = 60f;
            if (playBackTimeInSeconds < (3f * minute))
            {
                eegLabelIndex = 0;
            } else if (playBackTimeInSeconds > (3f * minute) && playBackTimeInSeconds < (14f * minute))
            {
                eegLabelIndex = 1;
            } else
            {
                eegLabelIndex = 2;
            }

            LabelledDataPacket dataWithLabel = new LabelledDataPacket
            {
                DataPacketType = data.DataPacketType,
                DataPacketValue = data.DataPacketValue,
                TimeStamp = data.TimeStamp,
                Label = eegLabels[eegLabelIndex]
            };

            socket.Emit("eeg-calibration-data", 
                new JSONObject(
                    JsonUtility.ToJson(dataWithLabel)
                )
            );
        }
    }

    void AuthenticateSocket(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Authenticate received: " + e.name + " " + e.data);
        FBAccessToken fbAccessToken = new FBAccessToken();
        fbAccessToken.TokenString = Facebook.Unity.AccessToken.CurrentAccessToken.TokenString;
        fbAccessToken.UserType = "Student";
        socket.Emit("authenticate", new JSONObject(JsonUtility.ToJson(fbAccessToken)));
    }

    void AuthenticatedSocket(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Authenticated received: " + e.name + " " + e.data);
        authenticated = true;
    }

    void OpenSocket(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
    }

    void ErrorSocket(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    void CloseSocket(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
    }
}
