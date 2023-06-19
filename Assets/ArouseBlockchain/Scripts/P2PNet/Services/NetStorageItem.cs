using System;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Cms;
using System.Security.Policy;
using LiteDB;
using SuperNet.Netcode.Util;
using Unity.VisualScripting;
using UnityEngine;


namespace ArouseBlockchain.P2PNet
{
    public enum StorageItemType : byte
    {
        None = 0,
        NumberBase = 1,
        ObjBase = 2
    }

    public interface IStorageItem : IEquatable<IStorageItem>
    {
        //流转物品的类型
        public StorageItemType item_type { get; set; }
        //流转的物品内容
        public string get_content { get; }
    }

    public class StorageItem: IStorageItem
    {
        public ObjectId Id { get; set; }
        //流转物品的类型
        public virtual StorageItemType item_type { get; set; } = StorageItemType.None;
        //流转的物品内容
        [BsonIgnore]
        public virtual string get_content { get;}

        public override bool Equals(object obj) => Equals(obj as StorageItem);

        public bool Equals(IStorageItem other)
        {
            if (other is null)
                return false;

            return this.item_type == other.item_type && this.get_content == other.get_content;
        }

        public override int GetHashCode() => (item_type, get_content).GetHashCode();
    }

    /// <summary>
    /// 每个Obj对象都有着唯一的Hash，所以不能有数量的属性
    /// </summary>
    public class StorageObjItem: StorageItem
    {
        /// <summary>
        /// Only Get to get_content
        /// </summary>
        //物品Hash数

        public StorageObjItem()
        {

        }

       // public new ObjectId Id { get; set; }
       //[BsonId]
        public string hash {get; set; }
        /// 物品的名称
        public string name { get; set; }
        //物品主人的签名
        public string signature { get; set; }
        //物品主人的公钥匙
        public string pub_key { get; set; }

        public long created { get; set; }

        public int file_size { get; set; }

        //还需要添加作者信息

        public override StorageItemType item_type { get; set; } = StorageItemType.ObjBase;

        /// <summary>
        /// Only Set to hash
        /// </summary>
        [BsonIgnore]
        public override string get_content { get=>hash;}

        public void Read(Reader reader)
        {
            hash = reader.ReadString();
            name = reader.ReadString();
            signature = reader.ReadString();
            pub_key = reader.ReadString();

            created = reader.ReadInt64();
            file_size = reader.ReadInt32();

        }

        public void Write(Writer writer)
        {
            writer.Write(hash);
            writer.Write(name);
            writer.Write(signature);
            writer.Write(pub_key);
            writer.Write(created);
            writer.Write(file_size);
        }

        public override string ToString()
        {
            return string.Format("hash: {0} ; \nname: {1} ; \nsignature: {2} ; \npub_key: {3} ; \ncreated: {4} ; \nfile_size: {5} ;",
                hash, name, signature, pub_key, created, file_size);//base.ToString();
        }

        public new bool Equals(IStorageItem other)
        {
            var obj = other as StorageObjItem;
            if (other is null)
                return false;
           
            return this.hash == obj.hash && this.name == obj.name
                && this.signature == obj.signature
                && this.pub_key == obj.pub_key
                && this.created == obj.created
                && this.file_size == obj.file_size
                && this.item_type == obj.item_type;
        }

        public override bool Equals(object obj) => Equals(obj as StorageObjItem);
        public override int GetHashCode() => (hash, name, signature, pub_key, created, file_size, item_type).GetHashCode();
    }

    public class StorageNumberItem : StorageItem
    {
        public StorageNumberItem()
        {

        }

      //  public new ObjectId Id { get; set; }
        /// <summary>
        /// Only Get to get_content
        /// </summary>
        public double amount { get; set; }
        public override StorageItemType item_type { get; set; } = StorageItemType.NumberBase;

        /// <summary>
        /// Only Set to amount
        /// </summary>
        [BsonIgnore]
        public override string get_content { get => amount.ToString();}

        public void Read(Reader reader)
        {
            amount = reader.ReadDouble();
           
        }

        public void Write(Writer writer)
        {
            writer.Write(amount);
        }
        //BsonMapper.Global.RegisterType<Uri>
        //(
        //     serialize: (uri) => uri.AbsoluteUri,
        //     deserialize: (bson) => new Uri(bson.AsString)
        //);

        public new bool Equals(IStorageItem other)
        {
            var obj = other as StorageNumberItem;
            if (other is null)
                return false;

            return this.amount == obj.amount && this.item_type == obj.item_type
                && this.get_content == obj.get_content;
        }

        public override bool Equals(object obj) => Equals(obj as StorageNumberItem);
        public override int GetHashCode() => (amount, item_type, get_content).GetHashCode();

    }
}

