﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HashLib;


namespace GatelessGateSharp
{
    class NiceHashEthashStratum : EthashStratum
    {
        public new class Work : EthashStratum.Work
        {
            readonly private Job mJob;

            new public Job GetJob() { return mJob; }

            public Work(Job aJob)
                : base(aJob)
            {
                mJob = aJob;
            }
        }

        public new class Job : EthashStratum.Job
        {
            public Job(StratumServer aStratum, string aID, string aSeedhash, string aHeaderhash) 
                : base(aStratum, aID, aSeedhash, aHeaderhash)
            {
            }
        }

        int mJsonRPCMessageID = 1;
        string mSubsciptionID = null;
        private Mutex mMutex = new Mutex();

        protected override void ProcessLine(String line)
        {
            //MainForm.Logger("line: " + line);
            Dictionary<String, Object> response = JsonConvert.DeserializeObject<Dictionary<string, Object>>(line);
            if (response.ContainsKey("method") && response.ContainsKey("params"))
            {
                string method = (string)response["method"];
                JArray parameters = (JArray)response["params"];
                if (method.Equals("mining.set_difficulty"))
                {
                    try  { mMutex.WaitOne(5000); } catch (Exception) { }
                    mDifficulty = (double)parameters[0];
                    try  { mMutex.ReleaseMutex(); } catch (Exception) { }
                    MainForm.Logger("Difficulty set to " + (double)parameters[0] + ".");
                }
                else if (method.Equals("mining.notify"))
                {
                    bool jobChanged = (mJob == null || mJob.ID != (string)parameters[0]);
                    try { mMutex.WaitOne(5000); } catch (Exception) { }
                    mJob = (EthashStratum.Job)(new Job(this, (string)parameters[0], (string)parameters[1], (string)parameters[2]));
                    try  { mMutex.ReleaseMutex(); } catch (Exception) { }
                    if (!SilentMode && jobChanged) MainForm.Logger("Received new job: " + parameters[0]);
                    //MainForm.Logger("Seedhash: " + parameters[1]);
                }
                else if (method.Equals("mining.set_extranonce"))
                {
                    try  { mMutex.WaitOne(5000); } catch (Exception) { }
                    mPoolExtranonce = (String)parameters[0];
                    try  { mMutex.ReleaseMutex(); } catch (Exception) { }
                    MainForm.Logger("Received new extranonce: " + parameters[0]);
                }
                else if (method.Equals("client.reconnect"))
                {
                    throw new Exception("client.reconnect");
                }
                else
                {
                    MainForm.Logger("Unknown stratum method: " + line);
                }
            }   
            else if (response.ContainsKey("id") && response.ContainsKey("result"))
            {
                Int64 ID;
                try {
                    ID = (Int64)(response["id"]);
                } catch (Exception) {
                    ID = int.Parse((string)(response["id"]));
                }
                bool result = (bool)response["result"];

                if (ID > 3 && result)
                {
                    ReportAcceptedShare();
                } else if (ID > 3 && !result) {
                    ReportRejectedShare((String)(((JArray)response["error"])[1]));
                } else if (ID == 3 && !result) {
                    throw new UnrecoverableException("Authorization failed.");
                }
            }
            else
            {
                MainForm.Logger("Unknown JSON message: " + line);
            }
        }

        override protected void Authorize()
        {
            try  { mMutex.WaitOne(5000); } catch (Exception) { }

            mJsonRPCMessageID = 1;

            WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, Object> {
                { "id", mJsonRPCMessageID++ },
                { "method", "mining.subscribe" },
                { "params", new List<string> {
                    MainForm.normalizedShortAppName + "/" + MainForm.appVersion,
                    "EthereumStratum/1.0.0"
            }}}));

            Dictionary<String, Object> response;
            try {
                response = JsonConvert.DeserializeObject<Dictionary<string, Object>>(ReadLine());
                mSubsciptionID = (string)(((JArray)(((JArray)(response["result"]))[0]))[1]);
                mPoolExtranonce = (string)(((JArray)(response["result"]))[1]);
            } catch (Exception) {
                throw this.UnrecoverableException = new AuthorizationFailedException();
            }

            // mining.extranonce.subscribe
            WriteLine(JsonConvert.SerializeObject(new Dictionary<string, Object> {
                { "id", mJsonRPCMessageID++ },
                { "method", "mining.extranonce.subscribe" },
                { "params", new List<string> {
            }}}));
            response = JsonConvert.DeserializeObject<Dictionary<string, Object>>(ReadLine());
            //MainForm.Logger("mining.extranonce.subscribe: " + response["result"]); // TODO
            
            WriteLine(JsonConvert.SerializeObject(new Dictionary<string, Object> {
                { "id", mJsonRPCMessageID++ },
                { "method", "mining.authorize" },
                { "params", new List<string> {
                    Username,
                    Password
            }}}));

            try  { mMutex.ReleaseMutex(); } catch (Exception) { }
        }

        override public void Submit(OpenCLDevice aDevice, EthashStratum.Job job, UInt64 output)
        {
            if (Stopped)
                return;

            try  { mMutex.WaitOne(5000); } catch (Exception) { }
            ReportSubmittedShare(aDevice);
            try
            {
                String stringNonce
                      = ((PoolExtranonce.Length == 0) ? (String.Format("{7:x2}{6:x2}{5:x2}{4:x2}{3:x2}{2:x2}{1:x2}{0:x2}", ((output >> 0) & 0xff), ((output >> 8) & 0xff), ((output >> 16) & 0xff), ((output >> 24) & 0xff), ((output >> 32) & 0xff), ((output >> 40) & 0xff), ((output >> 48) & 0xff), ((output >> 56) & 0xff))) :
                         (PoolExtranonce.Length == 2) ? (String.Format("{6:x2}{5:x2}{4:x2}{3:x2}{2:x2}{1:x2}{0:x2}", ((output >> 0) & 0xff), ((output >> 8) & 0xff), ((output >> 16) & 0xff), ((output >> 24) & 0xff), ((output >> 32) & 0xff), ((output >> 40) & 0xff), ((output >> 48) & 0xff))) :
                         (PoolExtranonce.Length == 4) ? (String.Format("{5:x2}{4:x2}{3:x2}{2:x2}{1:x2}{0:x2}", ((output >> 0) & 0xff), ((output >> 8) & 0xff), ((output >> 16) & 0xff), ((output >> 24) & 0xff), ((output >> 32) & 0xff), ((output >> 40) & 0xff))) :
                                                        (String.Format("{4:x2}{3:x2}{2:x2}{1:x2}{0:x2}", ((output >> 0) & 0xff), ((output >> 8) & 0xff), ((output >> 16) & 0xff), ((output >> 24) & 0xff), ((output >> 32) & 0xff))));
                String message = JsonConvert.SerializeObject(new Dictionary<string, Object> {
                    { "id", mJsonRPCMessageID },
                    { "method", "mining.submit" },
                    { "params", new List<string> {
                        Username,
                        job.ID,
                        stringNonce
                }}});
                WriteLine(message);
                ++mJsonRPCMessageID;
            }
            catch (Exception ex) {
                MainForm.Logger("Failed to submit share: " + ex.Message + "\nReconnecting to the server...");
                Reconnect();
            }
            try  { mMutex.ReleaseMutex(); } catch (Exception) { }
        }

        public NiceHashEthashStratum(String aServerAddress, int aServerPort, String aUsername, String aPassword, String aPoolName, bool aSecureConnection = false)
            : base(aServerAddress, aServerPort, aUsername, aPassword, aPoolName, aSecureConnection)
        {
        }
    }
}
