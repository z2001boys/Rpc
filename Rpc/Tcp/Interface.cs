using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once IdentifierTypo
namespace Rpc.Tcp
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITcpServer : IDisposable
    {
        /// <summary>
        /// 是否上線
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// 列出所有連線的client
        /// </summary>
        /// <returns></returns>
        IPEndPoint[] ListClient();

        /// <summary>
        /// Port
        /// </summary>
        int Port { get; set; }
        /// <summary>
        /// 上線
        /// </summary>
        void OnLine();
        /// <summary>
        /// 下線
        /// </summary>
        void OffLine();

        /// <summary>
        /// 送出資料
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="data"></param>
        void Send(IPEndPoint remoteEndPoint, byte[] data);

        /// <summary>
        /// 有client連結
        /// </summary>
        event EventHandler<SocketInfoArgs> ClientConnected;

        /// <summary>
        /// 有client斷線
        /// </summary>
        event EventHandler<SocketInfoArgs> ClientDisConnected;

        /// <summary>
        /// 資料進入
        /// </summary>
        event EventHandler<DataInArgs> DataIn;


        /// <summary>
        /// log
        /// </summary>
        event EventHandler<string> Log;

    }


    public interface ITcpClient : IDisposable
    {
        IPAddress Ip { get; set; }
        int Port { get; set; }
        bool IsConnect { get; }
        void Connect();
        void Disconnect();
        void Send(byte[] data);

        event EventHandler<DataInArgs> DataIn;

        event EventHandler Disconnected;

    }

    public class DataInArgs
    {
        public Guid Id;
        public IPEndPoint RemoteEndPoint;
        public byte[] Data;

    }
}
