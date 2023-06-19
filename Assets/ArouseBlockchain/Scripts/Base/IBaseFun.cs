using System;
using System.Threading.Tasks;

namespace ArouseBlockchain
{
    public enum OperatorTye
    {
        Add,
        Reduce
    }

    public interface IBaseFun
    {
        Task<bool> Init();
        void Close();
    }

}