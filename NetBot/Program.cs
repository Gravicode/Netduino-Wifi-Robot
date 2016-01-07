//Written by Mif Masterz @ Gravicode
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using Toolbox.NETMF.Hardware;
using uPLibrary.Networking.M2Mqtt;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace NetBot
{
    public class Program
    {
        /*
        M2Mqtt : untuk protokol komunikasi
        SecretLabs.NETMF.Hardware.Netduino
        ToolBox.NetMf.Core : core lib toolbox
        Toolbox.NETMF.Hardware.HBridge : driver motor
        Toolbox.NETMF.Hardware.Netduino : additional lib for netduino
         */
        const string MQTT_BROKER_ADDRESS = "192.168.100.8";
        const sbyte HalfSpeed = 50;
        const sbyte FullSpeed = 80;

        static Arah lastDirection = Arah.Berhenti;
        static Arah jalan = Arah.Berhenti;
        static sbyte Speed = 0;
        public static MqttClient client { set; get; }
        //arah gerak robot
        public enum Arah { Berhenti, Maju, Mundur, Kiri, Kanan };
        public static void Main()
        {
            //distance sensor
            //triger (12), echo (13), 5V + G
            HC_SR04 sensor = new HC_SR04(Pins.GPIO_PIN_D12, Pins.GPIO_PIN_D13);
            //buzzer
            OutputPort Klakson = new OutputPort(Pins.GPIO_PIN_D0, false);
            //motor driver
            HBridge MotorDriver = new HBridge(SecretLabs.NETMF.Hardware.Netduino.PWMChannels.PWM_PIN_D5, Pins.GPIO_PIN_D4, SecretLabs.NETMF.Hardware.Netduino.PWMChannels.PWM_PIN_D6, Pins.GPIO_PIN_D7);
            //led indicator if there is an object in the front of robot
            OutputPort WarningLed = new OutputPort(Pins.GPIO_PIN_D1, false);
            OutputPort GoLed = new OutputPort(Pins.GPIO_PIN_D2, false);
   
            //waiting till connect...
            if (!Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IsDhcpEnabled)
            {
                // using static IP
                while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) ; // wait for network connectivity
            }
            else
            {
                // using DHCP
                while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any) ; // wait for DHCP-allocated IP address
            }
            //Debug print our IP address
            Debug.Print("Device IP: " + Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);

            // create mqtt client instance 
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);
            SubscribeMessage();
            PublishMessage("/robot/state", "ONLINE");
            while (true)
            {
                long ticks = sensor.Ping();
                if (ticks > 0L)
                {
                    double inches = sensor.TicksToInches(ticks);
                    Debug.Print("jarak ke object :" + inches);

                    if (inches < 4)
                    {
                        jalan = Arah.Berhenti;
                        Klakson.Write(true);
                        WarningLed.Write(true);
                        GoLed.Write(false);
                        PublishMessage("/robot/status", "Watchout there is an object in the front of robot!");
                    }
                    else
                    {
                        Klakson.Write(false);
                        WarningLed.Write(false);
                        GoLed.Write(true);
                    }
                }
                //stop first before change direction or turn
                if (lastDirection != jalan)
                {
                    //stop before start new direction
                    MotorDriver.SetState(HBridge.Motors.Motor1, 0);
                    MotorDriver.SetState(HBridge.Motors.Motor2, 0);
                }
              
                switch (jalan)
                {
                    case Arah.Maju:
                        MotorDriver.SetState(HBridge.Motors.Motor1, Speed);
                        MotorDriver.SetState(HBridge.Motors.Motor2, Speed);
                        break;
                    case Arah.Mundur:
                        MotorDriver.SetState(HBridge.Motors.Motor1,(sbyte)(-Speed));
                        MotorDriver.SetState(HBridge.Motors.Motor2, (sbyte)(-Speed));
                        break;
                    case Arah.Kiri:
                        MotorDriver.SetState(HBridge.Motors.Motor1, -50);
                        MotorDriver.SetState(HBridge.Motors.Motor2, 50);
                        break;
                    case Arah.Kanan:
                        MotorDriver.SetState(HBridge.Motors.Motor1, 50);
                        MotorDriver.SetState(HBridge.Motors.Motor2, -50);
                        break;
                    case Arah.Berhenti:
                        MotorDriver.SetState(HBridge.Motors.Motor1, 0);
                        MotorDriver.SetState(HBridge.Motors.Motor2, 0);
                        break;
                }
                lastDirection = jalan;
            }

        }

        static void PublishMessage(string Topic, string Pesan)
        {
            client.Publish(Topic, Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }


        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Subscribe(new string[] { "/robot/control" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string Message = new string(Encoding.UTF8.GetChars(e.Message));
            if (Message.IndexOf(":") < 1) return;
            // handle message received 
            Debug.Print("Message Received = " + Message);
            string[] CmdStr = Message.Split(':');
            if (CmdStr[0] == "MOVE")
            {
                switch (CmdStr[1])
                {
                    case "Forward":
                        jalan = Arah.Maju;
                        break;
                    case "Backward":
                        jalan = Arah.Mundur;
                        break;
                    case "Left":
                        jalan = Arah.Kiri;
                        break;
                    case "Right":
                        jalan = Arah.Kanan;
                        break;
                    case "Stop":
                        jalan = Arah.Berhenti;
                        break;
                }
                if (lastDirection == jalan)
                {
                    if (Speed == 0) Speed = HalfSpeed;
                    else Speed = FullSpeed;
                }
                else
                {
                    Speed = HalfSpeed;
                }
                PublishMessage("/robot/status", "Robot Status:" + CmdStr[1]);

            }
            else if(CmdStr[0]=="REQUEST" && CmdStr[1]=="STATUS")
            {
                PublishMessage("/robot/state", "ONLINE");
            }
        }
       
        
    }
}
