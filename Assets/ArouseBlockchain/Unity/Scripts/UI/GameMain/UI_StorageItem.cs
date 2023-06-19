using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.Solve;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UI_StorageItem : MonoBehaviour
{
    StorageObjItem _StorageObjItem;
    public Button _Button;
    public RawImage _ItemImage;
    public TextMeshProUGUI _ItemName;
    public TextMeshProUGUI _ItemDescribe;

    Texture2D texture = null;

    public void InitUI(StorageObjItem storageObjItem)
    {
        _StorageObjItem = storageObjItem;

        _Button = GetComponent<Button>();
        _ItemName.text = storageObjItem.name;
        _ItemDescribe.text = "";

        // 需要网络下载图片
        // 可行的方案是，用户或者社区创建的NFT，生成之后，分为元数据和区块数据，元数据即是文件本身二进制文件，保存在文件服务器中，区块数据保存在区块链中。
        // 需要在用户展示图片的时候，通过NFT的唯一地址，在文件服务器进行获取。

        //这里不加赘述了，不能再浪了，得花些时间在挣钱上了，
        //这里就直接加载本地的文件了

       // 这里是空的 _StorageObjItem.hash 

        string filePath =  SolveManager.BlockChain.Storage.GetTexItemPath(_StorageObjItem);
        if (!string.IsNullOrEmpty(filePath))
        {
            StartCoroutine(LoadTexture(filePath));
        }
        else ALog.Log("{0} :没有这个物品的图片文件 {1}" ,gameObject.name , _StorageObjItem.ToString());


    }

    IEnumerator LoadTexture(string filePath)
    {
        if (texture != null) yield break;

        var uri = new System.Uri(filePath).AbsoluteUri;

        if (File.Exists(filePath))
        {
            using (UnityWebRequest request = new UnityWebRequest(uri))
            {
                DownloadHandlerTexture downloadHandlerTexture = new DownloadHandlerTexture(true);
                request.downloadHandler = downloadHandlerTexture;
                yield return request.SendWebRequest();

                bool connectionError = request.result == UnityWebRequest.Result.ConnectionError;
                bool protocolError = request.result == UnityWebRequest.Result.ProtocolError;

                if (request.isDone && string.IsNullOrWhiteSpace(request.error))
                {
                    texture = downloadHandlerTexture.texture;
                    _ItemImage.texture = texture;
                }
                else
                {
                    ALog.Error("{0} :File read error: {1} ; connectionError= {2} ; protocolError= {3}", gameObject.name ,request.error, connectionError, protocolError);
                   
                }

            }

        }
           
       
    }


}
