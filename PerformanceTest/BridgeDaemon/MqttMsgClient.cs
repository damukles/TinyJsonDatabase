using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Net;
using System.Text;
using System.Threading;
using Serilog;
using System.Linq;
using Newtonsoft.Json;
using TinyBlockStorage.Blob;

namespace BridgeDaemon
{
    public class MqttMsgClient : IJob
    {
        private readonly BlobDatabase _db;
        private MqttClient _client { get; set; }

        private readonly string _clientId;

        private ManualResetEventSlim _exitEvent { get; set; }

        public MqttMsgClient(BlobDatabase db)
        {
            _db = db;
            _clientId = Guid.NewGuid().ToString();
            _exitEvent = new ManualResetEventSlim();
        }

        // UPDATE last online is not implemented

        public void Start(object param)
        {
            var controllerId = (int)param;

            Log.Information("Started MqttMsgClient {clientId}", _clientId);

            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                _client = new MqttClient(IPAddress.Parse("127.0.0.1"), controllerId, false, null, null, MqttSslProtocols.None);
#pragma warning restore CS0618 // Type or member is obsolete

                _client.MqttMsgPublishReceived += MqttMsgReceived;
                _client.ConnectionClosed += MqttConnectionClosed;

                _client.Connect(_clientId);
                _client.Subscribe(new string[] { $"/loadtest/{_clientId}" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            catch (Exception e)
            {
                Log.Error(e, "MQTT client threw an error.");
                Stop();
            }

            _exitEvent.Wait();

            Log.Information("Ended MqttMsgClient {clientId}", _clientId);
        }

        public void Stop()
        {
            Log.Information("Shutdown requested for MqttMsgClient {clientId}", _clientId);

            if (_client != null && _client.IsConnected)
                _client.Disconnect();
        }

        private void MqttMsgReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (_client == null || !_client.IsConnected)
                return;

            _db.Insert(new BlobModel(e.Message));

            Log.Information("Record for client {clientId} has been added to the database.", _clientId);
        }

        private void MqttConnectionClosed(object sender, EventArgs e)
        {
            Log.Warning("MQTT client connection was closed.");
            _exitEvent.Set();
            _db.Dispose();
        }
    }
}