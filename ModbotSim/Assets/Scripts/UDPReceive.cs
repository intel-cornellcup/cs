﻿using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPReceive : MonoBehaviour
{
    // receiving Thread
    Thread receiveThread;

    // udpclient object
    UdpClient client;

    // public string IP = "127.0.0.1"; default local
    public int port; // define > init

    // infos
    public Vector3 lastReceivedUDPPacket;

    //constants
    const int INIT_PORT = 8051;
    const string MULTICAST_ADDR = "239.0.0.222";

    // start from unity3d
    public void Start()
    {
        print("UDPSend.init()");

        // define port
        port = INIT_PORT;

        // status
        print("Listening on 127.0.0.1 : " + port);


        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

    }

    // receive thread
    private void ReceiveData()
    {
        client = new UdpClient();

        IPEndPoint localEp = new IPEndPoint(IPAddress.Any, port);
        client.Client.Bind(localEp);

        client.JoinMulticastGroup(IPAddress.Parse(MULTICAST_ADDR));
        while (true)
        {
            try
            {
                byte[] data = client.Receive(ref localEp);
                string text = Encoding.UTF8.GetString(data);
                string[] message = text.Split(',');
                Vector3 result = new Vector3(float.Parse(message[0]), float.Parse(message[1]), float.Parse(message[2]));

                print(">> " + result);

                lastReceivedUDPPacket = result;
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    // getLatestUDPPacket
    // cleans up the rest
    public Vector3 getLatestUDPPacket()
    {
        return lastReceivedUDPPacket;
    }

    public void OnApplicationQuit()
    {
		Debug.Log ("!!!!!!!!");
		Debug.Log (this.ToString());
        if (receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        client.Close();
    }
}