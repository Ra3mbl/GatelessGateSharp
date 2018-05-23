﻿// Copyright 2017 Yurio Miyazawa (a.k.a zawawa) <me@yurio.net>
//
// This file is part of Gateless Gate Sharp.
//
// Gateless Gate Sharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Gateless Gate Sharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Gateless Gate Sharp.  If not, see <http://www.gnu.org/licenses/>.



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cloo;
using HashLib;



namespace GatelessGateSharp {
    class OpenCLDualEthashPascalMiner : OpenCLMiner {
        public static readonly int sPascalInputSize = 196;
        public static readonly int sPascalOutputSize = 256;
        public static readonly int sPascalMidstateSize = 32;

        private long[] mEthashGlobalWorkOffsetArray = new long[1];
        private long[] mEthashGlobalWorkSizeArray = new long[1];
        private long[] mEthashLocalWorkSizeArray = new long[1];
        private int mEthashIntensity;

        UInt32[] mPascalOutput = new UInt32[sPascalOutputSize];
        byte[] mPascalInput = new byte[sPascalInputSize];
        byte[] mPascalMidstate = new byte[sPascalMidstateSize];
        private UInt32 mPascalRatio = 4;

        public OpenCLDualEthashPascalMiner(OpenCLDevice aGatelessGateDevice)
            : base(aGatelessGateDevice, "ethash_pascal", "ethash", "pascal") {
        }

        public void Start(EthashStratum aEthashStratum, PascalStratum aPascalStratum, int aEthashIntensity, int aPascalIterations, int aKernelOptimizationLevel = -1)
        {
            KernelOptimizationLevel = aKernelOptimizationLevel;
            PrimaryStratum = aEthashStratum;
            mEthashLocalWorkSizeArray[0] = 256;
            mEthashIntensity = aEthashIntensity;

            SecondaryStratum = aPascalStratum;
            mPascalRatio = (UInt32)aPascalIterations;

            base.Start();
        }

        // based on HashLib's SHA256 implementation
        private static readonly uint[] s_K = 
        {
            0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
            0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
            0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
            0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
            0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85, 
            0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
            0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
            0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
        };
        void CalculatePascalMidState() {
            uint[] state = new uint[] { 0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19 };

            for (int block = 0; block < 3; ++block) {
                uint[] data = new uint[80];

                uint A = state[0];
                uint B = state[1];
                uint C = state[2];
                uint D = state[3];
                uint E = state[4];
                uint F = state[5];
                uint G = state[6];
                uint H = state[7];

                for (int j = 0; j < 16; ++j)
                    data[j] = (uint)(mPascalInput[block * 64 + j * 4 + 0] << 24)
                              | (uint)(mPascalInput[block * 64 + j * 4 + 1] << 16)
                              | (uint)(mPascalInput[block * 64 + j * 4 + 2] << 8)
                              | (uint)(mPascalInput[block * 64 + j * 4 + 3] << 0);

                for (int r = 16; r < 64; r++) {
                    uint T = data[r - 2];
                    uint T2 = data[r - 15];
                    data[r] = (((T >> 17) | (T << 15)) ^ ((T >> 19) | (T << 13)) ^ (T >> 10)) + data[r - 7] +
                        (((T2 >> 7) | (T2 << 25)) ^ ((T2 >> 18) | (T2 << 14)) ^ (T2 >> 3)) + data[r - 16];
                }

                for (int r = 0; r < 64; r++) {
                    uint T = s_K[r] + data[r] + H + (((E >> 6) | (E << 26)) ^ ((E >> 11) | (E << 21)) ^ ((E >> 25) |
                             (E << 7))) + ((E & F) ^ (~E & G));
                    uint T2 = (((A >> 2) | (A << 30)) ^ ((A >> 13) | (A << 19)) ^
                              ((A >> 22) | (A << 10))) + ((A & B) ^ (A & C) ^ (B & C));
                    H = G;
                    G = F;
                    F = E;
                    E = D + T;
                    D = C;
                    C = B;
                    B = A;
                    A = T + T2;
                }

                state[0] += A;
                state[1] += B;
                state[2] += C;
                state[3] += D;
                state[4] += E;
                state[5] += F;
                state[6] += G;
                state[7] += H;
            }

            for (int j = 0; j < 8; ++j) {
                mPascalMidstate[j * 4 + 0] = (byte)((state[j] >> 0) & 0xff);
                mPascalMidstate[j * 4 + 1] = (byte)((state[j] >> 8) & 0xff);
                mPascalMidstate[j * 4 + 2] = (byte)((state[j] >> 16) & 0xff);
                mPascalMidstate[j * 4 + 3] = (byte)((state[j] >> 24) & 0xff);
            }
        }

