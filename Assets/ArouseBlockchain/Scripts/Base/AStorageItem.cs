using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.NetService;
using ArouseBlockchain.Solve;
using UnityEngine;
using ArouseBlockchain.P2PNet;
using Newtonsoft.Json;


namespace ArouseBlockchain
{
    public enum StorageObjItemType: byte
    {
        None,
        Texture,
        Model,
        Animation,
        Effects
    }

    /// <summary>
    /// 储仓物品处理类（铸造、验证、加载实际资源）
    /// </summary>
    public class AStorageItem: IBaseFun
    {

        List<(string fileName, StorageObjItem item)> _ObjItems = new List<(string, StorageObjItem)>();

        public async Task<bool> Init()
        {
            return true;
        }
        public void Close()
        {


        }

        //安卓不支持IO来操作，只能未来获取到之后，直接网络加载资源，铸造可以通过服务器铸造
        //这里的代码只会在Unity编辑器中运行，而客户端应该只需要保留 NFT 的 区块数据即可
        int start_index = 0;
        public List<StorageObjItem> GetGenesis(Solve.KeyPair card, int nubers = 10)
        {
            if (start_index + nubers > 10)
            {
                ALog.Error("超出范围：" + (start_index + nubers));
                return null;
            }

            //背景原因：
            //1. 由于暂时没有精力去再写资源服务器的相关功能
            //2. iOS不支持IO操作，无法构建图片的区块。如果分平台分别不同方式加载的话，效益不加，因为资源的加载本就应资源服务器去服务。
            //所以这里，目前就使用编辑器代码运行的时候，构建创世区块，并且把一些图片作为NFT存在streamingAssetsPath文件夹中
            //打包后，图片和应该Hash好的文件都直接在手机本地，手机用户在通过Hash文件进行加载图片就行
            //正式项目一定不可以这样

            string filepath_Cache = GetRootPath() + "EditorCache.ablian";

#if UNITY_EDITOR
            for (int i = start_index; i < start_index + nubers; i++)
            {
                string fileName = string.Format("FragmentsInDream_SurrealArt_Edwin{0}.jpg", i + 1);
                string filepath = GetRootPath(StorageObjItemType.Texture) + fileName;
                _ObjItems.Add((fileName, Foundry(card, filepath)));
            }
            start_index += nubers;
            File.WriteAllText(filepath_Cache, JsonConvert.SerializeObject(_ObjItems));

#else
            LoadLoacaCache();
#endif


            var objItems = _ObjItems.Select(x => x.item).ToList(); 
            if (objItems == null || _ObjItems == null)
            {
                ALog.Error("objItems || _ObjItems is NUll!!!");
            }

            return objItems;
        }

        void LoadLoacaCache()
        {
            string filepath_Cache = GetRootPath() + "EditorCache.ablian";
            if (File.Exists(filepath_Cache))
            {
                var cache = File.ReadAllText(filepath_Cache);
                if (string.IsNullOrEmpty(cache))
                {
                    ALog.Error("本地没有暂时缓存的链接文件 {0} ", filepath_Cache);
                }
                else
                {
                    _ObjItems = JsonConvert.DeserializeObject<List<(string fileName, StorageObjItem item)>>(cache);
                }
            }
            else ALog.Error("!本地没有链接文件 {0} ", filepath_Cache);

        }

        public string GetTexItemPath(StorageObjItem item_tex)
        {
            if (_ObjItems.Count <=0) LoadLoacaCache();

            var itemTemp = _ObjItems.Find(x => x.item.hash == item_tex.hash);
            if (!itemTemp.Equals(null))
            {
                return GetRootPath(StorageObjItemType.Texture) + itemTemp.fileName;
            }


            return "";
        }


