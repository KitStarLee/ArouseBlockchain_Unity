using System;
using System.Security.Cryptography;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Crypto;
using ArouseBlockchain.Common;
using System.Text;
using System.IO;
using ArouseBlockchain.NetService;
using UnityEngine;
using Newtonsoft.Json;
using static ArouseBlockchain.Common.AConfig;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArouseBlockchain.P2PNet;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Reflection;

namespace ArouseBlockchain.Solve
{
    public class KeyPair
    {
        //私钥
        public ExtKey PrivateKey { set; get; }
        //公钥
        public PubKey PublicKey { set; get; }
        //公钥Hex
        public string PublicKeyHex { set; get; }
        /// <summary>
        /// 在公链上检查是否有这个用户
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckInChain()
        {
            await Task.Delay(3000);
            return true;
        }

        public KeyPair Apply(ExtKey upperPrivateKey, KeyPath _Path = null)
        {
            PrivateKey = (_Path == null) ? upperPrivateKey.Derive(0824, true) : upperPrivateKey.Derive(_Path);
            PublicKey = PrivateKey.GetPublicKey();
            PublicKeyHex = PublicKey.Hash.ToString();

            return this;
        }
    }

    public struct AKeyPath
    {
        public byte Account;
        public int Change;
        public int AddressIndex;
    }

    public class WalletCard
    {

        //master Key
        private KeyPair MainKeyPair;

        //助记词
        private string Passphrase;

        private Mnemonic m_Mnemonic;
        private Account _Account;
        bool _IsAutoAddToDb = true;

        public WalletCard(bool isAutoAddToDb = true)
        {
            _IsAutoAddToDb = isAutoAddToDb;
        }


        /// <summary>
        /// Creat a Wallet Card
        /// 创建钱包获取助记词
        /// </summary>
        /// <returns>Returns a mnemonic, and it is also the password of your wallet card</returns>
        public async Task<(string, KeyPair)> CreatCard()
        {
            StringBuilder str_mnemonic = new StringBuilder();

            return await Task.Run(() =>
            {
                m_Mnemonic = new Mnemonic(Wordlist.ChineseSimplified, WordCount.Twelve);

                GetMasterKeyPair(m_Mnemonic);
                for (int i = 0; i < m_Mnemonic.Words.Length; i++)
                {
                    str_mnemonic.Append(m_Mnemonic.Words[i]);
                }

                //更具用户
                //1. 交易：返回对外首款的共钥
                //2. 挖矿：返回需要验证的共钥
                if (string.IsNullOrEmpty(_Account.address)) _Account = SolveManager.BlockChain.WalletAccount.GetByAddress(GetAccountAddress());
                if (MainKeyPair != null && string.IsNullOrEmpty(_Account.address) && _IsAutoAddToDb) SolveManager.BlockChain.WalletAccount.AddAccount(this);
                return (str_mnemonic.ToString(), MainKeyPair);
            });

        }

        /// <summary>
        /// 通过助记词获取用户Key钥匙链
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <returns></returns>
        public KeyPair GetMasterKeyPair(string mnemonic)
        {
            try
            {
                mnemonic = string.Join(" ", mnemonic.ToCharArray());
                //  ALog.Log("passphrase is : " + mnemonic);
                m_Mnemonic = new Mnemonic(mnemonic, Wordlist.ChineseSimplified);
                return GetMasterKeyPair(m_Mnemonic);
            }
            catch (Exception ex)
            {
                ALog.Error("Get KeyPair Error : " + ex);
            }

            return null;

        }

        KeyPair GetMasterKeyPair(Mnemonic mnemonic)
        {
            ExtKey masterKey = mnemonic.DeriveExtKey();
            ExtPubKey masterPubKey = masterKey.Neuter();

            MainKeyPair = new KeyPair().Apply(masterKey);
            //  ALog.Log("[Self] new " + "\n" + MainKeyPair.PrivateKey.GetPublicKey().GetAddress(ScriptPubKeyType.TaprootBIP86, NBitcoin.Network.Main) + "\n" + MainKeyPair.PublicKey.GetAddress(ScriptPubKeyType.TaprootBIP86, NBitcoin.Network.Main));

            if (_Account.Equals(null)) _Account = SolveManager.BlockChain.WalletAccount.GetByAddress(GetAccountAddress());
            if (MainKeyPair != null && _Account.Equals(null) && _IsAutoAddToDb) SolveManager.BlockChain.WalletAccount.AddAccount(this);
            return MainKeyPair;
        }


