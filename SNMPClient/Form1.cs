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

namespace SNMPClient
{
    public partial class Form1 : Form
    {
        Request request;
        public Form1()
        {
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
            Dictionary<String, Dictionary<uint, AsnType>> result = new Dictionary<String, Dictionary<uint, AsnType>>();
            // Not every row has a value for every column so keep track of all columns available in the table
            List<uint> tableColumns = new List<uint>();
            List<Oid> columns = new List<Oid>();
            OctetString community = new OctetString("public");
            IpAddress peer = new IpAddress("localhost");
            AgentParameters param = new AgentParameters(community);
            UdpTarget target = new UdpTarget((IPAddress)peer);
            Oid startOid = new Oid(textBox1.Text);
            startOid.Add(1);
            Pdu pdu = new  Pdu(PduType.GetNext);
            pdu.VbList.Add(startOid);
            Oid curOid = (Oid)startOid.Clone();
            while (startOid.IsRootOf(curOid))
            {
                SnmpV1Packet res;
                try
                {
                    res = (SnmpV1Packet)target.Request(pdu, param);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Request failed: {0}", ex.Message);
                    target.Close();
                    return;
                }
                foreach (Vb v in res.Pdu.VbList)
                {
                    curOid = (Oid)v.Oid.Clone();
                    if (startOid.IsRootOf(v.Oid))
                    {
                        // Get child Id's from the OID (past the table.entry sequence)
                        uint[] childOids = Oid.GetChildIdentifiers(startOid, v.Oid);
                        // Get the value instance and converted it to a dotted decimal
                        //  string to use as key in result dictionary
                        uint[] instance = new uint[childOids.Length - 1];
                        Array.Copy(childOids, 1, instance, 0, childOids.Length - 1);
                        String strInst = InstanceToString(instance);
                        // Column id is the first value past <table oid>.entry in the response OID
                        uint column = childOids[0];
                        Oid leaf = (Oid)startOid.Clone();
                        leaf.Add(column);
                        if (!tableColumns.Contains(column))
                            tableColumns.Add(column);
                        if(!columns.Contains(leaf))
                            columns.Add(leaf);
                        if (result.ContainsKey(strInst))
                        {
                            result[strInst][column] = (AsnType)v.Value.Clone();
                        }
                        else
                        {
                            result[strInst] = new Dictionary<uint, AsnType>();
                            result[strInst][column] = (AsnType)v.Value.Clone();
                        }
                    }
                }
                if (startOid.IsRootOf(curOid))
                {
                    pdu.VbList.Clear();
                    pdu.VbList.Add(curOid);
                }
            }
            if (result.Count <= 0)
            {
                MessageBox.Show("No results returned.\n");
            }
            else
            {
                StringBuilder header = new StringBuilder();
                header.Append("Instance");
                foreach (Oid column in columns)
                {
                    header.Append("\t" + column.ToString());
                }
                MakeLog(header.ToString(), fileLogPath);
                foreach (KeyValuePair<string, Dictionary<uint, AsnType>> kvp in result)
                {
                    StringBuilder s = new StringBuilder();
                    s.Append(kvp.Key);
                    foreach (uint column in tableColumns)
                    {
                        if (kvp.Value.ContainsKey(column))
                        {
                            s.Append(" \t ");
                            s.Append(kvp.Value[column].ToString());
                            s.Append(" ");
                            s.Append("(" + SnmpConstants.GetTypeName(kvp.Value[column].Type) + ")");
                        }
                        else
                        {
                            s.Append("\t-");
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
        public static string InstanceToString(uint[] instance)
        {
            StringBuilder str = new StringBuilder();
            foreach (uint v in instance)
            {
                if (str.Length == 0)
                    str.Append(v);
                else
                    str.AppendFormat(".{0}", v);
            }
            return str.ToString();
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
            
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(Monitor);
            aTimer.Interval = 5000;
            if (aTimer.Enabled)
                aTimer.Stop();
            else
                aTimer.Start();
        }
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
                dataGridView1.Rows.Add(row);
            }
        }
    }
}
