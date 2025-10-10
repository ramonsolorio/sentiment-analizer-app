# 📈 Escenarios de Escalado para Sentiment Analyzer

## Resumen Ejecutivo

Este documento describe varios escenarios de escalado event-driven para la aplicación de análisis de sentimientos, con arquitecturas, implementaciones y casos de uso reales.

---

## 🏆 Escenario #1: Análisis Masivo con Azure Service Bus (RECOMENDADO)

### Caso de Uso
**Empresa de Marketing**: Analizar 100,000+ comentarios de redes sociales sobre el lanzamiento de un producto nuevo.

### Arquitectura Propuesta

```
┌─────────────────┐
│  Data Ingestion │ (Simulador o API externa)
│   Service       │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────────────────┐
│         Azure Service Bus Queue                 │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐          │
│  │ Msg1 │ │ Msg2 │ │ Msg3 │ │ ...  │          │
│  └──────┘ └──────┘ └──────┘ └──────┘          │
└────────┬────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────┐
│    Azure Container Apps (Worker Service)       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │Instance 1│  │Instance 2│  │Instance N│     │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘     │
│       │             │             │            │
│       └─────────────┴─────────────┘            │
│                     │                          │
│           ┌─────────▼─────────┐               │
│           │   Azure OpenAI    │               │
│           │   GPT-4.1         │               │
│           └─────────┬─────────┘               │
└─────────────────────┼─────────────────────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │   Azure Cosmos DB      │
         │  (Resultados + Stats)  │
         └────────────────────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │  Frontend Dashboard    │
         │  (Visualización)       │
         └────────────────────────┘
```

### Componentes Nuevos

#### 1. **Azure Service Bus Queue**
```bash
# Crear namespace
az servicebus namespace create \
  --name sb-sentiment-analyzer \
  --resource-group ACA-DEMO-RG \
  --location centralus \
  --sku Standard

# Crear cola
az servicebus queue create \
  --name sentiment-analysis-queue \
  --namespace-name sb-sentiment-analyzer \
  --resource-group ACA-DEMO-RG \
  --max-delivery-count 5 \
  --lock-duration PT5M
```

#### 2. **Worker Service (.NET 8)**
Nuevo proyecto que consume mensajes de la cola:

```csharp
// SentimentWorker.cs
public class SentimentWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ISentimentService _sentimentService;
    private readonly ICosmosDbService _cosmosDbService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var message = JsonSerializer.Deserialize<SentimentMessage>(
            args.Message.Body.ToString());
        
        // Analizar sentimiento con Azure OpenAI
        var result = await _sentimentService.AnalyzeSentimentAsync(
            message.Text);
        
        // Guardar en Cosmos DB
        await _cosmosDbService.SaveResultAsync(new SentimentResult
        {
            Id = Guid.NewGuid(),
            OriginalText = message.Text,
            Sentiment = result.Sentiment,
            Score = result.Score,
            ProcessedAt = DateTime.UtcNow,
            Source = message.Source
        });
        
        // Marcar mensaje como completado
        await args.CompleteMessageAsync(args.Message);
    }
}
```

#### 3. **KEDA Scaler para ACA**
Configuración de escalado automático basado en tamaño de cola:

```yaml
# ACA Scale Configuration
scale:
  minReplicas: 0           # Escala a 0 cuando no hay mensajes
  maxReplicas: 50          # Hasta 50 instancias en pico
  rules:
    - name: azure-servicebus-queue-rule
      type: azure-servicebus
      metadata:
        queueName: sentiment-analysis-queue
        namespace: sb-sentiment-analyzer
        messageCount: "10"   # Nueva instancia cada 10 mensajes
```

Comando para aplicar:
```bash
az containerapp update \
  --name sentiment-worker-aca \
  --resource-group ACA-DEMO-RG \
  --min-replicas 0 \
  --max-replicas 50 \
  --scale-rule-name servicebus-queue-scale \
  --scale-rule-type azure-servicebus \
  --scale-rule-metadata "queueName=sentiment-analysis-queue" \
                        "namespace=sb-sentiment-analyzer" \
                        "messageCount=10"
```

#### 4. **Azure Cosmos DB**
```bash
# Crear cuenta de Cosmos DB
az cosmosdb create \
  --name cosmos-sentiment-analyzer \
  --resource-group ACA-DEMO-RG \
  --locations regionName=centralus

# Crear base de datos
az cosmosdb sql database create \
  --account-name cosmos-sentiment-analyzer \
  --resource-group ACA-DEMO-RG \
  --name SentimentAnalysis

# Crear contenedor
az cosmosdb sql container create \
  --account-name cosmos-sentiment-analyzer \
  --database-name SentimentAnalysis \
  --name Results \
  --partition-key-path "/source" \
  --throughput 400
```

