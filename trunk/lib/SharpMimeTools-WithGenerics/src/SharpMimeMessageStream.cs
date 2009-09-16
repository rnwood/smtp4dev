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

namespace anmar.SharpMimeTools
{
	/// <summary>
	/// </summary>
	internal class SharpMimeMessageStream {
#if LOG
		private static log4net.ILog log  = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
		protected System.IO.Stream stream;
		private System.IO.StreamReader sr;
		private System.Text.Encoding enc;
		protected long initpos;
		protected long finalpos;
		
		private System.String _buf;
		private long _buf_initpos;
		private long _buf_finalpos;

		public SharpMimeMessageStream ( System.IO.Stream stream ) {
			this.stream = stream;
			this.enc = anmar.SharpMimeTools.SharpMimeHeader.EncodingDefault;
			sr = new System.IO.StreamReader ( this.stream, this.enc );
		}
		public SharpMimeMessageStream ( System.Byte[] buffer ) {
			this.stream = new System.IO.MemoryStream(buffer);
			this.enc = anmar.SharpMimeTools.SharpMimeHeader.EncodingDefault;
			sr = new System.IO.StreamReader ( this.stream, this.enc );
		}
		public void Close(){
			this.sr.Close();
		}
		public System.String ReadAll ( ) {
			return this.ReadLines ( this.Position, this.stream.Length );
		}
		public System.String ReadAll ( long start ) {
            return this.ReadLines ( start, this.stream.Length );
		}
		public System.String ReadLine ( ) {
			System.String line = null;
			if ( this._buf!=null ) {
				line = this._buf;
				this.initpos = this._buf_initpos;
				this.finalpos = this._buf_finalpos;
				this._buf = null;
			} else {
				System.Text.StringBuilder sb = new System.Text.StringBuilder(80);
				int ending = 0;
				this.initpos = this.Position;
				for ( int current=sr.Read(); current!=-1; current=sr.Read() ) {
					sb.Append((char)current);
					if ( current=='\r' )
						ending++;
					else if ( current=='\n' ) {
						ending++;
						break;
					}
				}
				// Line ending found
				if ( ending>0 ) {
					// Bytes read
					this.finalpos+=this.enc.GetByteCount(sb.ToString());
					// A single dot is treated as message end
					if ( sb.Length==(1+ending) && sb[0]== '.' )
						sb = null;
					// Undo the double dots
					else if ( sb.Length>(1+ending) && sb[0]=='.' && sb[1]=='.' )
						sb.Remove(0, 1);
					if ( sb!=null )
						line = sb.ToString(0, sb.Length-ending);
				} else {
					// Not line ending found, so we are at the end of the stream
					this.finalpos=this.stream.Length;
					// though at the end of the stream there may be some content
					if ( sb.Length>0 )
						line = sb.ToString();
				}
				sb = null;
			}
			return line;
		}
		public System.String ReadLines ( long start, long end ) {
			return this.ReadLinesSB ( start, end ).ToString();
		}
		public System.Text.StringBuilder ReadLinesSB ( long start, long end ) {
			System.Text.StringBuilder lines = new System.Text.StringBuilder();
			System.String line;
			this.SeekPoint ( start );
			do {
				line = this.ReadLine();
				if ( line!=null ) {
					// TODO: try catch
					if ( lines.Length>0 )
						lines.Append ( ABNF.CRLF );
					lines.Append ( line );
				}
			} while ( line!=null && this.Position!=-1 && this.Position<end );
			this.initpos = start;
			return lines;            
		}
		public void ReadLine_Undo () {
			this.SeekPoint(this.initpos);
			this.finalpos = this.initpos;
		}
		public void ReadLine_Undo ( System.String line ) {
			this._buf_initpos = this.initpos;
			this._buf_finalpos = this.finalpos;
			this._buf = line;
			this.finalpos = this.initpos;
		}
		public System.String ReadUnfoldedLine () {
			long initpos = this.Position;
			System.String first_line = this.ReadLine();
			if ( first_line!=null && first_line.Length>0 ) {
				System.Text.StringBuilder line = null;
				System.String tmpline;
				for ( ;; )  {
					tmpline = this.ReadLine();
					// RFC 2822 - 2.2.3 Long Header Fields
					if ( tmpline!=null && tmpline.Length>0 && (tmpline[0] == ' ' || tmpline[0] == '\t') ) {
						if ( line==null )
							line = new System.Text.StringBuilder(first_line, 72);
						line.Append(tmpline);
					} else {
						this.ReadLine_Undo(tmpline);
						break;
					}
				}
				this.initpos = initpos;
				if ( this.finalpos!=this.initpos ) {
					if ( line==null )
						return first_line;
					else
						return line.ToString();
				} else
					return null;
			}
			return (this.finalpos!=this.initpos)?first_line:null;
		}
		public void SaveTo ( System.IO.Stream stream, long start, long end ) {
			if ( start<0 || stream==null || !stream.CanWrite )
				return;
			this.SeekPoint(start);
			if ( end==-1 ) {
				end = this.stream.Length;
			}
			int n = 0;
			long pending = end-start;
			if ( pending<=0 )
				return;
			byte[] buffer = new byte[(pending>4*1024)?4*1024:pending];
			do {
				n = this.stream.Read(buffer, 0, (pending>buffer.Length)?buffer.Length:(int)pending);
				if ( n>0 ) {
					pending -= n;
					if ( pending==0 ) {
						if ( buffer[n-1]=='\n' ) {
							n--;
						}
						if ( n>0 && buffer[n-1]=='\r' ) {
							n--;
						}
					}
					if ( n>0 )
						stream.Write(buffer, 0, n);
					
				}
			} while ( n>0 );
		}
		public bool SeekLine ( long line ) {
			long linenumber = 0;
			this.SeekOrigin();
			for ( ; linenumber<(line-1) && this.ReadLine()!=null; linenumber++ ){}
			return (linenumber==(line-1))?true:false;
		}
		public void SeekOrigin () {
			this.SeekPoint (0);
		}
		public void SeekPoint ( long point ) {
			if ( this.sr.BaseStream.CanSeek && this.sr.BaseStream.Seek (point, System.IO.SeekOrigin.Begin) != point ) {
#if LOG
				if ( log.IsErrorEnabled) log.Error ("Error while seeking");
#endif
				throw new System.IO.IOException ();
			} else {
				this.sr.DiscardBufferedData();
				this.finalpos = point;
			}
			this._buf = null;
		}
		public System.Text.Encoding Encoding {
			set {
				if ( value!=null && this.enc.CodePage!=value.CodePage ) {
					this.enc = value;
					this.SeekPoint (this.Position);
					sr = new System.IO.StreamReader ( this.stream, this.enc );
				}
			}
		}
		public long Position {
			get { return this.finalpos; }
		}
		public long Position_preRead {
			get { return this.initpos; }
		}
		public System.IO.Stream Stream {
			get { return this.stream; }
		}
	}
}
