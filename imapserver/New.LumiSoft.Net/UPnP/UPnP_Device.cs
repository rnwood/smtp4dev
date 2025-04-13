using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LumiSoft.Net.UPnP
{
    /// <summary>
    /// This class represents UPnP device.
    /// </summary>
    public class UPnP_Device
    {
        private string m_BaseUrl          = "";
        private string m_DeviceType       = "";
        private string m_FriendlyName     = "";
        private string m_Manufacturer     = "";
        private string m_ManufacturerUrl  = "";
        private string m_ModelDescription = "";
        private string m_ModelName        = "";
        private string m_ModelNumber      = "";
        private string m_ModelUrl         = "";
        private string m_SerialNumber     = "";
        private string m_UDN              = "";
        private string m_PresentationUrl  = "";
        private string m_DeviceXml        = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="url">Device URL.</param>
        internal UPnP_Device(string url)
        {
            if(url == null){
                throw new ArgumentNullException("url");
            }

            Init(url);
        }

        #region method Init

        private void Init(string url)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(url);

            StringWriter xmlString = new StringWriter();
            xml.WriteTo(new XmlTextWriter(xmlString));
            m_DeviceXml = xmlString.ToString();

            // Set up namespace manager for XPath   
            XmlNamespaceManager ns = new XmlNamespaceManager(xml.NameTable);   
            ns.AddNamespace("n",xml.ChildNodes[1].NamespaceURI);

            m_BaseUrl          = xml.SelectSingleNode("n:root/n:URLBase",ns).InnerText;
            m_DeviceType       = xml.SelectSingleNode("n:root/n:device/n:deviceType",ns).InnerText;
            m_FriendlyName     = xml.SelectSingleNode("n:root/n:device/n:friendlyName",ns).InnerText;
            m_Manufacturer     = xml.SelectSingleNode("n:root/n:device/n:manufacturer",ns).InnerText;
            m_ManufacturerUrl  = xml.SelectSingleNode("n:root/n:device/n:manufacturerURL",ns).InnerText;
            m_ModelDescription = xml.SelectSingleNode("n:root/n:device/n:modelDescription",ns).InnerText;
            m_ModelName        = xml.SelectSingleNode("n:root/n:device/n:modelName",ns).InnerText;
            m_ModelNumber      = xml.SelectSingleNode("n:root/n:device/n:modelNumber",ns).InnerText;
            m_ModelUrl         = xml.SelectSingleNode("n:root/n:device/n:modelURL",ns).InnerText;
            m_SerialNumber     = xml.SelectSingleNode("n:root/n:device/n:serialNumber",ns).InnerText;
            m_UDN              = xml.SelectSingleNode("n:root/n:device/n:UDN",ns).InnerText;
            m_PresentationUrl  = xml.SelectSingleNode("n:root/n:device/n:presentationURL",ns).InnerText;
        }

        #endregion


        #region Proeprties implementation

        /// <summary>
        /// Gets device base URL.
        /// </summary>
        public string BaseUrl
        {
            get{ return m_BaseUrl; }
        }

        /// <summary>
        /// Gets device type.
        /// </summary>
        public string DeviceType
        {
            get{ return m_DeviceType; }
        }

        /// <summary>
        /// Gets device short name.
        /// </summary>
        public string FriendlyName
        {
            get{ return m_FriendlyName; }
        }

        /// <summary>
        /// Gets manufacturer's name.
        /// </summary>
        public string Manufacturer
        {
            get{ return m_Manufacturer; }
        }

        /// <summary>
        /// Gets web site for Manufacturer.
        /// </summary>
        public string ManufacturerUrl
        {
            get{ return m_ManufacturerUrl; }
        }

        /// <summary>
        /// Gets device long description.
        /// </summary>
        public string ModelDescription
        {
            get{ return m_ModelDescription; }
        }

        /// <summary>
        /// Gets model name.
        /// </summary>
        public string ModelName
        {
            get{ return m_ModelName; }
        }

        /// <summary>
        /// Gets model number.
        /// </summary>
        public string ModelNumber
        {
            get{ return m_ModelNumber; }
        }

        /// <summary>
        /// Gets web site for model.
        /// </summary>
        public string ModelUrl
        {
            get{ return m_ModelUrl; }
        }

        /// <summary>
        /// Gets serial number.
        /// </summary>
        public string SerialNumber
        {
            get{ return m_SerialNumber; }
        }

        /// <summary>
        /// Gets unique device name.
        /// </summary>
        public string UDN
        {
            get{ return m_UDN; }
        }

        // iconList
        // serviceList
        // deviceList

        /// <summary>
        /// Gets device UI url.
        /// </summary>
        public string PresentationUrl
        {
            get{ return m_PresentationUrl; }
        }

        /// <summary>
        /// Gets UPnP device XML description.
        /// </summary>
        public string DeviceXml
        {
            get{ return m_DeviceXml; }
        }

        #endregion
    }
}
