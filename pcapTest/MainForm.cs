﻿using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using SharpPcap.LibPcap;
using SharpPcap;
using PacketDotNet;
using pcapTest;


namespace pcapTest
{
    public partial class MainForm : Form
    {
        List<LibPcapLiveDevice> interfaceList = new List<LibPcapLiveDevice>();
        int selectedIntIndex;
        LibPcapLiveDevice wifi_device;
        CaptureFileWriterDevice captureFileWriter;
        Dictionary<int, Packet> capturedPackets_list = new Dictionary<int, Packet>();

        int packetNumber = 1;
        string time_str = "", sourceIP = "", destinationIP = "", protocol_type = "", length = "";

        bool startCapturingAgain = false;

        Thread sniffing;

        public MainForm(List<LibPcapLiveDevice> interfaces, int selectedIndex)
        {
            InitializeComponent();
            this.interfaceList = interfaces;
            selectedIntIndex = selectedIndex;
            // Extract a device from the list
            wifi_device = interfaceList[selectedIntIndex];
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)// Start sniffing
        {
            if (startCapturingAgain == false) //first time 
            {
                System.IO.File.Delete(Environment.CurrentDirectory + "capture.pcap");
                wifi_device.OnPacketArrival += new PacketArrivalEventHandler(Device_OnPacketArrival);
                sniffing = new Thread(new ThreadStart(sniffing_Proccess));
                sniffing.Start();
                toolStripButton1.Enabled = false;
                toolStripButton2.Enabled = true;
                textBox1.Enabled = false;

            }
            else if (startCapturingAgain)
            {
                if (MessageBox.Show("Your packets are captured in a file. Starting a new capture will override existing ones.", "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    // user clicked ok
                    System.IO.File.Delete(Environment.CurrentDirectory + "capture.pcap");
                    listView1.Items.Clear();
                    capturedPackets_list.Clear();
                    packetNumber = 1;
                    textBox2.Text = "";
                    wifi_device.OnPacketArrival += new PacketArrivalEventHandler(Device_OnPacketArrival);
                    sniffing = new Thread(new ThreadStart(sniffing_Proccess));
                    sniffing.Start();
                    toolStripButton1.Enabled = false;
                    toolStripButton2.Enabled = true;
                    textBox1.Enabled = false;
                }
            }
            startCapturingAgain = true;
        }

        // paket information
        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected) return;

            string protocol = e.Item.SubItems[4].Text;
            int key = Int32.Parse(e.Item.SubItems[0].Text);
            if (!capturedPackets_list.TryGetValue(key, out var packet))
            {
                textBox2.Text = "Packet not found";
                return;
            }

