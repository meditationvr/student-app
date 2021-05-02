using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MuseWatcher : Singleton<MuseWatcher>
{
    // (Optional) Prevent non-singleton constructor use.
    protected MuseWatcher() { }
    
    private string userPickedMuse;
    public enum ConnectionStatusType { Connecting, Connected, Disconnected };
    private ConnectionStatusType connectionStatus;

    private bool headbandOn;
    private LibmuseBridge Lib;

    [Serializable]
    public class ConnectionPacket
    {
        public string PreviousConnectionState;
        public string CurrentConnectionState;
    }

    [Serializable]
    public class DataPacket
    {
        public string DataPacketType;
        public List<float> DataPacketValue;
        public DateTime TimeStamp;
    }

    [Serializable]
    public class ArtifactPacket
    {
        public bool HeadbandOn;
        public bool Blink;
        public bool JawClench;
    }

    public delegate void MuseDevicesListDelegate(List<string> muses);
    public MuseDevicesListDelegate museDevicesListToCall;

    public delegate void MuseConnectionPacketsDelegate(ConnectionPacket data);
    public MuseConnectionPacketsDelegate museConnectionPacketsToCall;

    public delegate void MuseDataPacketsDelegate(DataPacket data);
    public MuseDataPacketsDelegate museDataPacketsToCall;

    public delegate void MuseArtifactPacketsDelegate(ArtifactPacket data);
    public MuseArtifactPacketsDelegate museArtifactPacketsToCall;
    
    public bool HeadbandOn { get => headbandOn; }
    public ConnectionStatusType ConnectionStatus { get => connectionStatus; }

    public override void Init()
    {
#if UNITY_IPHONE
        Lib = new LibmuseBridgeIos();
#elif UNITY_ANDROID
        Lib = new LibmuseBridgeAndroid();
#else
        Lib = new LibmuseBridgeGeneric();
#endif
        userPickedMuse = "";
        connectionStatus = ConnectionStatusType.Disconnected;

        Debug.Log("Libmuse version = " + Lib.getLibmuseVersion());

        registerListeners();
        registerAllData();
    }

    void registerListeners()
    {
        Lib.registerMuseListener(this.name, "receiveMuseList");
        Lib.registerConnectionListener(this.name, "receiveConnectionPackets");
        Lib.registerDataListener(this.name, "receiveDataPackets");
        Lib.registerArtifactListener(this.name, "receiveArtifactPackets");
    }

    void registerAllData()
    {
        // This will register for all the available data from muse headband
        // Comment out the ones you don't want
        Lib.listenForDataPacket("ACCELEROMETER");
        Lib.listenForDataPacket("GYRO");
        Lib.listenForDataPacket("EEG");
        Lib.listenForDataPacket("QUANTIZATION");
        Lib.listenForDataPacket("BATTERY");
        Lib.listenForDataPacket("DRL_REF");
        Lib.listenForDataPacket("ALPHA_ABSOLUTE");
        Lib.listenForDataPacket("BETA_ABSOLUTE");
        Lib.listenForDataPacket("DELTA_ABSOLUTE");
        Lib.listenForDataPacket("THETA_ABSOLUTE");
        Lib.listenForDataPacket("GAMMA_ABSOLUTE");
        Lib.listenForDataPacket("ALPHA_RELATIVE");
        Lib.listenForDataPacket("BETA_RELATIVE");
        Lib.listenForDataPacket("DELTA_RELATIVE");
        Lib.listenForDataPacket("THETA_RELATIVE");
        Lib.listenForDataPacket("GAMMA_RELATIVE");
        Lib.listenForDataPacket("ALPHA_SCORE");
        Lib.listenForDataPacket("BETA_SCORE");
        Lib.listenForDataPacket("DELTA_SCORE");
        Lib.listenForDataPacket("THETA_SCORE");
        Lib.listenForDataPacket("GAMMA_SCORE");
        Lib.listenForDataPacket("HSI_PRECISION");
        Lib.listenForDataPacket("ARTIFACTS");
    }

    // Public methods that gets called on UI events.
    public void startScanning()
    {
        // Must register at least MuseListeners before scanning for headbands.
        // Otherwise no callbacks will be triggered to get a notification.
        Lib.startListening();
    }

    public void connect(string userPickedMuse)
    {
        Debug.Log("Connecting to " + userPickedMuse);
        this.userPickedMuse = userPickedMuse;
        Lib.connect(userPickedMuse);
    }

    public void disconnect()
    {
        Lib.disconnect();
    }

    // These listener methods update the buffer
    // The Update() per frame will display the data.
    void receiveMuseList(string data)
    {
        // This method will receive a list of muses delimited by white space.
        Debug.Log("Found list of muses = " + data);
        List<string> muses = data.Split(' ').ToList<string>();
        museDevicesListToCall?.Invoke(muses);
    }

    void receiveConnectionPackets(string data)
    {
        ConnectionPacket connectionPacket = JsonUtility.FromJson<ConnectionPacket>(data);
        if (connectionPacket.CurrentConnectionState == "CONNECTED")
            connectionStatus = ConnectionStatusType.Connected;
        else if (connectionPacket.CurrentConnectionState == "CONNECTING")
            connectionStatus = ConnectionStatusType.Connecting;
        else if (connectionPacket.CurrentConnectionState == "DISCONNECTED")
            connectionStatus = ConnectionStatusType.Disconnected;

        museConnectionPacketsToCall?.Invoke(connectionPacket);
    }
    
    void receiveDataPackets(string data)
    {

        DataPacket dataPacket = JsonUtility.FromJson<DataPacket>(data);
        museDataPacketsToCall?.Invoke(dataPacket);
    }

    void receiveArtifactPackets(string data)
    {
        
        ArtifactPacket artifactPacket = JsonUtility.FromJson<ArtifactPacket>(data);
        
        headbandOn = artifactPacket.HeadbandOn;
        museArtifactPacketsToCall?.Invoke(artifactPacket);
    }


}
