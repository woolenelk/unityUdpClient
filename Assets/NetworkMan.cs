using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Diagnostics;
//using UnityEditor.PackageManager;
using UnityEngine.UIElements;

public class NetworkMan : MonoBehaviour
{
    
    public UdpClient udp;
    public GameObject prefab;
    public Dictionary<string, GameObject> currentPlayers = new Dictionary<string, GameObject>();
    
    public List<string> SpawningPlayers;
    public List<string> DestroyingPlayers;

    private string myID = "";

    // Start is called before the first frame update
    void Start()
    {

        udp = new UdpClient();

		//udp.Connect("18.188.244.109", 12345);
        udp.Connect("localhost",12345);

        UpdateMessage message = new UpdateMessage();
        message.cmd = "connect";
        string m = JsonUtility.ToJson(message);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(m);
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);

        InvokeRepeating("UpdatePosition", 1, 1);


    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        DROPPED_CLIENT,
        CONFIRM_IP
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
        public string id;
    }

    [Serializable]
    public class UpdateMessage
    {
        public string cmd;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class Player
    {
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        [Serializable]
        public struct receivedPosition
        {
            public float X;
            public float Y;
            
        }
        public string id;
        public receivedColor color;
        public receivedPosition position;
    }


    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    public Player spawningPlayer;
    public Player despawningPlayer;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        
        //UnityEngine.Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);


        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    UnityEngine.Debug.Log("JOINED CLIENT:" + returnData);

                    UnityEngine.Debug.Log("<>>" + latestMessage.id + "<>>returndata");
                    SpawningPlayers.Add(latestMessage.id);
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    UnityEngine.Debug.Log("UPDATE PLAYERS:" + returnData);
                    break;
                case commands.DROPPED_CLIENT:
                    UnityEngine.Debug.Log("DROPPED CLIENT:" + returnData);

                    UnityEngine.Debug.Log("<<>" + latestMessage.id + "<<>returndata");
                    DestroyingPlayers.Add(latestMessage.id);
                    break;
                case commands.CONFIRM_IP:
                    UnityEngine.Debug.Log("MYID: " + latestMessage.id + ":MYID");
                    myID = latestMessage.id;
                    break;
                default:
                    UnityEngine.Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            UnityEngine.Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }


    void SpawnPlayers()
    {
        foreach (string s in SpawningPlayers)
        {
            UnityEngine.Debug.Log("<>>" + s + "<>>spawn");
            if (s==null)
            { 
            }
            else if (!currentPlayers.ContainsKey(s))
            {
                
                currentPlayers.Add(s, Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity));
                currentPlayers[s].GetComponent<NetworkID>().Id = s;
                currentPlayers[s].name = s;
            }
        }
        SpawningPlayers.Clear();
        //string playersinclient = "Players in Client : ";
        //foreach (KeyValuePair<string, GameObject> pair in currentPlayers)
        //{
        //    playersinclient += pair.Key + " | ";
        //}
        //UnityEngine.Debug.Log(playersinclient);
    }


    void UpdatePlayers(){
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {
            //UnityEngine.Debug.Log("Update :" + lastestGameState.players[i].color.R + " | " + lastestGameState.players[i].color.G + " | " + lastestGameState.players[i].color.B);
            if (currentPlayers.ContainsKey(lastestGameState.players[i].id))
            {
                if (currentPlayers[lastestGameState.players[i].id] != null)
                {
                    //UnityEngine.Debug.Log("updating Players");
                    if (lastestGameState.players[i].id == myID)
                        currentPlayers[lastestGameState.players[i].id].GetComponent<NetworkID>().clientCube = true;
                    else
                        currentPlayers[lastestGameState.players[i].id].GetComponent<NetworkID>().clientCube = false;
                    currentPlayers[lastestGameState.players[i].id].GetComponent<NetworkID>().color = new Color(lastestGameState.players[i].color.R, lastestGameState.players[i].color.G, lastestGameState.players[i].color.B);
                    currentPlayers[lastestGameState.players[i].id].GetComponent<NetworkID>().cubePosition = new Vector3(lastestGameState.players[i].position.X, lastestGameState.players[i].position.Y, 0);
                }
                else
                {
                    currentPlayers[lastestGameState.players[i].id] = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
                }
            }
            else
            {
                SpawningPlayers.Add(lastestGameState.players[i].id);
            }
        }
        
    }

    void DestroyPlayers()
    {
        foreach (string s in DestroyingPlayers)
        {
            UnityEngine.Debug.Log("<<>" + s + "<<>Despawn");
            if (s == null)
            {

            }
            else if (currentPlayers.ContainsKey(s))
            {
                //currentPlayers[s].SetActive(false);
                Destroy(currentPlayers[s]);
                UnityEngine.Debug.Log("Remove Player from List: " + currentPlayers.Remove(s));
            }
        }
        //string playersinclient = "Players in Client : ";
        //foreach (KeyValuePair<string, GameObject> pair in currentPlayers)
        //{
        //    playersinclient += pair.Key + " | ";
        //}
        //UnityEngine.Debug.Log(playersinclient);

        DestroyingPlayers.Clear();

    }

    void HeartBeat()
    {
        UpdateMessage message = new UpdateMessage();
        message.cmd = "heartbeat";
        string m = JsonUtility.ToJson(message);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(m);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void UpdatePosition()
    {
        if (currentPlayers.ContainsKey(myID))
        {
            UpdateMessage message = new UpdateMessage();
            message.cmd = "updatePosition";
            message.x = currentPlayers[myID].transform.position.x;
            message.y = currentPlayers[myID].transform.position.y;
            message.z = currentPlayers[myID].transform.position.z;
            string m = JsonUtility.ToJson(message);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(m);
            udp.Send(sendBytes, sendBytes.Length);
        }
    }

    void Update(){
        
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
        
        if (Input.GetKey(KeyCode.W))
        {
            currentPlayers[myID].transform.Translate(new Vector3(0.0f, 0.0f, 0.2f));
            //Byte[] sendBytes = Encoding.ASCII.GetBytes("CubeUp");
            //udp.Send(sendBytes, sendBytes.Length);
        }
        if (Input.GetKey(KeyCode.S))
        {
            currentPlayers[myID].transform.Translate(new Vector3(0.0f, 0.0f, -0.2f));
            //Byte[] sendBytes = Encoding.ASCII.GetBytes("CubeDown");
            //udp.Send(sendBytes, sendBytes.Length);
        }
        if (Input.GetKey(KeyCode.A))
        {
            currentPlayers[myID].transform.Translate(new Vector3(-0.2f, 0.0f, 0.0f));
            //Byte[] sendBytes = Encoding.ASCII.GetBytes("CubeLeft");
            //udp.Send(sendBytes, sendBytes.Length);
        }
        if (Input.GetKey(KeyCode.D))
        {
            currentPlayers[myID].transform.Translate(new Vector3(0.2f, 0.0f, 0.0f));
            //Byte[] sendBytes = Encoding.ASCII.GetBytes("CubeRight");
            //udp.Send(sendBytes, sendBytes.Length);
        }
        if (Input.GetKey(KeyCode.R))
        {
            currentPlayers[myID].transform.Translate(new Vector3(0.0f, 0.2f, 0.0f));
            //Byte[] sendBytes = Encoding.ASCII.GetBytes("CubeRight");
            //udp.Send(sendBytes, sendBytes.Length);
        }
        if (Input.GetKey(KeyCode.F))
        {
            currentPlayers[myID].transform.Translate(new Vector3(0.0f, -0.2f, 0.0f));
            //Byte[] sendBytes = Encoding.ASCII.GetBytes("CubeRight");
            //udp.Send(sendBytes, sendBytes.Length);
        }
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}
