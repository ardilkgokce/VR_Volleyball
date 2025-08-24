# HTC Vive Setup Guide

Bu branch sadece HTC Vive ve Vive Tracker (Strap+) desteği içerir.

## Gereksinimler

- Unity 2022.3.62f1
- SteamVR yüklü
- HTC Vive veya Vive Pro headset
- Vive Controllers veya Vive Trackers (Strap+ için)

## Unity Proje Ayarları

### 1. XR Plugin Management
1. **Edit > Project Settings > XR Plug-in Management** açın
2. **PC, Mac & Linux Standalone** sekmesinde:
   - ✅ **OpenVR** etkinleştirin
   - ❌ **OpenXR** devre dışı bırakın (veya sadece Vive profile'ları etkinleştirin)

### 2. OpenVR Ayarları (OpenVR kullanıyorsanız)
- **Stereo Rendering Mode**: Multi Pass
- **Mirror View Mode**: OpenVR View

### 3. OpenXR Ayarları (OpenXR kullanıyorsanız)
1. **OpenXR** > **Features** bölümünde:
   - ✅ HTC Vive Controller Profile
   - ✅ HTC Vive Tracker Profile
   - ❌ Oculus Touch Controller Profile (devre dışı)
   - ❌ Meta Quest Support (devre dışı)

## Sahne Kurulumu

### 1. ViveInitializer Ekleyin
Sahneye boş bir GameObject ekleyin ve `ViveInitializer` component'ini ekleyin:
- **Force OpenVR**: ✅ (OpenVR kullanmak için)
- **Auto Initialize XR**: ✅
- **Enable Strap Plus**: Vive Tracker kullanıyorsanız ✅

### 2. VRInputManager Ayarları
`VRInputManager` GameObject'inde:
- **Input Device**: `Controller` (Vive Wands için) veya `ViveTracker` (Strap+ için)
- **Use Trackers For Hands**: Strap+ kullanıyorsanız ✅

### 3. ViveSetupManager Ayarları
`ViveSetupManager` GameObject'inde:
- **Use Vive Trackers**: Tracker kullanıyorsanız ✅
- **Use Strap Plus**: Strap+ sistemi için ✅
- **Left Wrist Tracker Index**: 3 (varsayılan)
- **Right Wrist Tracker Index**: 4 (varsayılan)

## Vive Controller Desteği

Proje otomatik olarak Vive Wand ve Valve Index (Knuckles) controller'larını destekler.

### Controller Input Mapping
- **Trigger**: Topa vurmak için kullanılır
- **Grip**: Gelecek özellikler için rezerve
- **Touchpad/Joystick**: Hareket için (opsiyonel)

## Vive Tracker (Strap+) Desteği

### Tracker Kurulumu
1. SteamVR'da tracker'ları yapılandırın
2. Tracker rollerini belirleyin (el bileği, dirsek vs.)
3. Unity'de `ViveSetupManager`'da tracker index'lerini ayarlayın

### Tracker Pozisyonları
- **Sol Bilek**: Tracker 3
- **Sağ Bilek**: Tracker 4
- Diğer tracker'lar gelecek güncellemeler için

## Sorun Giderme

### "No VR device found" Hatası
1. SteamVR'ın çalıştığından emin olun
2. Vive headset'in bağlı ve açık olduğunu kontrol edin
3. Unity'yi yeniden başlatın

### Tracker'lar Görünmüyor
1. SteamVR'da tracker'ların açık olduğunu kontrol edin
2. Tracker pil seviyelerini kontrol edin
3. `ViveSetupManager`'da **Show Debug Info** açın

### Performance İyileştirmeleri
- **Fixed Timestep**: 0.011111 (90 FPS için)
- **VSync**: Devre dışı bırakın
- **Quality Settings**: VR için optimize edilmiş

## Build Ayarları

1. **File > Build Settings**
2. Platform: **PC, Mac & Linux Standalone**
3. Architecture: **x86_64**
4. **Player Settings**:
   - **XR Settings** > **Virtual Reality Supported**: ✅
   - **Stereo Rendering Path**: Multi Pass veya Single Pass Instanced

## Debug Modu

`ViveSetupManager` üzerinde **Show Debug Info** aktif ederek:
- Bağlı cihazları görün
- Tracker durumlarını kontrol edin
- Controller bağlantılarını test edin