        /// <summary>
        /// 通过助记词获取子级的KeyPair
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="account"></param>
        /// <param name="change"></param>
        /// <param name="address_index"></param>
        /// <returns></returns>
        public KeyPair GetHDKeyPair(string mnemonic, AKeyPath aKeyPath)
        {
            try
            {
                if (MainKeyPair is null) MainKeyPair = GetMasterKeyPair(mnemonic);
                return GetHDKeyPair(MainKeyPair, aKeyPath);
            }
            catch (Exception ex)
            {
                ALog.Error("Get KeyPair Error : " + ex);
            }

            return null;

        }


        KeyPair GetHDKeyPair(KeyPair KeyPair, AKeyPath aKeyPath)
        {

            //BIP44规范:KeyPath =  m / purpose’ / coin_type’ / account’ / change / address_index
            //account : 比如用于捐赠的，用于挖矿的，用于消费的等等，根据个人喜好设置。
            //change: 用于生成找零地址。
            //m表示私钥。M表示公钥。


            //self: m/44'/1994'/0'/0/0
            //BTC :m/44'/0'/0'/0/0
            KeyPath _Path = new KeyPath(string.Format("m/44'/{0}'/{1}'/{2}/{3}", 1994, aKeyPath.Account, aKeyPath.Change, aKeyPath.AddressIndex));

            //主钥不在的情况下，需要先登陆钱包
            var keyPair = new KeyPair().Apply(KeyPair.PrivateKey, _Path);

            ALog.Log("[Self]" + "\n" + keyPair.PublicKey.GetAddress(ScriptPubKeyType.TaprootBIP86, NBitcoin.Network.Main) +
                "\n" + keyPair.PrivateKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.TaprootBIP86, NBitcoin.Network.Main));

            // BitcoinSecret btcPrivKey = privateKeyDer.PrivateKey.GetWif(Network.Main);
            //BitcoinAddress btcPrivKeyAddr = btcPrivKey.GetAddress(ScriptPubKeyType.TaprootBIP86);
            //BitcoinSecret btcPrivKey = publicKeyDer.PubKey.(Network.Main);


            //需要判断一下通过这个助记词获取的比特币主网的地址是否一直是唯一的
            // string baddress = publicKeyDer.GetAddress(ScriptPubKeyType.TaprootBIP86, Network.Main).ToString();

            // string selfbaddress = GetAddress(publicKeyDer);

            // ALog.Log("[Self] privateKeyDer: " + privateKeyDer.PrivateKey.ToString(Network.Main) + "\n" + "publicKeyDer: " + publicKeyDer.ToString(Network.Main) + "\n" + "publicKeyHex: " + publicKeyHex + "\n" + "selfbaddress: " + selfbaddress);
            // ALog.Log("[Bit] btcPrivKey: " + btcPrivKey.ToString() + "\n" + "btcPrivKeyAddr: " + btcPrivKeyAddr.ToString() + "\n" + "baddress: " + baddress);

            //var ss = new Key().PubKey.Address.ToString();



            return keyPair;
        }



        /// <summary>
        /// 获取公钥匙，如果参数都为0，则获取的是主钥的公钥匙
        /// </summary>
        /// <param name="account"></param>
        /// <param name="change"></param>
        /// <param name="address_index"></param>
        /// <returns></returns>
        public PubKey GetPublicKey(AKeyPath aKeyPath = default)
        {
            return GetKeyPair(aKeyPath)?.PublicKey;
        }

        public string GetPublicKeyHex(AKeyPath aKeyPath = default)
        {
            return GetKeyPair(aKeyPath)?.PublicKeyHex;
        }

