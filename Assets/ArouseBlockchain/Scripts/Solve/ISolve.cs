using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace ArouseBlockchain.Solve
{
    public interface ISolve
    {
        public UniTask<bool> Start(CancellationTokenSource _DCTS);
        public void Stop();
    }
}
