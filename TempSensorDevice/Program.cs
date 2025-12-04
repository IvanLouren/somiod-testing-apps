using System;
using System.Threading;

namespace TempSensorDevice
{
    class Program
    {
        private static string DEVICE_APP_NAME = "temp-sensor-001";
        private static string CONTAINER_NAME = "readings";
        private static SomiodClient somiodClient;
        private static NotificationListener listener;

        static void Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("  TEMPERATURE SENSOR DEVICE - Application A");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            // Initialize SOMIOD client
            somiodClient = new SomiodClient("https://localhost:44346");

            try
            {
                // 1. Register device in SOMIOD
                Console.WriteLine("[INIT] Registering device in SOMIOD...");
                RegisterDevice();

                // 2. Create subscription to listen for alert commands
                Console.WriteLine("[INIT] Creating subscription for alerts...");
                CreateSubscription();

                // 3. Start listening for notifications
                Console.WriteLine("[INIT] Starting notification listener...");
                listener = new NotificationListener("localhost", 1883);
                listener.OnNotificationReceived += HandleNotification;
                listener.Start($"api/somiod/{DEVICE_APP_NAME}/#");

                Console.WriteLine();
                Console.WriteLine("✓ Device ready! Listening for notifications...");
                Console.WriteLine("Press ESC to shutdown device");
                Console.WriteLine();

                // 4. Simulate periodic temperature readings
                StartTemperatureSimulation();

                // Keep running until ESC pressed
                while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                // Cleanup
                Console.WriteLine("\n[SHUTDOWN] Cleaning up...");
                listener?.Stop();
                CleanupDevice();
            }
        }

        static void RegisterDevice()
        {
            // Create application
            var app = somiodClient.CreateApplication(DEVICE_APP_NAME);
            Console.WriteLine($"  ✓ Application created: {app.resource_name}");

            // Create container for readings
            var container = somiodClient.CreateContainer(DEVICE_APP_NAME, CONTAINER_NAME);
            Console.WriteLine($"  ✓ Container created: {container.resource_name}");
        }

        static void CreateSubscription()
        {
            // Subscribe to creation events (for alert commands from dashboard)
            var sub = somiodClient.CreateSubscription(
                DEVICE_APP_NAME,
                CONTAINER_NAME,
                "alert-subscription",
                evt: 1, // Creation events
                endpoint: "mqtt://localhost:1883"
            );
            Console.WriteLine($"  ✓ Subscription created: {sub.resource_name}");
        }

        static void StartTemperatureSimulation()
        {
            Thread simulationThread = new Thread(() =>
            {
                Random rand = new Random();
                while (true)
                {
                    try
                    {
                        // 1. Simular temperatura (18-24°C)
                        double temp = 18.0 + (rand.NextDouble() * 6.0);

                        // 2. Criar o XML do conteúdo
                        string contentXml = $"<temp>{temp:F1}</temp>";

                        // 3. ENVIAR PARA O MIDDLEWARE (Faltava isto!)
                        // Nome do recurso único para cada leitura (ex: reading-123456)
                        string readingName = $"reading-{DateTime.Now.Ticks}";

                        somiodClient.CreateContentInstance(
                            DEVICE_APP_NAME,
                            CONTAINER_NAME,
                            readingName,
                            "application/xml",
                            contentXml
                        );

                        Console.WriteLine($"[SENT] {readingName}: {temp:F1}°C");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR SENDING]: {ex.Message}");
                    }

                    Thread.Sleep(10000); // 10 segundos
                }
            });
            simulationThread.IsBackground = true;
            simulationThread.Start();
        }

        static void HandleNotification(string topic, string payload)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("  🔔 NOTIFICATION RECEIVED!");
            Console.WriteLine("========================================");
            Console.WriteLine($"Topic: {topic}");
            Console.WriteLine($"Payload:\n{payload}");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // Serialize to XML with validation
            try
            {
                XmlNotificationSerializer.SerializeAndValidate(payload, DEVICE_APP_NAME);
                Console.WriteLine("✓ Notification saved to XML and validated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ XML serialization failed: {ex.Message}");
            }

            // TODO: Parse notification and take action (e.g., trigger alarm)
        }

        static void CleanupDevice()
        {
            try
            {
                // Optional: Delete application from SOMIOD on shutdown
                // somiodClient.DeleteApplication(DEVICE_APP_NAME);
                Console.WriteLine("  ✓ Device shutdown complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Cleanup error: {ex.Message}");
            }
        }
    }
}