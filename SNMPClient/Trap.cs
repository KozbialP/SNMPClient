using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SnmpSharpNet;
using System.Net;
using System.IO;
using System.Timers;
using System.Net.Sockets;

namespace SNMPClient
{
    class Trap
    {
        enum Generic
        {
            ColdStart, WarmStart, LinkDown, LinkUp, AuthenticationFailure, EGPNeithbourLoss, Other
        };
        Form1 form;
        public Socket mySocket;
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
        public Trap(bool flag)
        {

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
            if (this != null) 
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
                if(this!=null)
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
        delegate void DataCallback(byte[] data, int size);
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
                if(stringValue=="Other")
                {
                    stringValue = "Specific: " +  pkt.Pdu.Specific.ToString() + "; " + pkt.Pdu.Enterprise.ToString();
                }
                row.CreateCells(form.TrapDataGrid, stringValue, pkt.Pdu.AgentAddress.ToString(), DateTime.Now.ToString());
                if (form.TrapDataGrid.InvokeRequired)
                {
                    DataCallback d = new DataCallback(ProcessReceivedData);
                    form.Invoke(d, new object[] { data, size });
                }
                else
                {
                    form.TrapDataGrid.Rows.Add(row);
                }
            }
            else
            {
                MessageBox.Show("Wrong version of SNMP");
            }
        }

    }
}
