using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace maya
{
    [Serializable]
    class DataUnit
    {
        public double[] da;
        public DataUnit()
        {
            da = new double[2068];
        }
        public DataUnit(double[] data)
        {
            this.da = data;
        }
    }    
}
