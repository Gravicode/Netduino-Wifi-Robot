using System;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Configuration;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Robot.Web
{
    
    [HubName("RobotHub")]
    public class RobotHub : Hub
    {
        public static MqttClient client { set; get; }
        public static string MQTT_BROKER_ADDRESS
        {
            get { return ConfigurationManager.AppSettings["MQTT_BROKER_ADDRESS"]; }
        }
        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            client.Subscribe(new string[] { "/robot/status", "/robot/control", "/robot/state" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

       
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string Pesan = Encoding.UTF8.GetString(e.Message);
            switch (e.Topic)
            {
                case "/robot/status":
                    WriteMessage(Pesan);
                    break;
                case "/robot/control":
                    WriteMessage(Pesan);
                    break;
                case "/robot/state":
                    UpdateState(Pesan);
                    break;
            }
            
        }
        public RobotHub()
        {
            if (client == null)
            {
                // create client instance 
                client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

                string clientId = Guid.NewGuid().ToString();
                client.Connect(clientId, "guest", "guest");

                SubscribeMessage();
            }
        }

        [HubMethodName("MoveRobot")]
        public void MoveRobot(string Cmd,string Direction)
        {
            string Pesan = Cmd + ":" + Direction;
            client.Publish("/robot/control", Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

        }
        internal static void UpdateState(string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<RobotHub>();
            dynamic allClients = context.Clients.All.UpdateState(message);
        }
        internal void WriteRawMessage(string msg)
        {
            WriteMessage(msg);
        }
        internal static void WriteMessage(string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<RobotHub>();
            dynamic allClients = context.Clients.All.WriteData(message);
        }
        
    }
}