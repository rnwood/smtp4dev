using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace LumiSoft.Net.HTTP.Server
{
    /// <summary>
    /// HTTP server component.
    /// </summary>
    public class HTTP_Server : SocketServer
    {
        /// <summary>
		/// Defalut constructor.
		/// </summary>
		public HTTP_Server() : base()
		{
			this.BindInfo = new BindInfo[]{new BindInfo(IPAddress.Any,80,false,null)};
		}


        #region override InitNewSession

		/// <summary>
		/// Initialize and start new session here. Session isn't added to session list automatically, 
		/// session must add itself to server session list by calling AddSession().
		/// </summary>
		/// <param name="socket">Connected client socket.</param>
        /// <param name="bindInfo">BindInfo what accepted socket.</param>
		protected override void InitNewSession(Socket socket,BindInfo bindInfo)
		{/*
            // Check maximum conncurent connections from 1 IP.
            if(m_MaxConnectionsPerIP > 0){
                lock(this.Sessions){
                    int nSessions = 0;
                    foreach(SocketServerSession s in this.Sessions){
                        IPEndPoint ipEndpoint = s.RemoteEndPoint;
                        if(ipEndpoint != null){
                            if(ipEndpoint.Address.Equals(((IPEndPoint)socket.RemoteEndPoint).Address)){
                                nSessions++;
                            }
                        }

                        // Maximum allowed exceeded
                        if(nSessions >= m_MaxConnectionsPerIP){
                            socket.Send(System.Text.Encoding.ASCII.GetBytes("-ERR Maximum connections from your IP address is exceeded, try again later !\r\n"));
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                            return;
                        }
                    }
                }
            }*/

            string   sessionID = Guid.NewGuid().ToString();
            SocketEx socketEx  = new SocketEx(socket);
            if(LogCommands){
                //socketEx.Logger = new SocketLogger(socket,this.SessionLog);
				//socketEx.Logger.SessionID = sessionID;
            }
			HTTP_Session session = new HTTP_Session(sessionID,socketEx,bindInfo,this);
		}

		#endregion

    }
}
