# Redragon Horus K622 RGB Controller

![Project Banner](multimedia/portada-prev3.png)

A high-performance, real-time RGB lighting controller for the **Redragon Horus TKL K622** keyboard. This application migrates the original Python-based controller to a modern **C# .NET 8.0 WPF** architecture, providing a smoother experience, lower CPU usage, and professional UI.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-0078D6.svg)](https://www.microsoft.com/windows)

---

## 📸 Preview

<div align="center">
  <img src="multimedia/image-prev1.png" width="45%" alt="Main Interface" />
  <img src="multimedia/image-prev2.png" width="45%" alt="Audio Visualizer" />
  <br />
  <img src="multimedia/image-prev3.png" width="60%" alt="Configuration" />
</div>

---

## ✨ Key Features

- 🎵 **Real-time Audio Visualization:** Synchronize your keyboard lighting with system audio (music, games, videos) using high-precision FFT analysis.
- 🔔 **Notification Alerts:** Flash specific colors or patterns when you receive Windows notifications.
- ⌨️ **Virtual Keyboard Preview:** Real-time visual representation of your keyboard's current RGB state within the app.
- 🎨 **Custom Static Modes:** Create and save your own color profiles and static lighting layouts.
- 🚀 **Performance Optimized:** Built with C# and .NET 8.0 for near-zero latency and minimal resource impact.
- 📥 **System Tray Support:** Run in the background with easy access from the system tray.

---

## 🛠️ Built With

The project leverages several powerful libraries to interact with hardware and process audio:

- **[C# / WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)** - Modern UI framework.
- **[NAudio](https://github.com/naudio/NAudio)** - Audio processing and system sound capture.
- **[FftSharp](https://github.com/swharden/FftSharp)** - Fast Fourier Transform for audio frequency analysis.
- **[HidLibrary](https://github.com/mikeobrien/HidLibrary)** - Communication with USB HID devices.
- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)** - Professional MVVM pattern implementation.
- **[Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon)** - System tray and notification management.

---

## ⚙️ Requirements

- **Operating System:** Windows 10/11 (x64)
- **Keyboard:** Redragon Horus TKL K622 (Sinowealth-based controller)
- **Runtime:** [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

---

## 🚀 Instalación y Uso

### Opción 1: Ejecutar versión compilada (Recomendado)
Para usar la aplicación sin necesidad de compilar el código:
1. Descarga la carpeta `dist/` de este repositorio o descarga el archivo `K622RGBController.exe`.
2. Asegúrate de tener instalado [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
3. Ejecuta `K622RGBController.exe`.

### Opción 2: Compilar desde el código fuente
Si deseas realizar cambios o compilar por tu cuenta, sigue estos pasos:

#### Requisitos previos:
- **Visual Studio 2022** (con la carga de trabajo "Desarrollo de escritorio de .NET") o **VS Code** con el SDK de .NET 8.0 instalado.
- **Git** (opcional, para clonar).

#### Pasos para compilar:
1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com/jioryII/Redragon-k622-audio-rgb-controller.git
   cd Redragon-k622-audio-rgb-controller
   ```
2. **Restaurar dependencias:**
   Abre una terminal en la carpeta del proyecto y ejecuta:
   ```bash
   dotnet restore
   ```
3. **Compilar y publicar (Crear el ejecutable):**
   Para generar el archivo `.exe` optimizado en un solo archivo:
   ```bash
   dotnet publish K622RGBController/K622RGBController.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
   ```
4. **Localizar el ejecutable:**
   El archivo generado se encontrará en:
   `K622RGBController/bin/Release/net8.0-windows/win-x64/publish/K622RGBController.exe`

---

## 🤝 Acknowledgments

This project is a modernized migration and extension. Special thanks to the community efforts in reverse-engineering the Sinowealth HID protocol for Redragon keyboards.

- Original HID protocol research inspired by community projects for Redragon K618/K622.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<p align="center">Made with ❤️ for the Redragon community</p>
