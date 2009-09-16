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
	internal class SharpMimeMessageCollection : IEnumerable<SharpMimeMessage> {
		protected anmar.SharpMimeTools.SharpMimeMessage parent;
		protected List<SharpMimeMessage> messages = new List<SharpMimeMessage>();
	
		public SharpMimeMessage this[ int index ] {
			get { return this.Get( index ); }
		}
		public void Add ( anmar.SharpMimeTools.SharpMimeMessage msg ) {
			messages.Add( msg );
		}
		public anmar.SharpMimeTools.SharpMimeMessage Get( int index ) {
			return (anmar.SharpMimeTools.SharpMimeMessage)messages[index];
		}
		
        System.Collections.IEnumerator IEnumerable.GetEnumerator() {
			return messages.GetEnumerator();
		}

	    public IEnumerator<SharpMimeMessage> GetEnumerator()
	    {
            return messages.GetEnumerator();
	    }

	    public void Clear () {
			messages.Clear();
		}
		public int Count {
			get {
				return messages.Count;
			}
		}
		public anmar.SharpMimeTools.SharpMimeMessage Parent {
			get {
				return this.parent;
			}
			set {
				this.parent = value;
			}
		}
	}
}