        /// <summary>
        /// 自己时间不充裕，暂时先本地存放固定数量的Obj吧，最好是资源和资源的ID资源分离，区块不保留本身资源
        /// </summary>
        /// <param name="objItemType"></param>
        /// <returns></returns>
        public static string GetRootPath(StorageObjItemType objItemType = StorageObjItemType.None)
        {
            string root_path = Application.streamingAssetsPath;
            switch (objItemType)
            {
                case StorageObjItemType.Texture:
                    root_path += "/Tx";
                    break;
                case StorageObjItemType.Model:
                    root_path += "/Md";
                    break;
                case StorageObjItemType.Animation:
                    root_path += "/An";
                    break;
                case StorageObjItemType.Effects:
                    root_path += "/Ef";
                    break;
            }

            return root_path + "/";
        }

        public StorageObjItem Foundry(Solve.KeyPair card, string file_path)
        {
            if (!File.Exists(file_path))
            {
                ALog.Error("没有找到这个文件 {0}", file_path);
                return null;
            }

            FileInfo file_info = new FileInfo(file_path);
            if (file_info.Length / Math.Pow(1024, 2) >= 1)
            {
                ALog.Error("只允许小于1MB的文件进行铸造 {0}", file_path);
                return null;
            }

            string file_hash = AUtils.BC.GetFileHash(file_path);
            if (file_hash != null)
            {
                //对文件进行识别，判断是否这个文件已经被铸造了
            }

            return CreatStorageObjItem(card, (int)file_info.Length, (objItem) =>
            {
                return AUtils.BC.GetStorageObjHash(objItem, file_path);
            });
        }


        public async Task<StorageObjItem> FoundryAsync(Solve.KeyPair card, string file_path)
        {
            if (!File.Exists(file_path))
            {
                ALog.Error("没有找到这个文件 {0}", file_path);
                return null;
            }

            FileInfo file_info = new FileInfo(file_path);
            if (file_info.Length / Math.Pow(1024, 2) >= 1)
            {
                ALog.Error("只允许小于1MB的文件进行铸造 {0}", file_path);
                return null;
            }

            string file_hash = AUtils.BC.GetFileHash(file_path);
            if (file_hash != null)
            {
                //对文件进行识别，判断是否这个文件已经被铸造了
            }

            return CreatStorageObjItem(card, (int)file_info.Length, (objItem) =>
            {
                return AUtils.BC.GetStorageObjHash(objItem, file_path);
            });

        }

       
        /// <summary>
        /// 铸造储仓OBJ物品，铸造业务不可撤回，不允许对一个文件进行的铸造
        /// 铸造业务流程：
        /// 1.获取规定类型的文件，通过核心数据信息进行Hash
        /// 2.铸造完成后进行签名，并且和积分一样进行交易->账号->块的流程。[且可以通过共钥找到最终所属人]
        /// 3.铸造完成后，原始文件进行保存(目前本地缓存)，并且设置权限为只读，并且铸造后的文件需要重新命名
        /// </summary>
        /// <returns></returns>
        public StorageObjItem Foundry(Solve.KeyPair card, byte[] obj_stream)
        {
            string file_hash = AUtils.BC.GetFileHash(obj_stream);
            if (file_hash != null)
            {
                //对文件进行识别，判断是否这个文件已经被铸造了
            }

            return CreatStorageObjItem(card, obj_stream.Length, (objItem) =>
            {
                return AUtils.BC.GetStorageObjHash(objItem, obj_stream);
            });
        }

        StorageObjItem CreatStorageObjItem(Solve.KeyPair card, int file_size, Func<StorageObjItem, string> gethash)
        {
            StorageObjItem objItem = new StorageObjItem();
            long time = AUtils.NowUtcTime_Unix;
            objItem.name = "";
            objItem.created = time;
            objItem.file_size = file_size;
            string hash = gethash(objItem);// await AUtils.BC.GetStorageObjHash(objItem, obj_stream);
            if (hash == null)
            {
                ALog.Log("铸造失败...无法识别并转化为Hash");
                return null;
            }
            objItem.hash = hash;
            objItem.pub_key = card.PublicKey.ToString();
            objItem.signature = AUtils.BC.BytesToHexString(WalletsSolve.Sign(card.PrivateKey.PrivateKey, objItem.hash).Signature);

            return objItem;
        }

       
    }
}

