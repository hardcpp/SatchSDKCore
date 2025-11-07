using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSC.Pool;

public class ByteArrayPool : IObjectPool<byte[]>
{
    public int CountAll => throw new NotImplementedException();

    public int CountActive => throw new NotImplementedException();

    public int CountInactive => throw new NotImplementedException();

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public byte[] Get()
    {
        throw new NotImplementedException();
    }

    public PooledObject<byte[]> Get(out byte[] p_Element)
    {
        throw new NotImplementedException();
    }

    public void Release(byte[] p_Element)
    {
        throw new NotImplementedException();
    }
}
