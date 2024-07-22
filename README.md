# Network Sniffer Using C#

This is a network packet sniffer application built using C# and the SharpPcap library. It allows users to capture and analyze network packets in real-time.

## Features

- Capture live network packets
- Display detailed information about each packet
- Filter packets based on criteria
- Save captured packets to a file

## Prerequisites

- Visual Studio 2022
- .NET Framework
- SharpPcap library
- PacketDotNet library

## Installation

1. Clone the repository:
    ```sh
    git clone https://github.com/Rohit999zzz/Network-Sniffer-Using-C-.git
    ```
2. Open the solution file `PacketSniffer.sln` in Visual Studio.
3. Restore the NuGet packages:
    ```sh
    dotnet restore
    ```

## Usage

1. Select a network interface to capture packets from.
2. Click the "Start" button to begin capturing packets.
3. Captured packets will be displayed in the list view.
4. Click on a packet to view detailed information.
5. Use the filter textbox to filter packets based on specific criteria.
6. Click the "Stop" button to stop capturing packets.

## Files and Directories

- `App.config`: Configuration file for the application.
- `Interfaces.cs`: Contains the logic for network interface selection.
- `MainForm.cs`: Main form for the application, including UI and event handlers.
- `MainForm.Designer.cs`: Auto-generated file containing UI design code.
- `MainForm.resx`: Resource file for the main form.
- `packages.config`: NuGet package configuration file.
- `Program.cs`: Entry point for the application.

## .gitignore

The `.gitignore` file should include the following to exclude unnecessary files:

