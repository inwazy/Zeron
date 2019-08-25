using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeron.Demand.Core.Common
{
    /// <summary>
    /// ZmqRepTask
    /// </summary>
    abstract class ZmqRepTask
    {
        /// <summary>
        /// OnResponse
        /// </summary>
        /// <returns>Returns void.</returns>
        protected abstract void OnResponse();
    }
}
