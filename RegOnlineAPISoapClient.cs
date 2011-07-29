using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace RegOnline.ConsoleAPISample.RegOnlineAPIProxy
{
    /// <summary>
    /// RegOnline API soap client
    /// </summary>
    public partial class RegOnlineAPISoapClient : IDisposable
    {
        /// <summary>
        /// Properly disposes of the WCF client, taking exception handling 
        /// based on State into consideration.
        /// </summary>
        public void Dispose()
        {
            if (this.State == CommunicationState.Faulted)
            {
                this.Abort();
            }
            else
            {
                this.Close();
            }
        }
    }
}
