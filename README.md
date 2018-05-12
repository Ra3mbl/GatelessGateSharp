# Gateless Gate Sharp

Gateless Gate Sharp is an user-friendly yet extremely powerful open-source multi-algorithm miner for Windows operating systems.
It focuses on performance, stability, and ease of use, featuring a one-of-a-kind ability to modify memory timings on the fly
and a powerful built-in optimizer. Unlike [the original Gateless Gate](https://github.com/zawawawa/gatelessgate), this miner
aims at stability with a much simpler design and the managed .NET Framework.

* [GatelessGateSharpInstaller.exe](https://github.com/zawawawa/GatelessGateSharp/releases/download/v1.3.8-alpha/GatelessGateSharpInstaller.exe) (Windows x64 Installer; highly recommended)
* [Gateless_Gate_Sharp_1.3.8_alpha.7z](https://github.com/zawawawa/GatelessGateSharp/releases/download/v1.3.8-alpha/Gateless_Gate_Sharp_1.3.8_alpha.7z)
* [Gateless_Gate_Sharp_1.3.8_alpha.zip](https://github.com/zawawawa/GatelessGateSharp/releases/download/v1.3.8-alpha/Gateless_Gate_Sharp_1.3.8_alpha.zip)

(Previous releases are found [here](https://github.com/zawawawa/GatelessGateSharp/releases).)

All you have to do to mine is to download and run the installer, launch the miner, enter your wallet address(es), and click the Start button.
Please refer to **[Prerequisites](#prerequisites)** and **[the online manual](https://github.com/zawawawa/GatelessGateSharp/blob/v1.3/Documentation/TOC.md)** for details.

![Screen Shot](https://i.imgur.com/gsiVgfP.png)

Currently, the miner supports the following algorithms:

* Ethash/Pascal dual-mining
* Ethash
* CryptoNight
* CryptoNight-Light
* CryptoNight-Heavy
* CryptoNightV7
* X16R
* X16S
* NeoScrypt
* Pascal
* Lbry
* LyraREv2

The following major pools are natively supported as default pools:

* NiceHash
* ethermine.org
* ethpool.org
* DwarfPool
* Nanopool
* Mining Pool Hub
* zpool
* mineXMR.com

In addition to the default pools, support for custom pools is also available. 

## <a name="prerequisites"></a>Prerequisites

The minimum requirements for the miner are as follows:

* Graphics card(s) with the AMD GCN architecture and/or the NVIDIA Maxwell and Pascal architectures.
* 64-bit Windows operating system.
* [AMD Radeon Software Adrenalin Edition 18.3.4](http://support.amd.com/en-us/kb-articles/Pages/Radeon-Software-Adrenalin-Edition-18.3.4-Release-Notes.aspx) or later for AMD cards.
* [.NET Framework 4.7.1](https://www.microsoft.com/en-us/download/details.aspx?id=56116) and [Visual C++ Redistributable 2017](https://go.microsoft.com/fwlink/?LinkId=746572) if you do not use the installer.

Please note that the current focus of the project is on AMD and this program has been tested mostly against [AMD Radeon Software Adrenalin Edition 18.1.1](http://support.amd.com/en-us/kb-articles/Pages/Radeon-Software-Adrenalin-Edition-18.1.1-Release-Notes.aspx). Other drivers may or may not work. For the best performance, please **turn off AMD CrossFire, if applicable, and choose Compute for GPU Workload in Radeon Settings.**

![Screen Shot](https://i.imgur.com/TNIBhCa.png)

## About the DEVFEE

This miner has a built-in 1% DEVFEE. I must emphasize that **I absolutely need the DEVFEE in order to continue this project, and I will not provide any support for those who run binaries I did not sign.** This project requires a full-time attention and testing is quite expensive. The DEVFEE is non-negotiable. If you don't like it, please go elsewhere.

## About BIOS Mods

Gateless Gate Sharp does not officialy support BIOS mods for other miners such as Claymore's as there are so many modded BIOS'es out there with outrageous parameters, which indicates that people who made them had no idea about what they were doing. You can of course try them at your own risk, but please don't complain to me if they don't work with GGS.
