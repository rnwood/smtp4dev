using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCard delivery address implementation.
    /// </summary>
    public class DeliveryAddress
    {
        private Item                     m_pItem             = null;
        private DeliveryAddressType_enum m_Type              = DeliveryAddressType_enum.Ineternational | DeliveryAddressType_enum.Postal | DeliveryAddressType_enum.Parcel | DeliveryAddressType_enum.Work;
        private string                   m_PostOfficeAddress = "";
        private string                   m_ExtendedAddress   = "";
        private string                   m_Street            = "";
        private string                   m_Locality          = "";
        private string                   m_Region            = "";
        private string                   m_PostalCode        = "";
        private string                   m_Country           = "";
                
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="item">Owner vCard item.</param>
        /// <param name="addressType">Address type. Note: This value can be flagged value !</param>
        /// <param name="postOfficeAddress">Post office address.</param>
        /// <param name="extendedAddress">Extended address.</param>
        /// <param name="street">Street name.</param>
        /// <param name="locality">Locality(city).</param>
        /// <param name="region">Region.</param>
        /// <param name="postalCode">Postal code.</param>
        /// <param name="country">Country.</param>
        internal DeliveryAddress(Item item,DeliveryAddressType_enum addressType,string postOfficeAddress,string extendedAddress,string street,string locality,string region,string postalCode,string country)
        {
            m_pItem             = item;
            m_Type              = addressType;
            m_PostOfficeAddress = postOfficeAddress;
            m_ExtendedAddress   = extendedAddress;
            m_Street            = street;
            m_Locality          = locality;
            m_Region            = region;
            m_PostalCode        = postalCode;
            m_Country           = country;
        }


        #region method Changed

        /// <summary>
        /// This method is called when some property has changed, we need to update underlaying vCard item.
        /// </summary>
        private void Changed()
        {
            string value = "" +
                m_PostOfficeAddress + ";" +
                m_ExtendedAddress + ";" +
                m_Street + ";" +
                m_Locality + ";" +
                m_Region + ";" +
                m_PostalCode + ";" +
                m_Country;

            m_pItem.ParametersString = AddressTypeToString(m_Type);
            m_pItem.SetDecodedValue(value);
        }

        #endregion


        #region internal static method Parse

        /// <summary>
        /// Parses delivery address from vCard ADR structure string.
        /// </summary>
        /// <param name="item">vCard ADR item.</param>
        internal static DeliveryAddress Parse(Item item)
        {
            DeliveryAddressType_enum type = DeliveryAddressType_enum.NotSpecified;
            if(item.ParametersString.ToUpper().IndexOf("PREF") != -1){
                type |= DeliveryAddressType_enum.Preferred;
            }
            if(item.ParametersString.ToUpper().IndexOf("DOM") != -1){
                type |= DeliveryAddressType_enum.Domestic;
            }
            if(item.ParametersString.ToUpper().IndexOf("INTL") != -1){
                type |= DeliveryAddressType_enum.Ineternational;
            }
            if(item.ParametersString.ToUpper().IndexOf("POSTAL") != -1){
                type |= DeliveryAddressType_enum.Postal;
            }
            if(item.ParametersString.ToUpper().IndexOf("PARCEL") != -1){
                type |= DeliveryAddressType_enum.Parcel;
            }
            if(item.ParametersString.ToUpper().IndexOf("HOME") != -1){
                type |= DeliveryAddressType_enum.Home;
            }
            if(item.ParametersString.ToUpper().IndexOf("WORK") != -1){
                type |= DeliveryAddressType_enum.Work;
            }

            string[] items = item.DecodedValue.Split(';');            
            return new DeliveryAddress(
                item,
                type,
                items.Length >= 1 ? items[0] : "",
                items.Length >= 2 ? items[1] : "",
                items.Length >= 3 ? items[2] : "",
                items.Length >= 4 ? items[3] : "",
                items.Length >= 5 ? items[4] : "",
                items.Length >= 6 ? items[5] : "",
                items.Length >= 7 ? items[6] : ""
            );
        }

        #endregion

        #region internal static AddressTypeToString

        /// <summary>
        /// Converts DeliveryAddressType_enum to vCard item parameters string.
        /// </summary>
        /// <param name="type">Value to convert.</param>
        /// <returns></returns>
        internal static string AddressTypeToString(DeliveryAddressType_enum type)
        {
            string retVal = "";
            if((type & DeliveryAddressType_enum.Domestic) != 0){
                retVal += "DOM,";
            }
            if((type & DeliveryAddressType_enum.Home) != 0){
                retVal += "HOME,";
            }
            if((type & DeliveryAddressType_enum.Ineternational) != 0){
                retVal += "INTL,";
            }
            if((type & DeliveryAddressType_enum.Parcel) != 0){
                retVal += "PARCEL,";
            }
            if((type & DeliveryAddressType_enum.Postal) != 0){
                retVal += "POSTAL,";
            }
            if((type & DeliveryAddressType_enum.Preferred) != 0){
                retVal += "Preferred,";
            }            
            if((type & DeliveryAddressType_enum.Work) != 0){
                retVal += "Work,";
            }
            if(retVal.EndsWith(",")){
                retVal = retVal.Substring(0,retVal.Length - 1);
            }

            return retVal;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets underlaying vCrad item.
        /// </summary>
        public Item Item
        {
            get{ return m_pItem; }
        }

        /// <summary>
        /// Gets or sets address type. Note: This property can be flagged value !
        /// </summary>
        public DeliveryAddressType_enum AddressType
        {
            get{ return m_Type; }

            set{ 
                m_Type = value; 
                Changed();
            }
        }

        /// <summary>
        /// Gets or sets post office address.
        /// </summary>
        public string PostOfficeAddress
        {
            get{ return m_PostOfficeAddress; }
            
            set{ 
                m_PostOfficeAddress = value; 
                Changed();
            }
        }

        /// <summary>
        /// Gests or sets extended address.
        /// </summary>
        public string ExtendedAddress
        {
            get{ return m_ExtendedAddress; }
            
            set{ 
                m_ExtendedAddress = value; 
                Changed();
            }
        }
        
        /// <summary>
        /// Gets or sets street.
        /// </summary>
        public string Street
        {
            get{ return m_Street; }
            
            set{ 
                m_Street = value; 
                Changed();
            }
        }
        
        /// <summary>
        /// Gets or sets locality(city).
        /// </summary>
        public string Locality
        {
            get{ return m_Locality; }

            set{ 
                m_Locality = value; 
                Changed();
            }
        }
        
        /// <summary>
        /// Gets or sets region.
        /// </summary>
        public string Region
        {
            get{ return m_Region; }
            
            set{ 
                m_Region = value; 
                Changed();
            }
        }
        
        /// <summary>
        /// Gets or sets postal code.
        /// </summary>
        public string PostalCode
        {
            get{ return m_PostalCode; }
            
            set{ 
                m_PostalCode = value; 
                Changed();
            }
        }
        
        /// <summary>
        /// Gets or sets country.
        /// </summary>
        public string Country
        {
            get{ return m_Country; }
            
            set{ 
                m_Country = value; 
                Changed();
            }
        }

        #endregion

    }
}
