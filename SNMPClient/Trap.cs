using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using SnmpSharpNet;
using System.Windows.Forms;

namespace SNMPClient
{
    class Trap
    {
        enum Generic
        {
            ColdStart, WarmStart, LinkDown, LinkUp, AuthenticationFailure, EGPNeithbourLoss, Other
        };
        Form1 form;
        Socket mySocket;
        IPEndPoint myIpEndPoint;
        IPEndPoint trapSenderIPEndPoint;
        EndPoint trapSenderEndPoint;
        byte[] buffer;
        int myPort;
        int trapSenderPort;
        public Trap(Form1 form)
        {
            myPort = 163;
            trapSenderPort = 163;
            this.form = form;
            InitializeSocket();
        }
        private void InitializeSocket()
        {
            try
            {
                mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                myIpEndPoint = new IPEndPoint((IPAddress.Any), myPort);
                mySocket.Bind(myIpEndPoint);
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            }
            catch
            {
            }

            try
            {

                trapSenderIPEndPoint = new IPEndPoint((IPAddress.Any), trapSenderPort);
                trapSenderEndPoint = (EndPoint)trapSenderIPEndPoint;
            }
            catch
            {
            }
            buffer = new byte[16*1024];
            mySocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref trapSenderEndPoint, new AsyncCallback(ReceivedPacket), null);
        }
        public void ReceivedPacket(IAsyncResult res)
        {
            int size;
            try
            {
                size = mySocket.EndReceiveFrom(res, ref trapSenderEndPoint);
            }
            catch
            {
                mySocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref trapSenderEndPoint, new AsyncCallback(ReceivedPacket), null);
                return;
            }
            byte[] receivedData = new byte[size];
            Array.Copy(buffer, receivedData, receivedData.Length);
            ProcessReceivedData(receivedData, size);
            IPEndPoint receivedIPEndPoint = (IPEndPoint)trapSenderEndPoint;
            buffer = new byte[16 * 1024];
            mySocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref trapSenderEndPoint, new AsyncCallback(ReceivedPacket), null);
        }
        public void ProcessReceivedData(byte[] data, int size)
        {
            int ver = SnmpPacket.GetProtocolVersion(data, size);
            if (ver == (int)SnmpVersion.Ver1)
            {
                SnmpV1TrapPacket pkt = new SnmpV1TrapPacket();
                pkt.decode(data, size);
                DataGridViewRow row = (DataGridViewRow)form.TrapDataGrid.RowTemplate.Clone();
                int value = pkt.Pdu.Generic;
                Generic enumDisplayStatus = (Generic)value;
                string stringValue = enumDisplayStatus.ToString();
                row.CreateCells(form.TrapDataGrid, stringValue, pkt.Pdu.AgentAddress.ToString(), DateTime.Now.ToString());
                form.TrapDataGrid.Rows.Add(row);
            }
            else
            {
                MessageBox.Show("Wrong version of SNMP");
            }
        }

    }
}