            switch (protocol)
            {
                case "TCP":
                    var tcpPacket = (TcpPacket)packet.Extract(typeof(TcpPacket));
                    if (tcpPacket != null)
                    {
                        int srcPort = tcpPacket.SourcePort;
                        int dstPort = tcpPacket.DestinationPort;
                        var checksum = tcpPacket.Checksum;

                        textBox2.Text = $"Packet number: {key}" +
                                        " Type: TCP" +
                                        $"\r\nSource port: {srcPort}" +
                                        $"\r\nDestination port: {dstPort}" +
                                        $"\r\nTCP header size: {tcpPacket.DataOffset}" +
                                        $"\r\nWindow size: {tcpPacket.WindowSize}" + // bytes that the receiver is willing to receive
                                        $"\r\nChecksum: {checksum}" + (tcpPacket.ValidChecksum ? ",valid" : ",invalid") +
                                        $"\r\nTCP checksum: " + (tcpPacket.ValidTCPChecksum ? ",valid" : ",invalid") +
                                        $"\r\nSequence number: {tcpPacket.SequenceNumber}" +
                                        $"\r\nAcknowledgment number: {tcpPacket.AcknowledgmentNumber}" + (tcpPacket.Ack ? ",valid" : ",invalid") +
                                        // flags
                                        $"\r\nUrgent pointer: " + (tcpPacket.Urg ? "valid" : "invalid") +
                                        $"\r\nACK flag: " + (tcpPacket.Ack ? "1" : "0") + // indicates if the AcknowledgmentNumber is valid
                                        $"\r\nPSH flag: " + (tcpPacket.Psh ? "1" : "0") + // push 1 = the receiver should pass the data to the app immediately, don't buffer it
                                        $"\r\nRST flag: " + (tcpPacket.Rst ? "1" : "0") + // reset 1 is to abort existing connection
                                                                                          // SYN indicates the sequence numbers should be synchronized between the sender and receiver to initiate a connection
                                        $"\r\nSYN flag: " + (tcpPacket.Syn ? "1" : "0") +
                                        // closing the connection with a deal, host_A sends FIN to host_B, B responds with ACK
                                        // FIN flag indicates the sender is finished sending
                                        $"\r\nFIN flag: " + (tcpPacket.Fin ? "1" : "0") +
                                        $"\r\nECN flag: " + (tcpPacket.ECN ? "1" : "0") +
                                        $"\r\nCWR flag: " + (tcpPacket.CWR ? "1" : "0") +
                                        $"\r\nNS flag: " + (tcpPacket.NS ? "1" : "0");
                    }
                    break;
                case "UDP":
                    var udpPacket = (UdpPacket)packet.Extract(typeof(UdpPacket));
                    if (udpPacket != null)
                    {
                        int srcPort = udpPacket.SourcePort;
                        int dstPort = udpPacket.DestinationPort;
                        var checksum = udpPacket.Checksum;

                        textBox2.Text = $"Packet number: {key}" +
                                        " Type: UDP" +
                                        $"\r\nSource port: {srcPort}" +
                                        $"\r\nDestination port: {dstPort}" +
                                        $"\r\nChecksum: {checksum} valid: {udpPacket.ValidChecksum}" +
                                        $"\r\nValid UDP checksum: {udpPacket.ValidUDPChecksum}";
                    }
                    break;
                case "ARP":
                    var arpPacket = (ARPPacket)packet.Extract(typeof(ARPPacket));
                    if (arpPacket != null)
                    {
                        var senderAddress = arpPacket.SenderProtocolAddress;
                        var targetAddress = arpPacket.TargetProtocolAddress;
                        var senderHardwareAddress = arpPacket.SenderHardwareAddress;
                        var targetHardwareAddress = arpPacket.TargetHardwareAddress;

                        textBox2.Text = $"Packet number: {key}" +
                                        " Type: ARP" +
                                        $"\r\nHardware address length: {arpPacket.HardwareAddressLength}" +
                                        $"\r\nProtocol address length: {arpPacket.ProtocolAddressLength}" +
                                        $"\r\nOperation: {arpPacket.Operation}" + // ARP request or ARP reply ARP_OP_REQ_CODE, ARP_OP_REP_CODE
                                        $"\r\nSender protocol address: {senderAddress}" +
                                        $"\r\nTarget protocol address: {targetAddress}" +
                                        $"\r\nSender hardware address: {senderHardwareAddress}" +
                                        $"\r\nTarget hardware address: {targetHardwareAddress}";
                    }
                    break;
                case "ICMP":
                    var icmpPacket = (ICMPv4Packet)packet.Extract(typeof(ICMPv4Packet));
                    if (icmpPacket != null)
                    {
                        textBox2.Text = $"Packet number: {key}" +
                                        " Type: ICMP v4" +
                                        $"\r\nType Code: 0x{icmpPacket.TypeCode:x}" +
                                        $"\r\nChecksum: {icmpPacket.Checksum:x}" +
                                        $"\r\nID: 0x{icmpPacket.ID:x}" +
                                        $"\r\nSequence number: {icmpPacket.Sequence:x}";
                    }
                    break;
                case "IGMP":
                    var igmpPacket = (IGMPv2Packet)packet.Extract(typeof(IGMPv2Packet));
                    if (igmpPacket != null)
                    {
                        textBox2.Text = $"Packet number: {key}" +
                                        " Type: IGMP v2" +
                                        $"\r\nType: {igmpPacket.Type}" +
                                        $"\r\nGroup address: {igmpPacket.GroupAddress}" +
                                        $"\r\nMax response time: {igmpPacket.MaxResponseTime}";
                    }
                    break;
                default:
                    textBox2.Text = "";
                    break;
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)// last packet
        {
            var items = listView1.Items;
            var last = items[items.Count - 1];
            last.EnsureVisible();
            last.Selected = true;
        }