#### 5. **Data Ingestion Service** (Simulador)
Script PowerShell para generar carga de prueba:

```powershell
# simulate-load.ps1
$serviceBusConnectionString = "YOUR_CONNECTION_STRING"
$queueName = "sentiment-analysis-queue"

$messages = @(
    "¡Me encanta este producto! Es increíble.",
    "Muy decepcionado con el servicio al cliente.",
    "El precio está bien, pero la calidad es mala.",
    "Excelente experiencia, lo recomiendo 100%",
    "No volvería a comprar aquí nunca más."
)

# Enviar 10,000 mensajes
for ($i = 0; $i -lt 10000; $i++) {
    $randomText = $messages | Get-Random
    $message = @{
        text = "$randomText (mensaje #$i)"
        source = "twitter"
        timestamp = (Get-Date).ToString("o")
    } | ConvertTo-Json
    
    # Enviar a Service Bus
    Send-ServiceBusMessage -ConnectionString $serviceBusConnectionString `
                          -QueueName $queueName `
                          -Message $message
    
    if ($i % 100 -eq 0) {
        Write-Host "Enviados $i mensajes..."
    }
}
```

### Métricas de Escalado

| Mensajes en Cola | Réplicas Activas | Throughput Estimado |
|-----------------|------------------|---------------------|
| 0-10            | 0-1              | 10-20 msg/min       |
| 10-100          | 1-10             | 100-200 msg/min     |
| 100-500         | 10-50            | 500-1000 msg/min    |
| 500+            | 50 (max)         | 1000+ msg/min       |

### Dashboard de Visualización

Nuevo componente en el frontend para mostrar:
- ✅ Total de mensajes procesados
- ✅ Distribución de sentimientos (Positivo/Neutral/Negativo)
- ✅ Gráfico de tendencias en tiempo real
- ✅ Fuentes más activas
- ✅ Palabras clave más mencionadas

---

## 🎯 Escenario #2: Análisis de Emails con Event Grid

### Caso de Uso
**Departamento de Soporte**: Analizar automáticamente emails de clientes y priorizar casos urgentes.

### Arquitectura

```
┌─────────────────┐
│  Email System   │ (Outlook, Gmail)
└────────┬────────┘
         │ Forward/Export
         ▼
┌──────────────────────────┐
│ Azure Storage Account    │
│  (Blob Container)        │
└────────┬─────────────────┘
         │ Blob Created Event
         ▼
┌──────────────────────────┐
│   Azure Event Grid       │
└────────┬─────────────────┘
         │
         ▼
┌──────────────────────────┐
│ ACA Backend (HTTP)       │
│  - Procesa evento        │
│  - Analiza sentimiento   │
└────────┬─────────────────┘
         │
         ├──→ Cosmos DB (Resultados)
         │
         └──→ Logic App (Si sentimiento muy negativo)
                 │
                 └──→ Enviar email a manager
                 └──→ Crear ticket de Zendesk
```

### Implementación

```csharp
// EventGridController.cs
[ApiController]
[Route("api/eventgrid")]
public class EventGridController : ControllerBase
{
    [HttpPost("email-analysis")]
    public async Task<IActionResult> HandleEmailEvent(
        [FromBody] EventGridEvent[] events)
    {
        foreach (var eventGridEvent in events)
        {
            // Validar suscripción
            if (eventGridEvent.EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var validationCode = ((JObject)eventGridEvent.Data)["validationCode"];
                return Ok(new { validationResponse = validationCode });
            }

            // Procesar blob creado
            if (eventGridEvent.EventType == "Microsoft.Storage.BlobCreated")
            {
                var blobUrl = eventGridEvent.Data.ToString();
                
                // Descargar email del blob
                var emailContent = await _blobService.DownloadBlobAsync(blobUrl);
                
                // Analizar sentimiento
                var sentiment = await _sentimentService.AnalyzeSentimentAsync(
                    emailContent);
                
                // Si es muy negativo, disparar alerta
                if (sentiment.Score < -0.7)
                {
                    await _alertService.SendUrgentAlertAsync(sentiment);
                }
            }
        }
        
        return Ok();
    }
}
```

### Configuración Event Grid

