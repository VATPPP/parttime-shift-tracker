# Part-Time Shift Tracker

An offline-first Android application designed to help shift workers log working hours and calculate wages. Built entirely with C# and .NET MAUI.

## Overview
This application serves as a personal tool to manage part-time work schedules. It provides a secure, completely offline environment to track hours and calculate expected income without relying on external servers or internet connectivity.

*(Features list will be updated as development progresses)*

## Technology Stack
- Platform: Android (API Level 29+)
- Framework: .NET MAUI (.NET 8.0)
- Programming Language: C#
- Architecture Pattern: MVVM (Model-View-ViewModel)
- Database: SQLite 

## Repository Structure
The project is structured to separate concerns, adhering to the MVVM pattern:

- /src/ShiftTracker.App/
  - /Models: Contains data entities.
  - /ViewModels: Contains presentation logic and state management.
  - /Views: Contains XAML UI definitions.
  - /Services: Contains local database operations and core logic.

## How to Build and Run

### Prerequisites
- Visual Studio 2022 with the ".NET Multi-platform App UI development" workload installed.
- An Android device with "Developer Options" and "USB Debugging" enabled.

### Steps
1. Clone this repository to your local machine:
   git clone https://github.com/VATPPP/parttime-shift-tracker.git
2. Open the solution file (.sln) in Visual Studio.
3. Connect your Android device via USB.
4. Select your connected device from the target dropdown menu in Visual Studio.
5. Press F5 to build the application and deploy it directly to your device.

## License
Distributed under the MIT License. See the LICENSE file in the repository for more information.