        private void toolStripButton5_Click(object sender, EventArgs e)// fist packet
        {
            var first = listView1.Items[0];
            first.EnsureVisible();
            first.Selected = true;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)//next
        {
            if (listView1.SelectedItems.Count == 1)
            {
                int index = listView1.SelectedItems[0].Index;
                listView1.Items[index + 1].Selected = true;
                listView1.Items[index + 1].EnsureVisible();
            }
        }

        private void chooseInterfaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interfaces openInterfaceForm = new Interfaces();
            this.Hide();
            openInterfaceForm.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)// prev
        {
            if (listView1.SelectedItems.Count == 1)
            {
                int index = listView1.SelectedItems[0].Index;
                listView1.Items[index - 1].Selected = true;
                listView1.Items[index - 1].EnsureVisible();
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)// Stop sniffing
        {
            sniffing.Abort();
            wifi_device.StopCapture();
            wifi_device.Close();
            captureFileWriter.Close();

            toolStripButton1.Enabled = true;
            textBox1.Enabled = true;
            toolStripButton2.Enabled = false;
        }

        private void sniffing_Proccess()
        {
            // Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            wifi_device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);

            // Start the capturing process
            if (wifi_device.Opened)
            {
                if (textBox1.Text != "")
                {
                    wifi_device.Filter = textBox1.Text;
                }
                captureFileWriter = new CaptureFileWriterDevice(wifi_device, Environment.CurrentDirectory + "capture.pcap");
                wifi_device.Capture();
            }
        }

        public void Device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            // Dump to a file
            captureFileWriter.Write(e.Packet);

            // Start extracting properties for the listview 
            DateTime time = e.Packet.Timeval.Date;
            time_str = (time.Hour + 1) + ":" + time.Minute + ":" + time.Second + ":" + time.Millisecond;
            length = e.Packet.Data.Length.ToString();

            var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            // Extract IP packet for protocol type
            var ipPacket = (IpPacket)packet.Extract(typeof(IpPacket));
            if (ipPacket != null)
            {
                sourceIP = ipPacket.SourceAddress.ToString();
                destinationIP = ipPacket.DestinationAddress.ToString();
                protocol_type = ipPacket.Protocol.ToString();

                // Extract transport layer information
                string sourcePort = "";
                string destinationPort = "";

                var transportPacket = ipPacket.PayloadPacket;
                if (transportPacket is TcpPacket tcpPacket)
                {
                    sourcePort = tcpPacket.SourcePort.ToString();
                    destinationPort = tcpPacket.DestinationPort.ToString();
                }
                else if (transportPacket is UdpPacket udpPacket)
                {
                    sourcePort = udpPacket.SourcePort.ToString();
                    destinationPort = udpPacket.DestinationPort.ToString();
                }

                // Store packet in dictionary before incrementing packet number
                capturedPackets_list[packetNumber] = packet;

                // Create ListViewItem
                ListViewItem item = new ListViewItem(packetNumber.ToString());
                item.SubItems.Add(time_str);
                item.SubItems.Add(sourceIP);
                item.SubItems.Add(destinationIP);
                item.SubItems.Add(protocol_type);
                item.SubItems.Add(length);
                item.SubItems.Add(sourcePort); // Add source port
                item.SubItems.Add(destinationPort); // Add destination port

                Action action = () => listView1.Items.Add(item);
                listView1.Invoke(action);

                // Increment packetNumber after adding to the list
                packetNumber++;
            }
        }

    }
}


