using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP QUOTA entry. Defined in RFC 2087 5.1.
    /// </summary>
    public class IMAP_Quota_Entry
    {
        private string m_ResourceName = "";
        private long   m_CurrentUsage = 0;
        private long   m_MaxUsage     = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="resourceName">Resource limit name.</param>
        /// <param name="currentUsage">Current resourse usage.</param>
        /// <param name="maxUsage">Maximum allowed resource usage.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>resourceName</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_Quota_Entry(string resourceName,long currentUsage,long maxUsage)
        {
            if(resourceName == null){
                throw new ArgumentNullException("resourceName");
            }
            if(resourceName == string.Empty){
                throw new ArgumentException("Argument 'resourceName' value must be specified.","resourceName");
            }

            m_ResourceName = resourceName;
            m_CurrentUsage = currentUsage;
            m_MaxUsage     = maxUsage;
        }


        #region Properties implementation

        /// <summary>
        /// Gets resource limit name.
        /// </summary>
        public string ResourceName
        {
            get{ return m_ResourceName;}
        }

        /// <summary>
        /// Gets current resource usage.
        /// </summary>
        public long CurrentUsage
        {
            get{ return m_CurrentUsage; }
        }

        /// <summary>
        /// Gets maximum allowed resource usage.
        /// </summary>
        public long MaxUsage
        {
            get{ return m_MaxUsage; }
        }

        #endregion
    }
}
