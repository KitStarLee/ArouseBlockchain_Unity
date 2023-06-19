using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArouseBlockchain.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ArouseBlockchain.Solve
{
	public class SolveManager
	{
        static SolveManager init;
        public static SolveManager Init {
            get {
                if (init == null)
                {
                    init = new SolveManager();
                }
                return init;
            }
        }

        public static MintingSolve Minting { set; get; }
        public static WalletsSolve Wallets { set; get; }
        public static BlockChainSolve BlockChain { set; get; }

        DBSolve DB;

        static List<ISolve> services = new List<ISolve>();

        public  void Add(params ISolve[] solve)
        {
            foreach (var item in solve)
            {

                if (services.Contains(item))
                {
                    ALog.Error("请勿重复添加服务！");
                    continue;
                }

                switch (item)
                {
                    case MintingSolve M:
                        Minting = M as MintingSolve;
                        break;
                    case WalletsSolve W:
                        Wallets = W as WalletsSolve;
                        break;
                    case BlockChainSolve B:
                        BlockChain = B as BlockChainSolve;
                        break;
                    case DBSolve D:
                        DB = D as DBSolve;
                        break;
                    default:
                        break;
                }

                services.Add(item);
            }
           
        }

        
        public async static UniTask<bool> Start(CancellationTokenSource tokenSource)
        {
            if (tokenSource == null || tokenSource.IsCancellationRequested) return false;
           
            foreach (var item in services)
            {
                if (item != null)
                {
                   bool isOK = await item.Start(tokenSource);
                   await UniTask.DelayFrame(50);
                }
                else Debug.LogError(item + " is NUll");

            }

            return true;
           
        }
        public static void Stop()
        {
            foreach (var item in services)
            {
                if (item != null) item.Stop();
                else Debug.LogError(item + " is NUll");
            }
            
        }
    }
}
