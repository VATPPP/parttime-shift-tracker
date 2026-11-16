# Part-Time Shift Tracker

An offline-first Android application engineered for shift and hourly workers to track irregular work schedules, compute multi-tier wage calculations, and analyze earnings. Built with **.NET MAUI**, **C#**, and **SQLite**.

---

## Overview

Hourly and part-time workers often face irregular work shifts, complex pay rates (base wages, overtime multipliers, allowances), and fragmented tracking methods. Many commercial solutions require persistent internet connections, demand invasive account registrations, or pose privacy risks with sensitive financial data.

**Part-Time Shift Tracker** provides a reliable, privacy-focused alternative built on an **Offline-First** architecture. All data persistence and wage calculations remain 100% on-device, delivering instant performance without relying on remote servers.

---

## Technical Architecture & Engineering Highlights

The project adheres to clean architecture principles with a strong focus on modularity, testability, and native Android performance:

### 1. MVVM & Reactive Presentation
* **State Management:** Built on `CommunityToolkit.Mvvm` utilizing C# source generators (`[ObservableProperty]`, `[RelayCommand]`) to minimize boilerplate, reduce allocations, and ensure clean decoupled data binding.
* **Separation of Concerns:** Strict structural boundaries across Views (XAML), ViewModels, Domain Models, and Data Access Services.

### 2. High-Performance Local Persistence
* **Engine:** Powered by `sqlite-net-pcl` and native SQLite binaries (`SQLitePCLRaw`).
* **Reliability:** Complete offline persistence supporting ACID transactions with zero network overhead.

### 3. Native Android Platform Integration
* **Home Screen App Widget (`ShiftWidgetProvider`):** Implemented native Android `RemoteViews` and `AppWidgetProvider` to surface glanceable weekly shift schedules directly on the Android home launcher.
* **Midnight Sync Alarm (`ACTION_MIDNIGHT_SYNC`):** Integrated `SCHEDULE_EXACT_ALARM` broadcast receivers to automatically synchronize and refresh the home screen widget at midnight day boundaries.
* **Over-The-Air (OTA) In-App Self-Update:** Custom update pipeline integrating the GitHub Releases REST API, buffered stream downloads with real-time progress reporting, and native Android `FileProvider` (`REQUEST_INSTALL_PACKAGES`) for seamless sideloading.

### 4. Build & Runtime Optimizations
* **Compile-Time XAML Inflation (`MauiXamlInflator: SourceGen`):** Leverages compile-time source generation instead of runtime reflection for faster startup latency and lower memory overhead.

---

## Core Features

* **Shift Management:** Fast logging with custom start/end times, automatic break deductions, and flexible wage multipliers.
* **Interactive Shift Calendar:** Monthly and weekly calendar views providing clear visual scheduling context.
* **Wage Analytics Dashboard:** Comprehensive monthly summaries detailing total hours worked, estimated earnings, and wage breakdowns.
* **Native Android Widget:** Weekly schedule monitoring directly from the home screen without opening the application.
* **In-App Updater:** Integrated one-tap check and installation for application updates.

---

## Installation & Distribution

### For End Users (Mobile Only)
No development tools or computer required:
1. Open the **Releases** page on an Android device (Android 10+ / API Level 29+).
2. Download the latest signed `ShiftTracker.apk`.
3. Tap the file to install (grant *"Install unknown apps"* if prompted) and launch.

---

## Developer Guide & Build Setup

### Prerequisites
* .NET 10.0 SDK with the `.NET MAUI` workload.
* Visual Studio 2022 (v17.12+) with the *.NET Multi-platform App UI development* workload.
* Android SDK (API Level 29+ / Android 10.0 or higher).

### Build from Source
```bash
# Clone the repository
git clone https://github.com/VATPPP/parttime-shift-tracker.git
cd parttime-shift-tracker

# Restore dependencies
dotnet restore

# Build signed APK for Release
dotnet publish src/ShiftTracker.App/ShiftTracker.App.csproj \
    -f net10.0-android \
    -c Release \
    -p:AndroidPackageFormat=apk
```

---

## Technical Stack Summary

| Layer | Technology |
| :--- | :--- |
| **Framework** | .NET MAUI (.NET 10.0) |
| **Target Platform** | Android 10.0+ (API Level 29+) |
| **Language** | C# 13 |
| **Architecture** | Model-View-ViewModel (MVVM) |
| **MVVM Toolkit** | CommunityToolkit.Mvvm (v8.4.2) |
| **Database** | SQLite (`sqlite-net-pcl`, `SQLitePCLRaw`) |
| **Platform APIs** | Android AppWidgets, Exact Alarm Manager, FileProvider, Intent Services |

---

## License

This project is licensed under the [MIT License](LICENSE).