        // <summary>
        /// 获取钥匙链，如果参数都为0，则获取的是主钥的钥匙链
        /// </summary>
        /// <param name="account"></param>
        /// <param name="change"></param>
        /// <param name="address_index"></param>
        /// <returns></returns>
        public KeyPair GetKeyPair(AKeyPath aKeyPath = default)
        {
            if (MainKeyPair == null)
            {
                ALog.Error("没有主键，建议先创建钱包");
                return null;
            }

            if (aKeyPath.Account <= 0 && aKeyPath.Change <= 0 && aKeyPath.AddressIndex <= 0) return MainKeyPair;
            return GetHDKeyPair(MainKeyPair, aKeyPath);
        }

        public string GetAccountAddress(AKeyPath aKeyPath = default)
        {
            KeyPair keyPair = GetKeyPair(aKeyPath);
            return WalletsSolve.GetWalletAddress(keyPair.PublicKey);
        }


        public CompactSignature Sign(string dataHash, AKeyPath aKeyPath = default)
        {
            KeyPair keyPair = GetKeyPair(aKeyPath);
            return WalletsSolve.Sign(keyPair.PrivateKey.PrivateKey, dataHash);
        }


        public static bool CheckSignature(string publicKeyHex, CompactSignature signature, string dataHash)
        {
            var pubKey = new PubKey(publicKeyHex);

            uint256 hash = Hashes.DoubleSHA256(AUtils.BC.GenHashBytes(dataHash));
            return pubKey.Verify(hash, signature.Signature);
        }

    }

    /// <summary>
    /// 负责创建账号（私钥、公钥、地址、签名）
    /// </summary>
    public class WalletsSolve : ISolve
    {
        List<WalletCard> WalletCards = new List<WalletCard>();

        WalletCard _WalletCard;

        public event Action<Account> OnSelectAccount;

        public WalletsSolve()
        {

        }

        public async UniTask<bool> Start(CancellationTokenSource _DCTS)
        {
            Console.WriteLine("... Wallet service is starting");
            // Mnemonic = new Mnemonic(Passphrase);
            // KeyPair = GenerateKeyPair(Mnemonic, 0);
            Console.WriteLine("...... Wallet service is ready");

            //if (IsAutoSave) //默认保存用来，对下次
            //{
            List<string> mnem = LoadUserWalletMnemonics();
            if (mnem == null || mnem.Count <= 0) return true;
            ALog.Log("----- 加载缓存的本地钱包 ----- \n" + mnem.Count);
            mnem.ForEach((m) =>
            {

                WalletCard card = GetUserWalletCard(m);
                if (card != null)
                {
                    if (!WalletCards.Exists(w => (w.GetPublicKeyHex() == card.GetPublicKeyHex())))
                        WalletCards.Add(card);
                    AUtils.BC.PrintWallet(card);

                    ALog.Log("add :{0} \n pri :{1} \n pri :{2}", card.GetAccountAddress(), card.GetKeyPair().PrivateKey.ToString(NBitcoin.Network.Main), card.GetKeyPair().PrivateKey.PrivateKey.ToString(NBitcoin.Network.Main));
                    //第一个为默认钱包，未来还需要用户自己选择
                   // if (_WalletCard == null) _WalletCard = card;
                }

            });

            //}

            return true;
        }

        public void Stop() { }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>one=Mnemonic ; two = address</returns>
        public async Task<(string, KeyPair)> CreatUserWalletCard()
        {
            WalletCard card = new WalletCard();
            //创建的钱包应该默认为返回： 助记词、对外首款ID
            await Task.Delay(UnityEngine.Random.Range(2000, 4000));
            WalletCards.Add(card);
            return await card.CreatCard();
        }

        public WalletCard GetUserWalletCard(string passphrase)
        {
            WalletCard card = new WalletCard();

            //需要进行验证，确定无误在返回,不能其他地方的私钥也进来

            try
            {
                KeyPair keyPair = card.GetMasterKeyPair(passphrase);
                WalletCards.Add(card);
                return card;
            }
            catch (Exception ex)
            {
                ALog.Log("GetUserWalletCard Error : " + ex);
                return null;
            }
        }

        public Account[] GetUserAllWalletAccount()
        {
            if (WalletCards.Count <= 0)
                return null;

            Account[] accounts = new Account[WalletCards.Count];
            for (int i = 0; i < WalletCards.Count; i++)
            {
                accounts[i] = new Account();
                accounts[i].pub_key = WalletCards[i].GetPublicKey().ToString();
                accounts[i].address = WalletCards[i].GetAccountAddress();
            }
            return accounts;
        }

        public WalletCard SelectUserWalletCard(int index)
        {
            if (index >= WalletCards.Count)
                return null;

            _WalletCard = WalletCards[index];
            string address = _WalletCard.GetAccountAddress();
            var currAccount = SolveManager.BlockChain.WalletAccount.GetByAddress(address);
            OnSelectAccount?.Invoke(currAccount);

            return _WalletCard;
        }

        public WalletCard SelectUserWalletCard(PubKey publicKey)
        {
            var cards = WalletCards.Find(x => x.GetPublicKey() == publicKey);

            if (cards == null) {
                ALog.Error("没有这个公钥的Card {0} ", publicKey.ToString()) ;
                return null;
            }
              
            string address = cards.GetAccountAddress();
            var currAccount = SolveManager.BlockChain.WalletAccount.GetByAddress(address);
            OnSelectAccount?.Invoke(currAccount);

            return _WalletCard;
        }

        public int GetWalletCardCount => WalletCards.Count;
        public WalletCard GetCurrWalletCard => _WalletCard;

        public string GetCurrAddress
        {
            get
            {
                if (_WalletCard == null) ALog.Error("当前 _WalletCard 是空的");
                return _WalletCard?.GetAccountAddress();
            }
        }
        public CompactSignature Sign(string dataHash)
        {
            return _WalletCard?.Sign(dataHash);
        }




        public bool IsAutoSave
        {
            set
            {
                string prefkey = "&IsSave&Mnemonic";
                PlayerPrefs.SetInt(prefkey, (value) ? 5 : 0);
                if (!value) PlayerPrefs.DeleteKey(prefkey);
            }
            get
            {

                int con = PlayerPrefs.GetInt("&IsSave&Mnemonic", 0);
                return (con == 5) ? true : false;
            }
        }



        public List<string> LoadUserWalletMnemonics()
        {
            //if (!IsAutoSave) return null;

            string mnemonicJson = PlayerPrefs.GetString("&Mnemonic&");
            if (!string.IsNullOrEmpty(mnemonicJson))
            {
                List<string> mnemonics = new List<string>();
                return JsonConvert.DeserializeObject<List<string>>(mnemonicJson);
            }

            return null;
        }

        /// <summary>
        /// 毕竟是实现性项目，可以帮助用户保存助记词在本地，如果用户同意
        /// 需要提醒用户，铭文加密，如果不是玩玩的心态最好不要保存到本地
        /// </summary>
        /// <returns></returns>
        public bool SavaUserWalletMnemonic(string mnemonic)
        {
            string mnemonicJson = PlayerPrefs.GetString("&Mnemonic&");
            List<string> mnemonics = new List<string>();
            if (!string.IsNullOrEmpty(mnemonicJson))
            {
                mnemonics = JsonConvert.DeserializeObject<List<string>>(mnemonicJson);
            }
            mnemonics.Add(mnemonic);

            string newMnemonics = JsonConvert.SerializeObject(mnemonics);
            PlayerPrefs.SetString("&Mnemonic&", newMnemonics);
            // ALog.Log("Mnemonic : " + PlayerPrefs.GetString("&Mnemonic&") + " ::: " + newMnemonics);
            return true;

        }


        public static string GetWalletAddress(NBitcoin.PubKey publicKey)
        {
            byte[] hash = SHA256.Create().ComputeHash(publicKey.ToBytes());
            return Encoders.Base58.EncodeData(hash);
        }
        public static CompactSignature Sign(NBitcoin.Key privateKey, string dataHash)
        {
            // KeyPair keyPair = GetKeyPair(aKeyPath);

            uint256 hash = Hashes.DoubleSHA256(AUtils.BC.GenHashBytes(dataHash));

            return privateKey.SignCompact(hash);
        }

    }
}