        public EthashStratum PrimaryStratum { get; set; }
        public PascalStratum SecondaryStratum { get; set; }

        public override void SetPrimaryStratum(StratumServer stratum) {
            PrimaryStratum = (EthashStratum)stratum;
        }
        public override void SetSecondaryStratum(StratumServer stratum) {
            SecondaryStratum = (PascalStratum)stratum;
        }

        override unsafe protected void MinerThread() {
            ComputeProgram program = null;
            try {
                Random r = new Random();
                ComputeDevice computeDevice = OpenCLDevice.GetComputeDevice();
                UInt32[] ethashOutput = new UInt32[256];
                byte[] ethashHeaderhash = new byte[32];

                MarkAsAlive();

                MainForm.Logger("Miner thread for Device #" + DeviceIndex + " started.");

                program = BuildProgram("ethash_pascal", mEthashLocalWorkSizeArray[0], "", "", "");

                MemoryUsage = 256;
                MemoryUsage += 32;
                MemoryUsage += sPascalInputSize;
                MemoryUsage += sPascalOutputSize;
                MemoryUsage += sPascalMidstateSize;

                using (var searchKernel = program.CreateKernel("search"))
                using (var DAGKernel = program.CreateKernel("GenerateDAG"))
                using (var ethashOutputBuffer = new ComputeBuffer<UInt32>(Context, ComputeMemoryFlags.ReadWrite, 256))
                using (var ethashHeaderBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly, 32))
                using (var pascalInputBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly, sPascalInputSize))
                using (var pascalOutputBuffer = new ComputeBuffer<UInt32>(Context, ComputeMemoryFlags.ReadWrite, sPascalOutputSize))
                using (var pascalMidstateBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly, sPascalMidstateSize))
                fixed (UInt32* ethashOutputPtr = ethashOutput)
                fixed (byte* ethashHeaderhashPtr = ethashHeaderhash)
                fixed (byte* pascalMidstatePtr = mPascalMidstate)
                fixed (byte* pascalInputPtr = mPascalInput)
                fixed (UInt32* pascalOutputPtr = mPascalOutput)
                while (!Stopped) {
                    ComputeBuffer<byte> ethashDAGBuffer = null;

                    MarkAsAlive();

                    try {
                        int ethashEpoch = -1;
                        long ethashDAGSize = 0;

                        searchKernel.SetMemoryArgument(7 + 0, pascalInputBuffer);
                        searchKernel.SetMemoryArgument(7 + 1, pascalOutputBuffer);
                        searchKernel.SetMemoryArgument(7 + 4, pascalMidstateBuffer);
                        searchKernel.SetValueArgument<UInt32>(7 + 5, mPascalRatio);

                        // Wait for the first job to arrive.
                        int elapsedTime = 0;
                        while ((PrimaryStratum == null || PrimaryStratum.GetJob() == null || SecondaryStratum == null || SecondaryStratum.GetJob() == null) && elapsedTime < Parameters.TimeoutForFirstJobInMilliseconds && !Stopped) {
                            Thread.Sleep(100);
                            elapsedTime += 100;
                        }
                        if (PrimaryStratum == null || PrimaryStratum.GetJob() == null || SecondaryStratum == null || SecondaryStratum.GetJob() == null)
                            throw new TimeoutException("Stratum server failed to send a new job.");

                        System.Diagnostics.Stopwatch consoleUpdateStopwatch = new System.Diagnostics.Stopwatch();
                        EthashStratum.Work ethashWork;
                        EthashStratum.Job ethashJob;
                        PascalStratum.Work pascalWork;
                        PascalStratum.Job pascalJob;

                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        while (!Stopped
                            && (ethashWork = PrimaryStratum.GetWork()) != null && (ethashJob = ethashWork.GetJob()) != null
                            && (pascalWork = SecondaryStratum.GetWork()) != null && (pascalJob = pascalWork.Job) != null) {
                            MarkAsAlive();

                            String ethashPoolExtranonce = PrimaryStratum.PoolExtranonce;
                            byte[] ethashExtranonceByteArray = Utilities.StringToByteArray(ethashPoolExtranonce);
                            byte ethashLocalExtranonce = (byte)ethashWork.LocalExtranonce;
                            UInt64 ethashStartNonce = (UInt64)ethashLocalExtranonce << (8 * (7 - ethashExtranonceByteArray.Length));
                            for (int i = 0; i < ethashExtranonceByteArray.Length; ++i)
                                ethashStartNonce |= (UInt64)ethashExtranonceByteArray[i] << (8 * (7 - i));
                            ethashStartNonce += (ulong)r.Next(0, int.MaxValue) & (0xfffffffffffffffful >> (ethashExtranonceByteArray.Length * 8 + 8));
                            String ethashJobID = ethashJob.ID;
                            String ethashSeedhash = ethashJob.Seedhash;
                            double ethashDifficulty = PrimaryStratum.Difficulty;
                            Buffer.BlockCopy(Utilities.StringToByteArray(ethashWork.GetJob().Headerhash), 0, ethashHeaderhash, 0, 32);
                            Queue.Write<byte>(ethashHeaderBuffer, true, 0, 32, (IntPtr)ethashHeaderhashPtr, null);

                            Array.Copy(pascalWork.Blob, mPascalInput, sPascalInputSize);
                            CalculatePascalMidState();
                            Queue.Write<byte>(pascalMidstateBuffer, true, 0, sPascalMidstateSize, (IntPtr)pascalMidstatePtr, null);
                            UInt32 pascalStartNonce = (UInt32)(r.Next(0, int.MaxValue));
                            UInt64 PascalTarget = (UInt64)((double)0xffff0000UL / SecondaryStratum.Difficulty);
                            searchKernel.SetValueArgument<UInt64>(7 + 3, PascalTarget);
                            Queue.Write<byte>(pascalInputBuffer, true, 0, sPascalInputSize, (IntPtr)pascalInputPtr, null);

                            if (ethashEpoch != ethashWork.GetJob().Epoch) {
                                if (ethashDAGBuffer != null) {
                                    MemoryUsage -= ethashDAGBuffer.Size;
                                    ethashDAGBuffer.Dispose();
                                    ethashDAGBuffer = null;
                                }
                                ethashEpoch = ethashWork.GetJob().Epoch;
                                DAGCache cache = new DAGCache(ethashEpoch, ethashWork.GetJob().Seedhash);
                                ethashDAGSize = Utilities.GetDAGSize(ethashEpoch);

                                sw.Restart();
                                mEthashGlobalWorkSizeArray[0] = ethashDAGSize / 64;
                                mEthashGlobalWorkSizeArray[0] /= 8;
                                if (mEthashGlobalWorkSizeArray[0] % mEthashLocalWorkSizeArray[0] > 0)
                                    mEthashGlobalWorkSizeArray[0] += mEthashLocalWorkSizeArray[0] - mEthashGlobalWorkSizeArray[0] % mEthashLocalWorkSizeArray[0];

                                ComputeBuffer<byte> DAGCacheBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly, cache.GetData().Length);
                                MemoryUsage += DAGCacheBuffer.Size; 
                                fixed (byte* p = cache.GetData())
                                    Queue.Write<byte>(DAGCacheBuffer, true, 0, cache.GetData().Length, (IntPtr)p, null);
                                ethashDAGBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite, mEthashGlobalWorkSizeArray[0] * 8 * 64 /* ethashDAGSize */); // With this, we can remove a conditional statement in the DAG kernel.
                                MemoryUsage += ethashDAGBuffer.Size;

                                DAGKernel.SetValueArgument<UInt32>(0, 0);
                                DAGKernel.SetMemoryArgument(1, DAGCacheBuffer);
                                DAGKernel.SetMemoryArgument(2, ethashDAGBuffer);
                                DAGKernel.SetValueArgument<UInt32>(3, (UInt32)cache.GetData().Length / 64);
                                DAGKernel.SetValueArgument<UInt32>(4, 0xffffffffu);

                                for (long start = 0; start < ethashDAGSize / 64; start += mEthashGlobalWorkSizeArray[0]) {
                                    mEthashGlobalWorkOffsetArray[0] = start;
                                    Queue.Execute(DAGKernel, mEthashGlobalWorkOffsetArray, mEthashGlobalWorkSizeArray, mEthashLocalWorkSizeArray, null);
                                    Queue.Finish();
                                    if (Stopped || PrimaryStratum.GetJob() == null || !PrimaryStratum.GetJob().ID.Equals(ethashJobID))
                                        break;
                                }
                                DAGCacheBuffer.Dispose();
                                MemoryUsage -= DAGCacheBuffer.Size;
                                if (Stopped || PrimaryStratum.GetJob() == null || !PrimaryStratum.GetJob().ID.Equals(ethashJobID))
                                    break;
                                MainForm.Logger("Generated DAG for Epoch #" + ethashEpoch + " (" + (long)sw.Elapsed.TotalMilliseconds + "ms).");
                                sw.Restart();
                            }

                            consoleUpdateStopwatch.Start();

                            while (!Stopped && PrimaryStratum.GetJob().ID.Equals(ethashJobID) && PrimaryStratum.PoolExtranonce.Equals(ethashPoolExtranonce) && SecondaryStratum.GetJob().Equals(pascalJob)) {

                                MarkAsAlive();

                                mEthashGlobalWorkOffsetArray[0] = 0;
                                mEthashGlobalWorkSizeArray[0] = mEthashIntensity * mEthashLocalWorkSizeArray[0] * OpenCLDevice.GetComputeDevice().MaxComputeUnits;

                                // Get a new local extranonce if necessary.
                                if ((ethashStartNonce & (0xfffffffffffffffful >> (ethashExtranonceByteArray.Length * 8 + 8)) + (ulong)mEthashGlobalWorkSizeArray[0] * 3 / 4) >= ((ulong)0x1 << (64 - (ethashExtranonceByteArray.Length * 8 + 8))))
                                    break;
                                if (0xffffffffu - pascalStartNonce < (UInt32)mEthashGlobalWorkSizeArray[0] * mPascalRatio)
                                    break;

                                UInt64 target = (UInt64)((double)0xffff0000U / ethashDifficulty);
                                searchKernel.SetMemoryArgument(0, ethashOutputBuffer); // g_output
                                searchKernel.SetMemoryArgument(1, ethashHeaderBuffer); // g_header
                                searchKernel.SetMemoryArgument(2, ethashDAGBuffer); // _g_dag
                                searchKernel.SetValueArgument<UInt32>(3, (UInt32)(ethashDAGSize / 128)); // DAG_SIZE
                                searchKernel.SetValueArgument<UInt64>(4, ethashStartNonce); // start_nonce
                                searchKernel.SetValueArgument<UInt64>(5, target); // target
                                searchKernel.SetValueArgument<UInt32>(6, 0xffffffffu); // isolate

                                searchKernel.SetValueArgument<UInt32>(7 + 2, pascalStartNonce);

                                ethashOutput[255] = 0; // ethashOutput[255] is used as an atomic counter.
                                Queue.Write<UInt32>(ethashOutputBuffer, true, 0, 256, (IntPtr)ethashOutputPtr, null);
                                mPascalOutput[255] = 0; // mPascalOutput[255] is used as an atomic counter.
                                Queue.Write<UInt32>(pascalOutputBuffer, true, 0, sPascalOutputSize, (IntPtr)pascalOutputPtr, null);
                                Queue.Execute(searchKernel, mEthashGlobalWorkOffsetArray, mEthashGlobalWorkSizeArray, mEthashLocalWorkSizeArray, null);

                                Queue.Read<UInt32>(ethashOutputBuffer, true, 0, 256, (IntPtr)ethashOutputPtr, null);
                                if (PrimaryStratum.GetJob() != null && PrimaryStratum.GetJob().ID.Equals(ethashJobID)) {
                                    for (int i = 0; i < ethashOutput[255]; ++i)
                                        PrimaryStratum.Submit(Device, ethashWork.GetJob(), ethashStartNonce + (UInt64)ethashOutput[i]);
                                }
                                ethashStartNonce += (UInt64)mEthashGlobalWorkSizeArray[0] * 3 / 4;

                                Queue.Read<UInt32>(pascalOutputBuffer, true, 0, sPascalOutputSize, (IntPtr)pascalOutputPtr, null);
                                if (SecondaryStratum.GetJob() != null && SecondaryStratum.GetJob().Equals(pascalJob)) {
                                    for (int i = 0; i < mPascalOutput[255]; ++i)
                                        SecondaryStratum.Submit(Device, pascalWork, mPascalOutput[i]);
                                }
                                pascalStartNonce += (UInt32)mEthashGlobalWorkSizeArray[0] * mPascalRatio;

                                ReportHashCount((double)mEthashGlobalWorkSizeArray[0] * 0.75, (double)mEthashGlobalWorkSizeArray[0] * mPascalRatio, sw.Elapsed.TotalSeconds);
                                sw.Restart();
                                if (consoleUpdateStopwatch.ElapsedMilliseconds >= 10 * 1000) {
                                    MainForm.Logger("Device #" + DeviceIndex + ": " + String.Format("{0:N2} Mh/s (Ethash), ", Speed / (1000000)) + String.Format("{0:N2} Mh/s (Pascal)", SpeedSecondaryAlgorithm / (1000000)));
                                    consoleUpdateStopwatch.Restart();
                                }
                            }
                        }
                    } catch (Exception ex) {
                        MainForm.Logger("Exception in miner thread: " + ex.Message + ex.StackTrace);
                        Speed = 0;
                        if (UnrecoverableException.IsUnrecoverableException(ex)) {
                            this.UnrecoverableException = new UnrecoverableException(ex, Device);
                            Stop();
                        } else {
                            MainForm.Logger("Restarting miner thread...");
                            System.Threading.Thread.Sleep(Parameters.WaitTimeForRestartingMinerThreadInMilliseconds);
                        }
                    } finally {
                        if (ethashDAGBuffer != null) {
                            MemoryUsage -= ethashDAGBuffer.Size;
                            ethashDAGBuffer.Dispose();
                            ethashDAGBuffer = null;
                        }
                    }
                }
            } catch (UnrecoverableException ex) {
                this.UnrecoverableException = ex;
            } catch (Exception ex) {
                this.UnrecoverableException = new UnrecoverableException(ex, Device);
            } finally {
                MarkAsDone();
                MemoryUsage = 0;
                if (program != null)
                    program.Dispose();
                program = null;
            }
        }
    }
}
