using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;


namespace ArouseBlockchain.Common
{
    public static class AConfig
    {
        public class NetConfig
        {
            public string Version { get; set; }
            public List<string> BootSrtapPeers { get; set; }
            public List<string> PublicIPServices { get; set; }
        }

        static NetConfig netConfig;

        public static async UniTask<NetConfig> UpdateNetConfig()
        {
            string jsonfilename = "appsetting.json";
            string jsonPath = Application.persistentDataPath+ "/Config";
            string jsonfilePath = Path.Combine(jsonPath, jsonfilename);

            string jsonContent = await AUtils.AsyncLoadTextFile("http://120.26.66.97:43023/cj");
            ALog.Log("network read json : \n" + jsonContent);

            try
            {
                if (jsonContent != null)
                {
                    netConfig = JsonConvert.DeserializeObject<NetConfig>(jsonContent);

                    if (!Directory.Exists(jsonPath)) Directory.CreateDirectory(jsonPath);
                    File.WriteAllText(jsonfilePath, jsonContent);

                    ALog.Log("[NetConfig] network read succeeded and cached : " + jsonfilePath);
                    return netConfig;
                }
                else
                {
                    if (File.Exists(jsonfilePath))
                    {
                        ALog.Log("[NetConfig] network read error, local cache will be obtained : " + jsonfilePath);
                        string readText = File.ReadAllText(jsonfilePath);
                        netConfig = JsonConvert.DeserializeObject<NetConfig>(readText);
                        return netConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                ALog.Error("Error NetConfig : " + ex);
            }
           
           

            return null;
        }

    }


}