// -----------------------------------------------------------------------
//
//   Copyright (C) 2003-2006 Angel Marin
// 
//   This file is part of SharpMimeTools
//
//   SharpMimeTools is free software; you can redistribute it and/or
//   modify it under the terms of the GNU Lesser General Public
//   License as published by the Free Software Foundation; either
//   version 2.1 of the License, or (at your option) any later version.
//
//   SharpMimeTools is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//   Lesser General Public License for more details.
//
//   You should have received a copy of the GNU Lesser General Public
//   License along with SharpMimeTools; if not, write to the Free Software
//   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace anmar.SharpMimeTools
{
	internal class SharpMimeAddressCollection : IEnumerable<SharpMimeAddress> {
		protected List<SharpMimeAddress> list = new List<SharpMimeAddress>();

		public SharpMimeAddressCollection ( System.String text ) {
			System.String[] tokens = anmar.SharpMimeTools.ABNF.address_regex.Split(text);
			foreach ( System.String token in tokens ) {
				if ( anmar.SharpMimeTools.ABNF.address_regex.IsMatch(token ) )
					this.Add ( new anmar.SharpMimeTools.SharpMimeAddress( token ) );
			}
		}
		public anmar.SharpMimeTools.SharpMimeAddress this [ int index ] {
			get {
					return this.Get( index );
			}
		}
		System.Collections.IEnumerator IEnumerable.GetEnumerator() {
			return list.GetEnumerator();
		}

	    public IEnumerator<SharpMimeAddress> GetEnumerator()
	    {
	        return list.GetEnumerator();
	    }

	    public void Add ( anmar.SharpMimeTools.SharpMimeAddress address ) {
			list.Add ( address);
		}
		public anmar.SharpMimeTools.SharpMimeAddress Get ( int index ) {
			return (anmar.SharpMimeTools.SharpMimeAddress) list[index];
		}
		public static anmar.SharpMimeTools.SharpMimeAddressCollection Parse( System.String text ) {
			if ( text == null )
				throw new ArgumentNullException();
			return new anmar.SharpMimeTools.SharpMimeAddressCollection ( text );
		}
		public int Count {
			get {
				return list.Count;
			}
		}
		public override string ToString() {
			System.Text.StringBuilder text = new System.Text.StringBuilder();
			foreach ( anmar.SharpMimeTools.SharpMimeAddress token in list ) {
				text.Append ( token.ToString() );
				if ( token.Length>0 )
					text.Append ("; ");
			}
			return text.ToString(); 
		}
	}
	/// <summary>
	/// rfc 2822 email address
	/// </summary>
	public class SharpMimeAddress {
		private System.String name;
		private System.String address;
		/// <summary>
		/// Initializes a new address from a RFC 2822 name-addr specification string
		/// </summary>
		/// <param name="dir">RFC 2822 name-addr address</param>
		/// 
		public SharpMimeAddress ( System.String dir ) {
			name = anmar.SharpMimeTools.SharpMimeTools.parseFrom ( dir, 1 );
			address = anmar.SharpMimeTools.SharpMimeTools.parseFrom ( dir, 2 );
		}
		/// <summary>
		/// Gets the decoded address or name contained in the name-addr
		/// </summary>
		public System.String this [System.Object key] {
			get {
				if ( key == null ) throw new System.ArgumentNullException();
				switch (key.ToString()) {
					case "0":
					case "name":
						return this.name;
					case "1":
					case "address":
						return this.address;
				}
				return null;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override System.String ToString() {
			if ( this.name.Equals (System.String.Empty ) && this.address.Equals (System.String.Empty ) )
				return "";
			if ( this.name.Equals (System.String.Empty ) )
				return String.Format( "<{0}>", this.address);
			else
				return String.Format( "\"{0}\" <{1}>" , this.name , this.address);
		}
		/// <summary>
		/// Gets the length of the decoded address
		/// </summary>
		public int Length {
			get {
				return this.name.Length + this.address.Length;
			}
		}
	}
}
