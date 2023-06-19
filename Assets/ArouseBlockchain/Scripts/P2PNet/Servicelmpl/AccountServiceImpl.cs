using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.NetService;
using ArouseBlockchain.P2PNet;
using Newtonsoft.Json;

namespace ArouseBlockchain.P2PNet
{
    public class AccountServiceImpl : NetAccount.AccountServer
    {
        public override Task<Account> Add(Account account, ServerCallContext callContext)
        {
            throw new NotImplementedException();
        }

        public override Task<Account> GetByAddress(Account account, ServerCallContext callContext)
        {
            throw new NotImplementedException();
        }

        public override Task<Account> GetByPubkey(Account account, ServerCallContext callContext)
        {
            throw new NotImplementedException();
        }

        public override Task<AccountList> GetRange(AccountParams accountParams, ServerCallContext callContext)
        {
            throw new NotImplementedException();
        }

        public override Task<Account> Update(Account account, ServerCallContext callContext)
        {
            throw new NotImplementedException();
        }
    }
}