```bash
# Crear suscripción de eventos
az eventgrid event-subscription create \
  --name email-sentiment-subscription \
  --source-resource-id /subscriptions/YOUR_SUB/resourceGroups/ACA-DEMO-RG/providers/Microsoft.Storage/storageAccounts/YOUR_STORAGE \
  --endpoint https://sentiment-analyzer-backend-aca.braveflower-b755a572.centralus.azurecontainerapps.io/api/eventgrid/email-analysis \
  --endpoint-type webhook \
  --included-event-types Microsoft.Storage.BlobCreated
```

---

## 📊 Escenario #3: Reviews de Productos con Event Hub

### Caso de Uso
**E-commerce**: Procesar 50,000+ reviews diarias en tiempo real.

### Arquitectura

```
┌──────────────┐
│ Web Frontend │ (Usuarios escribiendo reviews)
└──────┬───────┘
       │
       ▼
┌──────────────────────┐
│  Azure Event Hub     │ (Streaming)
│  - Particiones: 32   │
└──────┬───────────────┘
       │
       ▼
┌────────────────────────────────┐
│ ACA Worker (múltiples réplicas)│
│  - Consume eventos             │
│  - Analiza con OpenAI          │
└──────┬─────────────────────────┘
       │
       ├──→ Azure SQL (Reviews + Sentiments)
       │
       └──→ Azure SignalR (Notificaciones en tiempo real)
              │
              └──→ Dashboard Frontend (Gráficos live)
```

### Ventajas
- ✅ Throughput masivo (millones de eventos/día)
- ✅ Particionado para paralelismo
- ✅ Retención de eventos (7 días)
- ✅ Integración con Stream Analytics

---

## 💰 Comparación de Costos

| Escenario | Costo Mensual Estimado | Mejor Para |
|-----------|------------------------|------------|
| Service Bus Queue | $20-50 | Procesamiento batch, carga variable |
| Event Grid | $0.60-2 | Eventos poco frecuentes, integración |
| Event Hub | $100-300 | Alto throughput, streaming continuo |

---

## 🚀 Plan de Implementación Recomendado

### Fase 1: Service Bus + Worker (Semana 1)
1. Crear Azure Service Bus namespace y queue
2. Crear Worker Service (.NET 8)
3. Implementar KEDA scaler en ACA
4. Crear simulador de carga

### Fase 2: Almacenamiento y Visualización (Semana 2)
1. Crear Cosmos DB
2. Implementar repositorio de datos
3. Crear API de consulta de resultados
4. Implementar dashboard en frontend

### Fase 3: Monitoreo y Optimización (Semana 3)
1. Configurar Application Insights
2. Crear alertas de rendimiento
3. Optimizar throughput
4. Documentar métricas

---

## 📋 Checklist de Implementación

### Infraestructura
- [ ] Azure Service Bus namespace creado
- [ ] Cola configurada con dead-letter
- [ ] Cosmos DB cuenta y contenedor creados
- [ ] Connection strings en Key Vault
- [ ] Managed Identity configurada

### Código
- [ ] Worker Service creado
- [ ] Service Bus client implementado
- [ ] Cosmos DB repository implementado
- [ ] API de resultados creada
- [ ] Dashboard frontend actualizado

### Configuración ACA
- [ ] Worker desplegado en ACA
- [ ] KEDA scaler configurado
- [ ] Variables de entorno configuradas
- [ ] Min replicas = 0 configurado
- [ ] Max replicas = 50 configurado

### Testing
- [ ] Simulador de carga funcional
- [ ] Escalado automático validado
- [ ] Throughput medido
- [ ] Dead-letter queue monitoreada
- [ ] Dashboard mostrando datos en tiempo real

---

## 📚 Recursos Adicionales

- [Azure Service Bus Documentation](https://docs.microsoft.com/azure/service-bus-messaging/)
- [KEDA Scalers](https://keda.sh/docs/scalers/azure-service-bus/)
- [Azure Container Apps Scaling](https://docs.microsoft.com/azure/container-apps/scale-app)
- [Cosmos DB Best Practices](https://docs.microsoft.com/azure/cosmos-db/best-practices)

---

## 🎯 Conclusión

**Recomendación**: Implementar el **Escenario #1 (Service Bus Queue)** porque ofrece:
- ✅ Mejor demostración de escalado automático
- ✅ Costo-efectivo (escala a 0)
- ✅ Fácil de simular y medir
- ✅ Arquitectura production-ready
- ✅ Patrón aplicable a múltiples casos de uso

¿Quieres que comencemos con la implementación? 🚀
