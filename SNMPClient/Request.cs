using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using SnmpSharpNet;
using System.Windows.Forms;

namespace SNMPClient
{
    class Request
    {
        OctetString community;
        AgentParameters param;
        string agentAddress;
        IpAddress agent;
        UdpTarget target;
        Oid rootOid = new Oid();
        Oid lastOid = new Oid();
        public Request()
        {
            community = new OctetString("public");
            param = new AgentParameters(community);
            param.Version = SnmpVersion.Ver1;
            agentAddress = "localhost";
            Initialize();
        }
        public void Initialize()
        {
            agent = new IpAddress(agentAddress);
            target = new UdpTarget((IPAddress)agent, 161, 2000, 1);
        }
        public SnmpV1Packet Get(string oid)
        {
            Pdu pdu = new Pdu(PduType.Get);
            pdu.VbList.Add(oid);
            SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);
            if (result != null)
            {
                if (result.Pdu.ErrorStatus != 0)
                {
                    MessageBox.Show("Error in SNMP reply. Error " + result.Pdu.ErrorStatus + " index " + result.Pdu.ErrorIndex);
                    return null;
                }
                else
                    return result;
            }
            else
            {
                MessageBox.Show("No response received from SNMP agent.");
                return null;
            }
        }
        public SnmpV1Packet GetNext (string OID, string lastOID)
        {
            if (rootOid != null)
            {
                Oid tmp = new Oid(OID);
                if(rootOid == tmp)
                {
                    lastOid = new Oid(lastOID);
                }
                else
                {
                    rootOid = tmp;
                    lastOid = rootOid;
                }
            }
            else
            {
                rootOid = new Oid(OID);
                lastOid = rootOid;
            }
              Pdu pdu = new Pdu(PduType.GetNext);
              pdu.VbList.Clear();
              pdu.VbList.Add(lastOid);
              SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);
              if (result != null)
              {
                  if (result.Pdu.ErrorStatus != 0)
                  {
                      MessageBox.Show("Error in SNMP reply. Error "+ result.Pdu.ErrorStatus+ " index " + result.Pdu.ErrorIndex);
                      lastOid = null;
                      return null;
                  }
                  else
                  {
                       Vb v = result.Pdu.VbList[0];
                           if (rootOid.IsRootOf(v.Oid))
                           {
                                   return result;
                           }
                           else
                           {
                               lastOid = null;
                               return null;
                           } 
                    }
              }
              else
              {
                  MessageBox.Show("No response received from SNMP agent.");
                  return null;
              } 
        }
        public void GetTable()
        {

        }
    }
}
