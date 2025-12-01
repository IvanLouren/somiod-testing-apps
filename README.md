# SOMIOD Testing Applications

Testing suite demonstrating [SOMIOD Middleware](https://github.com/francisco3ferraz/somiod) capabilities.

## 📂 Project Structure

\\\
somiod-testing-apps/
├── TempSensorDevice/          # Application A - IoT Sensor
│   ├── Program.cs
│   ├── SomiodClient.cs
│   ├── NotificationListener.cs
│   ├── XmlNotificationSerializer.cs
│   └── Schemas/
│       └── notification.xsd
└── TempDashboard/             # Application B - Dashboard (coming soon)
\\\

## 🎯 Applications

### Application A - TempSensorDevice ✅
IoT temperature sensor simulator:
- Registers with SOMIOD middleware
- Creates MQTT subscriptions
- Receives notifications
- Serializes to XML with XSD validation
- Simulates temperature readings (18-24°C)

### Application B - TempDashboard 🚧
Control dashboard:
- Sends alerts to sensors
- Views device status
- Discovers SOMIOD resources

## 🚀 Quick Start

### Prerequisites
- .NET Framework 4.8
- SOMIOD Middleware at https://localhost:44346
- Mosquitto MQTT broker at localhost:1883

### Run Application A
\\\powershell
cd TempSensorDevice
# Open in Visual Studio and press F5
\\\

## 🔗 Related
- [SOMIOD Middleware](https://github.com/francisco3ferraz/somiod)

## 📝 Course Project
Integração de Sistemas - ESTG Leiria - 2024/2025
