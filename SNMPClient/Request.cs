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

            // Make SNMP request
            SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);

            // If result is null then agent didn't reply or we couldn't parse the reply.
            if (result != null)
            {
                // ErrorStatus other then 0 is an error returned by 
                // the Agent - see SnmpConstants for error definitions
                if (result.Pdu.ErrorStatus != 0)
                {
                    // agent reported an error with the request
                    MessageBox.Show("Error in SNMP reply. Error " + result.Pdu.ErrorStatus + " index " + result.Pdu.ErrorIndex);
                    return null;
                }
                else
                {
                    // Reply variables are returned in the same order as they were added
                    //  to the VbList
                    return result;
                    
                }
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
                  // ErrorStatus other then 0 is an error returned by 
                  // the Agent - see SnmpConstants for error definitions
                  if (result.Pdu.ErrorStatus != 0)
                  {
                      // agent reported an error with the request
                      Console.WriteLine("Error in SNMP reply. Error {0} index {1}",
                          result.Pdu.ErrorStatus,
                          result.Pdu.ErrorIndex);
                      lastOid = null;
                      return null;
                  }
                  else
                  {
                       Vb v = result.Pdu.VbList[0];
                           // Check that retrieved Oid is "child" of the root OID
                           if (rootOid.IsRootOf(v.Oid))
                           {
                               Console.WriteLine("{0} ({1}): {2}",
                                   v.Oid.ToString(),
                                   SnmpConstants.GetTypeName(v.Value.Type),
                                   v.Value.ToString());
                                   lastOid = v.Oid;
                        return result;
                           }
                           else
                           {
                               // we have reached the end of the requested
                               // MIB tree. Set lastOid to null and exit loop
                               lastOid = null;
                               return null;
                           } 
                    }
              }
              else
              {
                  Console.WriteLine("No response received from SNMP agent.");
                  return null;
              } 
        }
        public void GetTable()
        {

        }
    }
}
