using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;

public class ApplyTexture : MonoBehaviour
{
   public GameObject depthDisplayer;

   private Texture2D ourTex = null;
   WebSocket websocket;
   float timeElapsed = 0;
   bool sendOnce = false;

   int decimationAmount = 16;
   int rX;
   int rY;

   GameObject[] ourCubes;
   byte[] depthData;


   async void Start()
   {
      rX = (640 / decimationAmount);
      rY = (480 / decimationAmount);

      depthData = new byte[rX * rY];

      ourCubes = new GameObject[rX * rY]; 
      for (int y = 0; y < rY; y++)
      {
         for (int x = 0; x < rX; x++)
         {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(x, y, 0);
            ourCubes[y * rX + x] = cube;
         }
      }

      websocket = new WebSocket("ws://45.55.43.77:3902");

      websocket.OnOpen += () =>
      {
         Debug.Log("Connection open!");
      };

      websocket.OnError += (e) =>
      {
         Debug.Log("Error! " + e);
      };

      websocket.OnClose += (e) =>
      {
         Debug.Log("Connection closed!");
      };

      await websocket.Connect();

   }

   // Update is called once per frame
   void Update()
   {
#if !UNITY_WEBGL || UNITY_EDITOR
      websocket.DispatchMessageQueue();
#endif

      if (ourTex == null)
         return;

      timeElapsed += Time.deltaTime;
      if (timeElapsed < 0.5)
         return;

      timeElapsed = 0.0f; //reset


      /*if (sendOnce == true)
      {
         return;
      }
      sendOnce = true;
      */
      Color[] ourPixels = ourTex.GetPixels(); 

     // byte[] depthData = new byte[rX*rY];

      int numValid = 0;

      

      for (int y = 0; y < 480; y += decimationAmount)
      {
         for (int x = 0; x < 640; x += decimationAmount)
         {

            float avg = 0;
            int count = 0;

            for (int i = 0; i < decimationAmount; i++)
            {
               for (int e = 0; e < decimationAmount; e++)
               {
                  Color c = ourPixels[(y + i) * 640 + x + e];
                  float d = c.r;
                  if (d > 0.0f)
                  {
                     avg += d;
                     count++;
                  }
               }
            }

            int smallX = x / decimationAmount;
            int smallY = y / decimationAmount;
            int index = ((rY-1) - smallY) * rX + smallX;
            float avg100 = 0.0f;
            if (count > 0)
               avg100 = avg / count;

            float zCoord = avg100 * 4000.0f;

            if (zCoord > 60.0f || zCoord == 0.0f)
            {
               ourCubes[index].SetActive(false);
               depthData[index] = 0;
            }
            else
            {
               int asByte = (int)Mathf.Floor((zCoord/60.0f)*255.0f);
               depthData[index] = (byte)asByte;

               Vector3 pos = ourCubes[index].transform.position;
               pos.z = ((float)asByte) / 5.12f;
               ourCubes[index].transform.position = pos;

               ourCubes[index].SetActive(true);
               numValid++;
            }
         }
      }

      Debug.Log("numValid: " + numValid);
      SendWebSocketMessage(depthData);
   }

   public void TextureEvent(Texture incomingTex)
   {
      ourTex = (Texture2D)incomingTex;

      Debug.Log("got incoming texture! " + ourTex.width + "x" + ourTex.height);
      depthDisplayer.GetComponent<RawImage>().texture = incomingTex;
   }

   void SendWebSocketMessage(byte[] depthData)
   {
      if (websocket.State == WebSocketState.Open)
      {
         // Sending bytes
         websocket.Send(depthData);
         Debug.Log("done sending websocket message!");
      }
   }

   async private void OnApplicationQuit()
   {
      await websocket.Close();
   }
}
