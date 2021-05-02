using UnityEngine;
using SocketIO;
using System;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Video;
using agora_gaming_rtc;
using UnityEngine.Android;

[Serializable]
public class RoomData
{
    public string agoraIoVoiceKey;
    public string roomId;
}

public class MeditationRoom : MonoBehaviour
{
    private SocketIOComponent socket;
    private bool meditationInProgress;
    private IRtcEngine mRtcEngine = null;

    public GameObject cubeRoom;
    public GameObject cubeRoomText;    
    public GameObject videoSphere;
    public GameObject gvrPointer;

    [Serializable]
    public class LabelledDataPacket : MuseWatcher.DataPacket
    {
        public string Label;
    }

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

    }

    // Start is called before the first frame update
    void Start()
    {
        #if (UNITY_2018_3_OR_NEWER)
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone)) {

        } else {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        #endif

        meditationInProgress = false;

        cubeRoomText.GetComponent<TextMeshPro>().SetText("Searching for instructor...");

        GameObject go = GameObject.Find("SocketIO");
        socket = go.GetComponent<SocketIOComponent>();

        socket.On("open", OpenSocket);
        socket.On("error", ErrorSocket);
        socket.On("close", CloseSocket);
        socket.On("authenticate", AuthenticateSocket);
        socket.On("authenticated", AuthenticatedSocket);
        socket.On("could-not-find-instruction-room", CouldNotFindInstructionRoomSocket);
        socket.On("joined-instruction-room", JoinedInstructionRoomSocket);
        socket.On("instruction-started", InstructionStartedSocket);
        socket.On("instruction-did-not-start", InstructionDidNotStartSocket);
        socket.On("instructor-left-room", InstructorLeftRoomSocket);

        socket.Connect();
    }

    void Update()
    {
        if (MuseWatcher.instance.ConnectionStatus == MuseWatcher.ConnectionStatusType.Disconnected)
        {
            StartCoroutine(
                VRHelper.SwitchToVR(
                    "none",
                    () => SceneManager.LoadScene("Authentication")
                )
            );
        }
    }

    void OnDestroy()
    {
        Debug.Log("Scene got destroyed!");
        LeaveChannel();
        socket.Emit("disconnect");
    }

    void LeaveChannel()
    {
        // int duration = mRtcEngine.GetAudioMixingDuration ();
        // int current_duration = mRtcEngine.GetAudioMixingCurrentPosition ();

        // IAudioEffectManager effect = mRtcEngine.GetAudioEffectManager();
        // effect.StopAllEffects ();
        if (mRtcEngine != null)
            mRtcEngine.LeaveChannel();
    }

    void JoinChannel(string channelName)
    {
        Debug.Log(string.Format("tap joinChannel with channel name {0}", channelName));

        if (string.IsNullOrEmpty(channelName))
        {
            return;
        }

        mRtcEngine.JoinChannel(channelName, "extra", 0);
        // mRtcEngine.JoinChannelByKey ("YOUR_CHANNEL_KEY", channelName, "extra", 9527);
    }

    void InstructorLeftRoomSocket(SocketIOEvent e)
    {
        StartCoroutine(
            VRHelper.SwitchToVR(
                "none",
                () => SceneManager.LoadScene("Authentication")
            )
        );
    }

    void InstructionDidNotStartSocket(SocketIOEvent e)
    {
        socket.Emit("check-if-instructor-is-ready");
    }

    void InstructionStartedSocket(SocketIOEvent e)
    {
        Destroy(cubeRoom);
        videoSphere.GetComponent<VideoPlayer>().Play();
        meditationInProgress = true;
        gvrPointer.SetActive(false);

        RoomData roomData = JsonUtility.FromJson<RoomData>(e.data.ToString());
        mRtcEngine = IRtcEngine.GetEngine(roomData.agoraIoVoiceKey);
        JoinChannel(roomData.roomId);

        MuseWatcher.instance.museDataPacketsToCall = museDataPackets;
    }

    void museDataPackets(MuseWatcher.DataPacket data)
    {
        if (meditationInProgress && data.DataPacketType == "EEG")
        {
            socket.Emit("eeg-instruction-data", new JSONObject(JsonUtility.ToJson(data)));
        }
    }

    void JoinedInstructionRoomSocket(SocketIOEvent e)
    {
        cubeRoomText.GetComponent<TextMeshPro>().SetText("Found an instructor, waiting for session to start...");
        socket.Emit("check-if-instructor-is-ready");
    }

    void CouldNotFindInstructionRoomSocket(SocketIOEvent e)
    {
        socket.Emit("find-instruction-room");
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
        socket.Emit("find-instruction-room");
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
