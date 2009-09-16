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
//   Foundation, Inc., 51 Franklin Street, Fifth Floor,
//   Boston, MA  02110-1301  USA
//
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace anmar.SharpMimeTools
{
	/// <summary>
	/// Decodes <a href="http://msdn.microsoft.com/library/en-us/mapi/html/ca148ec3-8586-4c74-8ff8-cd542256e385.asp">ms-tnef</a> streams (those winmail.dat attachments). 
	/// </summary>
	/// <remarks>Only tnef attributes related to attachments are decoded right now. All the MAPI properties encoded in the stream (rtf body, ...) are ignored. <br />
	/// While decoding, the cheksum of each attribute is verified to ensure the tnef stream is not corupt.</remarks>
	public class SharpTnefMessage {
#if LOG
		private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
		private System.IO.BinaryReader _reader;
		private List<SharpAttachment> _attachments;
		private System.String _body;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="anmar.SharpMimeTools.SharpTnefMessage" /> class based on the supplied <see cref="System.IO.Stream" />.
		/// </summary>
		/// <param name="input"><see cref="System.IO.Stream" /> that contains the ms-tnef strream.</param>
		/// <remarks>The tnef stream isn't automatically parsed, you must call <see cref="Parse()" /> or <see cref="Parse(string)" />.</remarks>
		public SharpTnefMessage ( System.IO.Stream input ) {
			if ( input!=null && input.CanRead ) {
				if ( input is System.IO.BufferedStream )
					this._reader = new System.IO.BinaryReader(input);
				else
					this._reader = new System.IO.BinaryReader(new System.IO.BufferedStream(input));
			}
		}
		
		/// <summary>
		/// Gets a <see cref="System.Collections.ArrayList" /> instance that contains the attachments found in the tnef stream.
		/// </summary>
		/// <value><see cref="System.Collections.ArrayList" /> instance that contains the <see cref="SharpAttachment" /> found in the tnef stream. The <b>null</b> reference is retuned when no attachments found.</value>
		/// <remarks>Each attachment is a <see cref="SharpAttachment" /> instance.</remarks>
		public List<SharpAttachment> Attachments {
			get { return this._attachments; }
		}
		/// <summary>
		/// Gets a the text body from the ms-tnef stream (<b>BODY</b> tnef attribute).
		/// </summary>
		/// <value>Text body from the ms-tnef stream (<b>BODY</b> tnef attribute). Or the <b>null</b> reference if the attribute is not part of the stream.</value>
		public System.String TextBody {
			get { return this._body; }
		}
		/// <summary>
		/// Closes and releases the reading resources associated with this instance. 
		/// </summary>
		/// <remarks>Be carefull before calling this method, as it also close the underlying <see cref="System.IO.Stream" />.</remarks>
		public void Close () {
			if ( this._reader!=null )
				this._reader.Close();
			this._reader = null;
		}
		/// <summary>
		/// Parses the ms-tnef stream from the provided <see cref="System.IO.Stream" />.
		/// </summary>
		/// <returns><b>true</b> if parsing is successful. <b>false</b> otherwise.</returns>
		/// <remarks>The attachments found are saved in memory as <see cref="System.IO.MemoryStream" /> instances.</remarks>
		public bool Parse () {
			return this.Parse(null);
		}
		/// <summary>
		/// Parses the ms-tnef stream from the provided <see cref="System.IO.Stream" />.
		/// </summary>
		/// <param name="path">A <see cref="System.String" /> specifying the path on which to save the attachments found. Specify the <b>null</b> reference to save them in memory as  <see cref="System.IO.MemoryStream" /> instances instead.</param>
		/// <returns><b>true</b> if parsing is successful. <b>false</b> otherwise.</returns>
		public bool Parse ( System.String path ) {
			if ( this._reader==null || !this._reader.BaseStream.CanRead )
				return false;
			int sig = this.ReadInt();
			if ( sig!=TnefSignature ) {
#if LOG
				if ( logger.IsErrorEnabled )
					logger.Error(System.String.Concat("Tnef signature not matched [", sig, "]<>[", TnefSignature, "]"));
#endif
				return false;
			}
			bool error = false;
			this._attachments = new List<SharpAttachment>();
			ushort key = this.ReadUInt16();
			System.Text.Encoding enc = anmar.SharpMimeTools.SharpMimeHeader.EncodingDefault;
			anmar.SharpMimeTools.SharpAttachment attachment_cur = null; 
			for ( System.Byte cur=this.ReadByte(); cur!=System.Byte.MinValue; cur=ReadByte() ) {
				TnefLvlType lvl = (TnefLvlType)anmar.SharpMimeTools.SharpMimeTools.ParseEnum(typeof(TnefLvlType), cur, TnefLvlType.Unknown);
				// Type
				int type = this.ReadInt();
				// Size
				int size = this.ReadInt();
				// Attibute name and type
				TnefAttribute att_n = (TnefAttribute)anmar.SharpMimeTools.SharpMimeTools.ParseEnum(typeof(TnefAttribute), (ushort)((type<<16)>>16), TnefAttribute.Unknown);
				TnefDataType att_t = (TnefDataType)anmar.SharpMimeTools.SharpMimeTools.ParseEnum(typeof(TnefDataType), (ushort)(type>>16), TnefDataType.Unknown);
				if ( lvl==TnefLvlType.Unknown || att_n==TnefAttribute.Unknown || att_t==TnefDataType.Unknown ) {
#if LOG
				if ( logger.IsErrorEnabled )
					logger.Error(System.String.Concat("Attribute data is not valid [", lvl ,"] [type=", type, "->(", att_n, ",", att_t, ")] [size=", size, "]"));
#endif
					error = true;
					break;
				}
				// Read data
				System.Byte[] buffer = this.ReadBytes(size);
				// Read checkSum
				ushort checksum = this.ReadUInt16();
				// Verify checksum
				if ( !this.VerifyChecksum(buffer, checksum) ) {
#if LOG
				if ( logger.IsErrorEnabled )
					logger.Error(System.String.Concat("Checksum validation failed [", lvl ,"] [type=", type, "->(", att_n, ",", att_t, ")] [size=", size, "(", (buffer!=null)?buffer.Length:0, ")] [checksum=", checksum, "]"));
#endif
					error = true;
					break;
				}
				size = buffer.Length;
#if LOG
				if ( logger.IsDebugEnabled )
					logger.Debug(System.String.Concat("[", lvl ,"] [type=", type, "->(", att_n, ",", att_t, ")] [size=", size, "(", (buffer!=null)?buffer.Length:0, ")] [checksum=", checksum, "]"));
#endif
				if ( lvl==TnefLvlType.Message ) {
					// Text body
					if ( att_n==TnefAttribute.Body ) {
						if ( att_t==TnefDataType.atpString ) {
							this._body = enc.GetString(buffer, 0, size);
						}
					// Message mapi props (html body, rtf body, ...)
					} else if ( att_n==TnefAttribute.MapiProps ) {
						this.ReadMapi(buffer, size);
					// Stream Codepage
					} else if ( att_n==TnefAttribute.OEMCodepage ) {
						uint codepage1 = (uint)(buffer[0] + (buffer[1]<<8)  +(buffer[2]<<16) + (buffer[3]<<24));
						if ( codepage1>0 ) {
							try {
								enc = System.Text.Encoding.GetEncoding((int)codepage1);
#if LOG
								if ( logger.IsDebugEnabled ) {
									logger.Debug(System.String.Concat("Now using [", enc.EncodingName, "] encoding to decode strings."));
								}
#endif
							} catch ( System.Exception ) {}
						}
					}
				} else if ( lvl==TnefLvlType.Attachment ) {
					// Attachment start
					if ( att_n==TnefAttribute.AttachRendData ) {
						System.String name = System.String.Concat("generated_", key, "_", (this._attachments.Count+1), ".binary" );
						if ( path==null ) {
							attachment_cur = new anmar.SharpMimeTools.SharpAttachment(new System.IO.MemoryStream());
						} else {
							attachment_cur = new anmar.SharpMimeTools.SharpAttachment(new System.IO.FileInfo(System.IO.Path.Combine(path, name)));
						}
						attachment_cur.Name = name;
					// Attachment name
					} else if ( att_n==TnefAttribute.AttachTitle ) {
						if ( attachment_cur!=null && att_t==TnefDataType.atpString && buffer!=null ) {
							// NULL terminated string
							if ( buffer[size-1]=='\0' ) {
								size--;
							}
							if ( size>0 ) {
								System.String name = enc.GetString(buffer, 0, size);
								if ( name.Length>0 ) {
									attachment_cur.Name = name;
									// Content already saved, so we have to rename
									if ( attachment_cur.SavedFile!=null && attachment_cur.SavedFile.Exists ) {
										try {
											attachment_cur.SavedFile.MoveTo(System.IO.Path.Combine(path, attachment_cur.Name));
										} catch ( System.Exception ) {}
									}
								}
							}
						}
					// Modification and creation date
					} else if ( att_n==TnefAttribute.AttachModifyDate || att_n==TnefAttribute.AttachCreateDate ) {
						if ( attachment_cur!=null && att_t==TnefDataType.atpDate && buffer!=null && size==14 ) {
							int pos = 0;
							System.DateTime date = new System.DateTime((buffer[pos++]+(buffer[pos++]<<8)), (buffer[pos++]+(buffer[pos++]<<8)), (buffer[pos++]+(buffer[pos++]<<8)), (buffer[pos++]+(buffer[pos++]<<8)), (buffer[pos++]+(buffer[pos++]<<8)), (buffer[pos++]+(buffer[pos++]<<8)));
							if ( att_n==TnefAttribute.AttachModifyDate ) {
								attachment_cur.LastWriteTime = date;
							} else if ( att_n==TnefAttribute.AttachCreateDate ) {
								attachment_cur.CreationTime = date;
							}
						}
					// Attachment data
					} else if ( att_n==TnefAttribute.AttachData ) {
						if ( attachment_cur!=null && att_t==TnefDataType.atpByte && buffer!=null ) {
							if ( attachment_cur.SavedFile!=null ) {
								System.IO.FileStream stream = null;
								try {
									stream = attachment_cur.SavedFile.OpenWrite();
								} catch ( System.Exception e ) {
#if LOG
									if ( logger.IsErrorEnabled )
										logger.Error(System.String.Concat("Error writting file [", attachment_cur.SavedFile.FullName, "]"), e);
#endif
									error = true;
									break;
								}
								stream.Write(buffer, 0, size);
								stream.Flush();
								attachment_cur.Size = stream.Length;
								stream.Close();
								stream = null;
								attachment_cur.SavedFile.Refresh();
								// Is name has changed, we have to rename
								if ( attachment_cur.SavedFile.Name!=attachment_cur.Name )
									try {
										attachment_cur.SavedFile.MoveTo(System.IO.Path.Combine(path, attachment_cur.Name));
									} catch ( System.Exception ) {}
							} else {
								attachment_cur.Stream.Write(buffer, 0, size);
								attachment_cur.Stream.Flush();
								attachment_cur.Size = attachment_cur.Stream.Length;
							}
							this._attachments.Add(attachment_cur);
						}
					// Attachment mapi props
					} else if ( att_n==TnefAttribute.Attachment ) {
						this.ReadMapi(buffer, size);
					}
				}
			}
			if ( this._attachments.Count==0 )
				this._attachments = null;
			return !error;
		}
		
		private System.Byte ReadByte () {
			System.Byte cur;
			try {
				cur = this._reader.ReadByte();
			} catch ( System.Exception ) {
				cur = System.Byte.MinValue;
			}
			return cur;
		}
		private System.Byte[] ReadBytes ( int length ) {
			if ( length<=0 )
				return null;
			System.Byte[] buffer = null;
			try {
				buffer = this._reader.ReadBytes(length);
			} catch ( System.Exception ) {}
			return buffer;
		}
		private int ReadBytesTo ( int length, System.IO.Stream stream ) {
			if ( length<=0 || stream==null || !stream.CanWrite )
				return -1;
			int written=0;
			ushort checksum_calc = 0;
			while ( length>0 ) {
				System.Byte[] buffer = this.ReadBytes((length>4096)?4096:length);
				if ( buffer!=null ) {
					stream.Write(buffer, 0, buffer.Length);
					written+=buffer.Length;
					length-=buffer.Length;
					// Checksum for this data portion
					for ( int i=0, j=buffer.Length; i<j; i++ ) {
						checksum_calc += buffer[i];
					}
				}
			}
			if ( length==0 ) {
				ushort checksum = this.ReadUInt16();
				checksum_calc = (ushort)(checksum_calc%65536);
				if (checksum_calc!=checksum)
					return -2;
			}
			return written;
		}
		private int ReadInt () {
			int cur;
			try {
				cur = this._reader.ReadInt32();
			} catch ( System.Exception ) {
				cur = System.Int32.MinValue;
			}
			return cur;
		}
		private ushort ReadUInt16 () {
			ushort cur;
			try {
				cur = this._reader.ReadUInt16();
			} catch ( System.Exception ) {
				cur = System.UInt16.MinValue;
			}
			return cur;
		}
		private void ReadMapi ( System.Byte[] data, int size ) {
			int pos = 0;
			ushort count = (ushort)(data[pos++]+(data[pos++]<<8));
#if LOG
				if ( logger.IsDebugEnabled )
					logger.Debug(System.String.Concat("[MAPIPROPS] Found [", count ,"]"));
#endif
			if ( count==0 )
				return;
			//FIXME: Read each mapi prop
		}
		private bool VerifyChecksum ( System.Byte[] data, ushort checksum ) {
			if ( data==null )
				return false;
			ushort checksum_calc = 0;
			for ( int i=0, count=data.Length; i<count; i++ ) {
				checksum_calc += data[i];
			}
			checksum_calc = (ushort)(checksum_calc%65536);
			return (checksum_calc==checksum);
		}

		// TNEF signature
		private const int TnefSignature = 0x223e9f78;
		private enum TnefLvlType : byte {
			Message = 0x01,
			Attachment = 0x02,
			Unknown = System.Byte.MaxValue
		}
		// TNEF attributes
		private enum TnefAttribute : ushort {
			Owner                   = 0x0000,
			SentFor                 = 0x0001,
			Delegate                = 0x0002,
			DateStart               = 0x0006,
			DateEnd                 = 0x0007,
			AIDOwner                = 0x0008,
			Requestres              = 0x0009,
			From                    = 0x8000,
			Subject                 = 0x8004,
			DateSent                = 0x8005,
			DateRecd                = 0x8006,
			MessageStatus           = 0x8007,
			MessageClass            = 0x8008,
			MessageId               = 0x8009,
			ParentId                = 0x800a,
			ConversationId          = 0x800b,
			Body                    = 0x800c,
			Priority                = 0x800d,
			AttachData              = 0x800f,
			AttachTitle             = 0x8010,
			AttachMetafile          = 0x8011,
			AttachCreateDate        = 0x8012,
			AttachModifyDate        = 0x8013,
			DateModify              = 0x8020,
			AttachTransportFilename = 0x9001,
			AttachRendData          = 0x9002,
			MapiProps               = 0x9003,
			RecipTable              = 0x9004,
			Attachment              = 0x9005,
			TnefVersion             = 0x9006,
			OEMCodepage             = 0x9007,
			OriginalMessageClass    = 0x9008,
			Unknown                 = System.UInt16.MaxValue
		}
		// TNEF data types
		private enum TnefDataType : ushort {
			atpTriples = 0x0000,
			atpString  = 0x0001,
			atpText    = 0x0002,
			atpDate    = 0x0003,
			atpShort   = 0x0004,
			atpLong    = 0x0005,
			atpByte    = 0x0006,
			atpWord    = 0x0007,
			atpDword   = 0x0008,
			atpMax     = 0x0009,
			Unknown    = System.UInt16.MaxValue
		}
	}
	
}
