using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;

namespace RemoteAccess
{
    /// <summary>
    /// Interface that can be run over the remote AppDomain boundary.
    /// </summary>
    public interface IRemoteInterface
    {
        object Invoke(string lcMethod, object[] Parameters);
    }

    /// <summary>
    /// Factory class to create objects exposing IRemoteInterface
    /// </summary>
    public class RemoteLoaderFactory : MarshalByRefObject
    {
        private const BindingFlags bfi = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;
        public RemoteLoaderFactory() { }
        public IRemoteInterface Create(string assemblyFile, string typeName, object[] constructArgs)
        {
            return (IRemoteInterface)Activator.CreateInstanceFrom(
                     assemblyFile, typeName, false, bfi, null, constructArgs,
                     null, null, null).Unwrap();
        }

     

        public override object InitializeLifetimeService()
        {
            /*
             *
             * @MatthewLee said:
             *   It's been a long time since this question was asked, but I ran into this today and after a couple hours, I figured it out. 
             * The 5 minutes issue is because your Sponsor which has to inherit from MarshalByRefObject also has an associated lease. 
             * It's created in your Client domain and your Host domain has a proxy to the reference in your Client domain. 
             * This expires after the default 5 minutes unless you override the InitializeLifetimeService() method in your Sponsor class or this sponsor has its own sponsor keeping it from expiring.
             *   Funnily enough, I overcame this by returning Null in the sponsor's InitializeLifetimeService() override to give it an infinite timespan lease, and I created my ISponsor implementation to remove that in a Host MBRO.
             * Source: https://stackoverflow.com/questions/18680664/remoting-sponsor-stops-being-called
            */
            return (null);
        }
    }
}
