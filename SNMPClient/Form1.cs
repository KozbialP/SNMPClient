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
    public partial class Form1 : Form
    {
        Trap trap;
        Request request;
        System.Timers.Timer aTimer;
        public Form1()
        {
            trap = null;
            request = new Request();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            SnmpV1Packet res = new SnmpV1Packet();
            res = request.Get(textBox1.Text);
            if (res != null)
            {
                DataGridViewRow row = (DataGridViewRow)dataGridView1.RowTemplate.Clone();
                row.CreateCells(dataGridView1, res.Pdu.VbList[0].Oid.ToString(), res.Pdu.VbList[0].Value.ToString(), SnmpConstants.GetTypeName(res.Pdu.VbList[0].Value.Type), "localhost");
                dataGridView1.Rows.Add(row);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
          
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SnmpV1Packet res = new SnmpV1Packet();
            res = request.GetNext(textBox1.Text, textBox2.Text);
            if (res != null)
            {
                foreach (Vb v in res.Pdu.VbList)
                {
                    DataGridViewRow row = (DataGridViewRow)dataGridView1.RowTemplate.Clone();
                    row.CreateCells(dataGridView1, res.Pdu.VbList[0].Oid.ToString(), res.Pdu.VbList[0].Value.ToString(), SnmpConstants.GetTypeName(res.Pdu.VbList[0].Value.Type), "localhost");
                    dataGridView1.Rows.Add(row);
                }
                textBox2.Clear();
                textBox2.AppendText(res.Pdu.VbList[0].Oid.ToString());
                textBox2.Update();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string fileLogPath = textBox3.Text + ".txt";
            List<Oid> columns = new List<Oid>();
            SnmpV1Packet[][] results = new SnmpV1Packet[100][];
            OctetString community = new OctetString("public");
            IpAddress peer = new IpAddress("localhost");
            AgentParameters param = new AgentParameters(community);
            UdpTarget target = new UdpTarget((IPAddress)peer);
            Oid startOid = new Oid(textBox1.Text);
            Pdu pdu = new Pdu(PduType.GetNext);
            pdu.VbList.Add(startOid);
            uint rows = 0;
            uint nOColumns = 0;
            startOid.Add(1);
            Oid curOid = (Oid)startOid.Clone();
            while (startOid.IsRootOf(curOid))
            {
                int o = startOid.Count();
                SnmpV1Packet res;
                    try
                    {
                        res = request.GetNext(startOid.ToString(), curOid.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Request failed: {0}", ex.Message);
                        target.Close();
                        return;
                    }
                if (res == null)
                {
                    break;
                }
                    if (startOid.IsRootOf(curOid))
                    {
                        curOid = (Oid)res.Pdu.VbList[0].Oid.Clone();
                    }
                StringBuilder s = new StringBuilder();
                string[] abcd = curOid.ToString().Split('.');
                for (int j = 0; j <= o; j++)
                {
                    s.Append('.');
                    s.Append(abcd[j]);
                }
                Oid c = new Oid(s.ToString());
                if (!columns.Contains(c))
                {
                    columns.Add(c);
                    nOColumns++;
                    results[nOColumns] = new SnmpV1Packet[100];
                    rows = 0;
                }
                else
                {
                    rows++;
                }
                results[nOColumns][rows] = res;
            }
          if (results[1][0] == null)
          {
              MessageBox.Show("No results returned.\n");
          }
          else
          {
              StringBuilder header = new StringBuilder();
              foreach (Oid column in columns)
              {
                  header.Append(column.ToString() + " \t ");
              }
              MakeLog(header.ToString(), fileLogPath);
                for (int n = 0; n <= rows; n++)
                {
                    StringBuilder s = new StringBuilder();
                    for (int m = 1; m <= nOColumns; m++)
                    {
                        if (results[m][n] != null)
                        {
                            s.Append(results[m][n].Pdu.VbList[0].Value.ToString());
                            s.Append(" ");
                            s.Append("(" + SnmpConstants.GetTypeName(results[m][n].Pdu.VbList[0].Value.Type) + ")\t");
                        }
                    }
                    MakeLog(s.ToString(), fileLogPath);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
        }
        public static void MakeLog(string logDescription, string fileLogPath)
        {
            using (StreamWriter file = new StreamWriter(fileLogPath, true))
            {
                file.WriteLine(logDescription);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (aTimer == null)
            {
                aTimer = new System.Timers.Timer();
                aTimer.Elapsed += new ElapsedEventHandler(Monitor);
                aTimer.Interval = 5000;
                aTimer.Enabled = !aTimer.Enabled;
            }
            else
            {
                aTimer.Stop();
                aTimer = null;
            }
        }
        delegate void MonitorCallback(object source, ElapsedEventArgs e);
        private void Monitor(object source, ElapsedEventArgs e)
        {
            string oid = textBox1.Text;
            if (textBox2.Text == null)
                textBox2.Text = oid;
            string currentOid = textBox2.Text;
            SnmpV1Packet res = new SnmpV1Packet(currentOid);
            res = request.Get(oid);
            if (res != null)
            {
                DataGridViewRow row = (DataGridViewRow)dataGridView1.RowTemplate.Clone();
                row.CreateCells(dataGridView1, res.Pdu.VbList[0].Oid.ToString(), res.Pdu.VbList[0].Value.ToString(), SnmpConstants.GetTypeName(res.Pdu.VbList[0].Value.Type), "localhost");
                if (this.dataGridView1.InvokeRequired)
                {
                    MonitorCallback d = new MonitorCallback(Monitor);
                    this.Invoke(d, new object[] { source, e });
                }
                else
                {
                    this.dataGridView1.Rows.Add(row);
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
                trap = new Trap(this);
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            trap = null;
        }
    }
}
