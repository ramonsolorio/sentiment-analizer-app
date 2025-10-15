# 📊 Escalado Inteligente con Log Analytics, KEDA y Azure Container Apps

## ✅ Solución Implementada: KEDA con Log Analytics

**Importante**: La aplicación ahora utiliza **KEDA (Kubernetes Event Driven Autoscaling)** con el **Azure Log Analytics Scaler** para escalado basado en eventos de sentimientos negativos.

---

## 🎯 Solución Implementada: Escalado basado en Eventos + Telemetría

### Arquitectura

```
Usuario → Frontend → Backend (ACA) → Azure OpenAI
                         ↓
                 Application Insights
                    (registra eventos)
                         ↓
                 Log Analytics Workspace
                    (almacena eventos)
                         ↓
                   ┌─────────────────┐
                   │ KEDA Scaler     │
                   │ Poll cada 30s   │
                   │ Query KQL:      │
                   │ Count negative  │
                   │ events (5 min)  │
                   └─────────────────┘
                         ↓
         ┌────────────────────────────┐
         │ ACA Scaling Decision       │
         │ - ≥ 5 eventos → Scale UP   │
         │ - < 5 eventos → Scale DOWN │
         │ - Min replicas: 1          │
         │ - Max replicas: 10         │
         └────────────────────────────┘
```

### Cómo Funciona

1. **Usuario envía requests** con textos que contienen diferentes sentimientos
2. **Backend analiza** con Azure OpenAI y detecta sentimientos (Positive, Negative, Neutral)
3. **Backend envía eventos** a Application Insights con `SentimentAnalyzed` event
4. **Application Insights almacena** eventos en Log Analytics Workspace
5. **KEDA consulta Log Analytics** cada 30 segundos con query KQL
6. **Query cuenta eventos negativos** en los últimos 5 minutos
7. **Si count ≥ 5**: KEDA indica a ACA que escale UP (agregar réplica)
8. **Si count < 5 durante 5 min**: KEDA indica a ACA que escale DOWN (remover réplica)
9. **Application Insights monitorea** toda la telemetría para observabilidad

---

## 🚀 Configuración de Escalado HTTP en ACA

### Comando para aplicar escalado basado en requests HTTP

```bash
az containerapp update \
  --name sentiment-analyzer-backend-aca \
  --resource-group ACA-DEMO-RG \
  --min-replicas 1 \
  --max-replicas 10 \
  --scale-rule-name http-scaling-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 5
```

### Explicación de Parámetros

- **`--min-replicas 1`**: Siempre mantiene 1 instancia corriendo (para demo)
- **`--max-replicas 10`**: Escala hasta 10 instancias máximo
- **`--scale-rule-http-concurrency 5`**: Nueva réplica cada 5 requests concurrentes

### Comportamiento Esperado

| Requests Concurrentes | Réplicas Activas |
|-----------------------|------------------|
| 1-5                   | 1                |
| 6-10                  | 2                |
| 11-15                 | 3                |
| 16-20                 | 4                |
| 45-50                 | 10 (máximo)      |

---

## 📈 Métricas en Application Insights

### Métricas Personalizadas que Enviamos

1. **`NegativeSentimentCount`** (número)
   - Se incrementa cada vez que se detecta sentimiento negativo
   - Usada para monitoreo y dashboards

2. **`Sentiment_Positive`** (número)
   - Cuenta sentimientos positivos

3. **`Sentiment_Negative`** (número)
   - Cuenta sentimientos negativos

4. **`Sentiment_Neutral`** (número)
   - Cuenta sentimientos neutrales

### Eventos Personalizados

1. **`SentimentAnalyzed`**
   - Properties: Sentiment, Message
   - Metrics: Score

2. **`NegativeSentimentDetected`**
   - Properties: Text, Severity
   - Metrics: NegativeScore

---

## 🔍 Queries de Log Analytics (Kusto)

### Ver Sentimientos Negativos en Tiempo Real

```kusto
customMetrics
| where name == "NegativeSentimentCount"
| where timestamp > ago(1h)
| summarize TotalNegative = sum(value) by bin(timestamp, 1m)
| render timechart
```

### Ver Distribución de Sentimientos

```kusto
customMetrics
| where name startswith "Sentiment_"
| where timestamp > ago(1h)
| summarize Count = sum(value) by name
| render piechart
```

### Correlación entre Requests y Sentimientos Negativos

```kusto
let negatives = customMetrics
| where name == "NegativeSentimentCount"
| where timestamp > ago(1h)
| summarize NegativeCount = sum(value) by bin(timestamp, 1m);
let requests = requests
| where timestamp > ago(1h)
| summarize RequestCount = count() by bin(timestamp, 1m);
negatives
| join kind=inner requests on timestamp
| project timestamp, NegativeCount, RequestCount
| render timechart
```

