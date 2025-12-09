using System;
using System.Windows.Forms;
using System.Xml.Linq;
using TempSensorDevice; // Namespace onde estão as classes partilhadas

namespace TempDashboard
{
    public partial class Form1 : Form
    {
        // CONFIGURAÇÕES (Verifica se batem certo com a tua App A)
        string middlewareUrl = "https://localhost:44346"; // Confirma a porta do teu projeto Web
        string mqttBroker = "127.0.0.1";

        // Nomes SOMIOD
        string myAppName = "dashboard-b";
        string sensorAppName = "temp-sensor-001"; // Tem de ser igual ao DEVICE_APP_NAME da App A
        string sensorContainer = "readings";      // Tem de ser igual ao CONTAINER_NAME da App A

        SomiodClient client;
        NotificationListener listener;

        public Form1()
        {
            InitializeComponent();
            client = new SomiodClient(middlewareUrl);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Criar a Aplicação Dashboard no Middleware
                client.CreateApplication(myAppName);
                Log("Aplicação Dashboard registada com sucesso.");

                // 2. Criar Subscrição para ouvir a App A
                // O endpoint diz ao MQTT para onde enviar. O 'evt: 1' é para criação de dados.
                string subEndpoint = $"mqtt://{mqttBroker}:1883";
                client.CreateSubscription(sensorAppName, sensorContainer, "sub-dash", 1, subEndpoint);
                Log($"Subscrição criada no container '{sensorContainer}'.");

                // 3. Iniciar o Listener MQTT
                listener = new NotificationListener(mqttBroker);
                listener.OnNotificationReceived += OnMessageReceived;

                // Subscreve ao tópico onde o Middleware publica notificações
                // Formato habitual: api/somiod/APP/CONTAINER
                listener.Start($"api/somiod/{sensorAppName}/{sensorContainer}");
                Log("À escuta de notificações MQTT...");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro de conexão: {ex.Message}. Verifica se o Middleware está a correr.");
            }
        }

        private void OnMessageReceived(string topic, string payload)
        {
            // O evento vem de outra thread, temos de usar Invoke para mexer na UI
            this.Invoke(new Action(() =>
            {
                Log("msg recebida!");

                try
                {
                    // === REQUISITO OBRIGATÓRIO: VALIDAR XSD ===
                    // Guarda o XML na pasta e valida o schema
                    XmlNotificationSerializer.SerializeAndValidate(payload, myAppName);

                    // === LER A TEMPERATURA ===
                    // O payload da notificação diz-nos o NOME do recurso criado (ex: reading-123)
                    dynamic notification = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
                    string resourceName = notification.resource_name;

                    // Agora vamos buscar o conteúdo real desse recurso à API
                    var data = client.GetContentInstance(sensorAppName, sensorContainer, resourceName);
                    string contentXml = data.content; // Ex: <temp>25.5</temp>

                    // Parse do XML da temperatura
                    XElement xml = XElement.Parse(contentXml);
                    double temp = double.Parse(xml.Value, System.Globalization.CultureInfo.InvariantCulture);

                    // Atualizar UI
                    lblTemp.Text = $"{temp:F1} ºC";

                    // === LÓGICA DE CONTROLO ===
                    if (temp > 25.0)
                    {
                        Log($"ALERTA: {temp}ºC é muito alto! A enviar comando...");
                        SendCommand("FAN_ON");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Erro a processar: {ex.Message}");
                }
            }));
        }

        private void SendCommand(string cmd)
        {
            // Para controlar a App A, criamos um content-instance no container dela
            try
            {
                string xmlContent = $"<cmd>{cmd}</cmd>";
                string name = "cmd-" + DateTime.Now.Ticks;
                client.CreateContentInstance(sensorAppName, sensorContainer, name, "application/xml", xmlContent);
                Log($"Comando enviado: {cmd}");
            }
            catch (Exception ex)
            {
                Log("Erro ao enviar comando: " + ex.Message);
            }
        }

        private void Log(string msg)
        {
            lstLog.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (listener != null) listener.Stop();
        }

        private void btnSwitch_Click(object sender, EventArgs e)
        {
            SendCommand("MANUAL_TOGGLE");
        }
    }
}
