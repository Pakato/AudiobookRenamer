using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioBookManager.Core
{
    public class NumericComparer<T> : IComparer<T>
    {
        public NumericComparer()
        { }


        public int Compare(T x, T y)
        {
            if ((x is string) && (y is string))
            {
                return StringLogicalComparer.Compare(x as string, y as string);
            }
            return -1;
        }
    }
}