### Ver Eventos de Sentimientos Negativos

```kusto
customEvents
| where name == "NegativeSentimentDetected"
| where timestamp > ago(1h)
| project timestamp, 
          text = tostring(customDimensions.Text),
          severity = tostring(customDimensions.Severity),
          score = todouble(customMeasurements.NegativeScore)
| order by timestamp desc
```

---

## 🎯 Solución Alternativa: Azure Monitor Alert + Azure Functions

Si necesitas escalado **específicamente** basado en sentimientos negativos > 5, puedes implementar:

### Arquitectura Avanzada

```
Application Insights
    → Metric (NegativeSentimentCount)
        → Azure Monitor Alert Rule
            → Azure Logic App / Function
                → Az CLI: Update ACA replicas
```

### Pasos

1. **Crear Azure Monitor Alert Rule**

```bash
az monitor metrics alert create \
  --name negative-sentiment-alert \
  --resource-group ACA-DEMO-RG \
  --scopes /subscriptions/YOUR_SUB/resourceGroups/ACA-DEMO-RG/providers/microsoft.insights/components/appinsights-sentiment-analyzer \
  --condition "count customMetrics/NegativeSentimentCount > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action /subscriptions/YOUR_SUB/resourceGroups/ACA-DEMO-RG/providers/Microsoft.Logic/workflows/scale-up-aca
```

2. **Logic App que escala ACA**

```json
{
  "actions": {
    "HTTP": {
      "type": "Http",
      "inputs": {
        "method": "PATCH",
        "uri": "https://management.azure.com/subscriptions/YOUR_SUB/resourceGroups/ACA-DEMO-RG/providers/Microsoft.App/containerApps/sentiment-analyzer-backend-aca?api-version=2023-05-01",
        "authentication": {
          "type": "ManagedServiceIdentity"
        },
        "body": {
          "properties": {
            "template": {
              "scale": {
                "minReplicas": 3,
                "maxReplicas": 10
              }
            }
          }
        }
      }
    }
  }
}
```

---

## 📊 Dashboard de Monitoreo

### Crear Dashboard en Azure Portal

1. Ir a **Application Insights** → **Workbooks**
2. Crear nuevo Workbook con estas queries:

**Panel 1: Sentimientos por Minuto**
```kusto
customMetrics
| where name startswith "Sentiment_"
| where timestamp > ago(30m)
| summarize Count = sum(value) by name, bin(timestamp, 1m)
| render timechart
```

**Panel 2: Instancias de ACA vs Sentimientos Negativos**
```kusto
customMetrics
| where name == "NegativeSentimentCount"
| where timestamp > ago(30m)
| summarize NegativeCount = sum(value) by bin(timestamp, 1m)
| render timechart
```

**Panel 3: Mapa de Calor de Sentimientos**
```kusto
customEvents
| where name == "SentimentAnalyzed"
| where timestamp > ago(1h)
| extend sentiment = tostring(customDimensions.Sentiment)
| summarize count() by sentiment, bin(timestamp, 5m)
| render columnchart
```

---

## ✅ Ventajas de la Solución Implementada

1. ✅ **Telemetría completa** en Application Insights
2. ✅ **Escalado automático** basado en carga HTTP
3. ✅ **Métricas personalizadas** para análisis
4. ✅ **Dashboards visuales** para monitoreo
5. ✅ **Alertas configurables** para casos críticos
6. ✅ **Cost-effective** (escala según demanda real)

---

## 🚀 Próximos Pasos

1. **Aplicar regla de escalado HTTP** (ver comando arriba)
2. **Generar carga de prueba** con textos negativos
3. **Monitorear en tiempo real**:
   - App Insights → Live Metrics
   - ACA → Metrics → Replica Count
   - App Insights → Logs → Run Kusto queries

4. **Crear alertas** para casos críticos:
   - > 100 sentimientos negativos en 5 minutos
   - Score promedio < -0.8

---

## 📝 Notas Importantes

- **La telemetría tarda ~30-60 segundos** en aparecer en App Insights
- **El escalado de ACA tarda ~10-20 segundos** en crear nuevas réplicas
- **Para demo**: Usa `min-replicas: 1` para ver escalado inmediato
- **Para producción**: Usa `min-replicas: 0` para ahorrar costos

---

## 🎬 Script de Prueba

Ver `test-negative-sentiments.ps1` para generar carga de prueba automáticamente.

El script:
1. Envía 50 requests con textos negativos
2. Monitorea el número de réplicas de ACA
3. Muestra métricas de App Insights en tiempo real

