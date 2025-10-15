# Sentiment Analyzer App

## 📖 Overview
Aplicación web cloud-native de análisis de sentimientos que utiliza **Azure OpenAI (GPT-4.1)** para analizar el tono emocional de textos. Desplegada en **Azure Container Apps** con escalado automático **basado en eventos de sentimientos negativos** usando **KEDA y Log Analytics**.

## 🏗️ Arquitectura

- **Frontend**: Angular 12 + TypeScript + Nginx Alpine
- **Backend**: ASP.NET Core 8 Web API + C#
- **IA**: Azure OpenAI Service (GPT-4.1)
- **Hosting**: Azure Container Apps (ACA) con auto-scaling basado en eventos
- **Observabilidad**: Application Insights + Log Analytics
- **Auto-scaling**: KEDA con Log Analytics Scaler (event-driven)
- **Container Registry**: Azure Container Registry (ACR)
- **Autenticación**: Azure DefaultAzureCredential (Azure CLI, Managed Identity, Service Principal)
- **Containerización**: Docker multi-stage builds + Linux Alpine

### 📊 Diagrama de Arquitectura Completo

Para ver los diagramas detallados de la arquitectura, flujos de datos, y componentes de Azure, consulta:
👉 **[ARCHITECTURE.md](ARCHITECTURE.md)** - Incluye 10+ diagramas Mermaid interactivos

**Vista Rápida:**
```
Usuario → Frontend (ACA) → Backend (ACA) → Azure OpenAI
                              ↓
                    Application Insights
                              ↓
                    Log Analytics Workspace
                              ↓
                    KEDA Log Analytics Scaler
                    (Escala cuando negativos ≥ 5)
```

## 📋 Prerrequisitos

### Para Desarrollo Local
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (recomendado v21.6.1)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (opcional)

### Para Despliegue en Azure
- Suscripción de Azure activa
- Acceso a Azure OpenAI Service
- Permisos para crear recursos en Azure (Resource Group, Container Apps, etc.)

## 🌐 URLs de la Aplicación

### Producción (Azure Container Apps)
- **Frontend**: `https://academosampl1.braveflower-b755a572.centralus.azurecontainerapps.io`
- **Backend API**: `https://sentiment-analyzer-backend-aca.braveflower-b755a572.centralus.azurecontainerapps.io`

### Desarrollo Local
- **Frontend**: `http://localhost:4200`
- **Backend API**: `http://localhost:5000`

## 🔐 Configuración de Autenticación

La aplicación utiliza **DefaultAzureCredential** de Azure Identity, que soporta múltiples métodos de autenticación en este orden:

1. **Environment Variables** (Service Principal)
2. **Managed Identity** (recomendado para producción en Azure)
3. **Azure CLI** (recomendado para desarrollo local)
4. **Visual Studio / VS Code**
5. **Interactive Browser**

### ✅ Método Recomendado: Azure CLI (Desarrollo Local)

1. **Iniciar sesión con Azure CLI**:
   ```powershell
   az login
   ```

2. **Seleccionar la suscripción correcta**:
   ```powershell
   az account list --output table
   az account set --subscription "<NOMBRE_O_ID_DE_SUSCRIPCION>"
   ```

3. **Verificar acceso al recurso de Azure OpenAI**:
   ```powershell
   az cognitiveservices account show \
     --name <NOMBRE_RECURSO_OPENAI> \
     --resource-group <NOMBRE_RESOURCE_GROUP>
   ```

> **Nota**: No necesitas especificar tenant ID manualmente. Azure CLI maneja automáticamente el tenant correcto basado en tu suscripción.

### ✅ Método para Producción: Managed Identity

En Azure Container Apps, configura Managed Identity:

```bash
# Habilitar System-assigned Managed Identity
az containerapp identity assign \
  --name sentiment-analyzer-backend-aca \
  --resource-group <RESOURCE_GROUP>

# Asignar rol de acceso a Azure OpenAI
az role assignment create \
  --assignee <PRINCIPAL_ID_DE_MANAGED_IDENTITY> \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/<SUB_ID>/resourceGroups/<RG>/providers/Microsoft.CognitiveServices/accounts/<OPENAI_NAME>
```

### ✅ Método Alternativo: Service Principal

Si necesitas credenciales específicas (CI/CD, testing):

1. **Crear Service Principal**:
   ```powershell
   az ad sp create-for-rbac \
     --name "sentiment-analyzer-sp" \
     --role "Cognitive Services OpenAI User" \
     --scopes /subscriptions/<SUB_ID>/resourceGroups/<RG>/providers/Microsoft.CognitiveServices/accounts/<OPENAI_NAME>
   ```

2. **Configurar variables de entorno** (guardar el output JSON):
   ```powershell
   $env:AZURE_TENANT_ID='<tenant del output>'
   $env:AZURE_CLIENT_ID='<appId del output>'
   $env:AZURE_CLIENT_SECRET='<password del output>'
   ```

> **⚠️ Importante**: Nunca commitees credenciales al repositorio. Usa Azure Key Vault o GitHub Secrets para CI/CD.

## 🚀 Ejecutar Localmente

### Backend (.NET 8)

```powershell
# Navegar al directorio del backend
cd backend

# Restaurar dependencias
dotnet restore

# Configurar variables de entorno (reemplaza con tus valores)
$env:AZURE_OPENAI_ENDPOINT='https://<TU-RECURSO>.openai.azure.com/'
$env:AZURE_OPENAI_DEPLOYMENT='<TU-DEPLOYMENT-NAME>'

# Opcional: Connection string de Application Insights
$env:APPLICATIONINSIGHTS_CONNECTION_STRING='InstrumentationKey=...;IngestionEndpoint=...'

# Ejecutar
dotnet run
```

✅ **Backend disponible en**: `http://localhost:5000`

### Frontend (Angular 12)

```powershell
# Navegar al directorio del frontend
cd frontend

# Instalar dependencias
npm install

# Node.js 17+ requiere esta variable de entorno para webpack
$env:NODE_OPTIONS='--openssl-legacy-provider'

# Ejecutar en modo desarrollo
npm start
```

✅ **Frontend disponible en**: `http://localhost:4200`

## 🐳 Ejecutar con Docker

### Docker Compose (Desarrollo Local)

```powershell
# En la raíz del proyecto, crear archivo .env
@"
AZURE_OPENAI_ENDPOINT=https://<TU-RECURSO>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT=<TU-DEPLOYMENT-NAME>
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...
"@ | Out-File -FilePath .env -Encoding UTF8

# Construir y ejecutar
docker-compose up --build
```

- Frontend: `http://localhost:4200`
- Backend: `http://localhost:5000`

### Docker Individual (Linux containers)

```powershell
# Backend
cd backend
docker build --platform linux/amd64 -t sentiment-backend:latest .
docker run -p 5000:8080 \
  -e AZURE_OPENAI_ENDPOINT='https://<TU-RECURSO>.openai.azure.com/' \
  -e AZURE_OPENAI_DEPLOYMENT='<TU-DEPLOYMENT>' \
  -e ASPNETCORE_URLS='http://+:8080' \
  sentiment-backend:latest

# Frontend
cd frontend
docker build --platform linux/amd64 -t sentiment-frontend:latest .
docker run -p 4200:8080 sentiment-frontend:latest
```

## 📁 Estructura del Proyecto

```
sentiment-analyzer-app/
├── backend/                             # API Backend .NET 8
│   ├── Controllers/
│   │   └── SentimentController.cs       # Endpoint: POST /api/sentiment/analyze
│   ├── Models/
│   │   ├── SentimentRequest.cs          # Modelo de entrada
│   │   └── SentimentResponse.cs         # Modelo de salida
│   ├── Services/
│   │   ├── ISentimentService.cs         # Interfaz del servicio
│   │   ├── AzureOpenAISentimentService.cs    # Azure OpenAI integration
│   │   ├── ITelemetryService.cs         # Interfaz de telemetría
│   │   └── ApplicationInsightsTelemetryService.cs  # App Insights metrics
│   ├── Program.cs                       # Configuración: CORS, DI, App Insights
│   ├── appsettings.json                 # Configuración base
│   ├── Dockerfile                       # Multi-stage build (.NET 8 + Alpine)
│   └── SentimentAnalyzer.csproj         # Proyecto .NET 8
├── frontend/                            # SPA Frontend Angular 12
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   └── sentiment-analyzer/  # Componente principal
│   │   │   ├── services/
│   │   │   │   └── sentiment.service.ts # HTTP service → Backend API
│   │   │   ├── models/
│   │   │   │   └── sentiment-response.model.ts
│   │   │   ├── app.component.*          # Root component
│   │   │   └── app.module.ts            # Módulo principal
│   │   ├── environments/
│   │   │   ├── environment.ts           # Config desarrollo
│   │   │   └── environment.prod.ts      # Config producción (ACA URLs)
│   │   └── index.html
│   ├── nginx.conf                       # Configuración Nginx para ACA
│   ├── package.json                     # Dependencias npm
│   ├── Dockerfile                       # Multi-stage: Node build + Nginx
│   └── angular.json                     # Configuración Angular CLI
├── docker-compose.yml                   # Orquestación local (dev)
├── .env.example                         # Template de variables de entorno
├── README.md                            # Este archivo
├── ARCHITECTURE.md                      # 🆕 Diagramas detallados (10+ Mermaid)
├── UPGRADE-DOTNET8.md                   # Guía de migración .NET 6 → 8
├── ACR-DEPLOYMENT.md                    # Deployment en Azure Container Registry
├── ACA-DEPLOYMENT-SUMMARY.md            # Estado actual de ACA
├── APP-INSIGHTS-SCALING.md              # Configuración de escalado inteligente
├── SCALING-SCENARIOS.md                 # Escenarios de auto-scaling
└── RUN-LOCAL.md                         # Guía rápida de desarrollo local
```

## 🔑 Variables de Entorno

### Backend (.NET 8)

| Variable | Descripción | Ejemplo/Valor |
|----------|-------------|---------------|
| `AZURE_OPENAI_ENDPOINT` | Endpoint del recurso Azure OpenAI | `https://<tu-recurso>.openai.azure.com/` |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | Nombre del deployment del modelo | `gpt-4` o `gpt-4.1` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Connection string de Application Insights | `InstrumentationKey=<key>;IngestionEndpoint=https://...` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Development` (local) / `Production` (Azure) |
| `ASPNETCORE_HTTP_PORTS` | Puerto HTTP dentro del contenedor | `8080` |

**Autenticación (Producción con Managed Identity):**
- No requiere `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` en ACA
- El Container App usa su Managed Identity para acceder a Azure OpenAI

**Autenticación (CI/CD con Service Principal):**
- `AZURE_TENANT_ID`: ID del tenant de Azure
- `AZURE_CLIENT_ID`: Application ID del Service Principal
- `AZURE_CLIENT_SECRET`: Secret del Service Principal

### Frontend (Angular 12)

Configura `apiUrl` en:
- **Local**: `src/environments/environment.ts` → `http://localhost:5079/api`
- **Producción**: `src/environments/environment.prod.ts` → URL del Backend ACA

### Docker Compose (.env file)

```bash
AZURE_OPENAI_ENDPOINT=https://<tu-recurso>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_HTTP_PORTS=8080
```

## 📝 API Endpoints

### POST /api/sentiment/analyze

Analiza el sentimiento de un texto.

**Request**:
```json
{
  "text": "I love this product! It's amazing!"
}
```

**Response** (Exitoso - 200):
```json
{
  "sentiment": "Positive",
  "score": 1.0,
  "message": "Analysis completed successfully"
}
```

**Response** (Error - 400):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Text": ["The Text field is required."]
  }
}
```

## ⚠️ Solución de Problemas

### 1. Error: "CORS policy has blocked the request"

**Causa**: El frontend no puede acceder al backend por política CORS.

**Solución**: CORS ya está configurado en `Program.cs`. Si usas un dominio diferente, actualiza:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.WithOrigins(
                "http://localhost:4200",
                "https://tu-frontend-aca.azurecontainerapps.io"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});
```

### 2. Error: "ERR_OSSL_EVP_UNSUPPORTED" (Node.js 17+)

**Causa**: Angular 12 usa Webpack con algoritmos OpenSSL legacy.

**Solución**:
```powershell
$env:NODE_OPTIONS='--openssl-legacy-provider'
npm start
```

### 3. Error: "Azure OpenAI client failed to initialize"

**Verificaciones**:
1. ✅ Variables de entorno correctas:
   ```powershell
   echo $env:AZURE_OPENAI_ENDPOINT
   echo $env:AZURE_OPENAI_DEPLOYMENT_NAME
   ```
2. ✅ Autenticación configurada:
   ```powershell
   az login
   az account show  # Verificar suscripción activa
   ```
3. ✅ Acceso al recurso:
   ```powershell
   az cognitiveservices account show --name <tu-recurso> --resource-group <tu-rg>
   ```

### 4. Error: "No se conecta al backend desde el frontend"

**Local**: Verifica que el backend esté en `http://localhost:5079` y actualiza `environment.ts`.

**Producción**: Verifica que `environment.prod.ts` tenga la URL correcta del Backend ACA:
```typescript
apiUrl: 'https://sentiment-analyzer-backend-aca.redcliff-8b51d058.centralus.azurecontainerapps.io/api'
```

### 5. Error: Application Insights no recibe telemetría

**Verificaciones**:
1. ✅ Connection string configurada:
   ```powershell
   echo $env:APPLICATIONINSIGHTS_CONNECTION_STRING
   ```
2. ✅ En ACA, verifica la variable de entorno en Azure Portal
3. ✅ Consulta en Log Analytics:
   ```kql
   customMetrics
   | where name == "NegativeSentimentCount"
   | order by timestamp desc
   | take 50
   ```

### 6. El auto-scaling no funciona

**Verificaciones**:
1. ✅ Regla KEDA Log Analytics configurada:
   ```powershell
   az containerapp show --name sentiment-analyzer-backend-aca --resource-group ACA-DEMO-RG --query "properties.template.scale"
   ```
2. ✅ Managed Identity tiene permisos de "Log Analytics Reader":
   ```powershell
   # Verificar asignaciones de rol
   $principalId = az containerapp identity show --name sentiment-analyzer-backend-aca --resource-group ACA-DEMO-RG --query principalId -o tsv
   az role assignment list --assignee $principalId --output table
   ```
3. ✅ Generar eventos negativos (ver sección "Testing" más abajo)
4. ✅ Verificar query KQL en Log Analytics:
   ```kql
   app("appinsights-sentiment-analyzer").customEvents
   | where name == "SentimentAnalyzed"
   | where customDimensions.Sentiment == "Negative"
   | where timestamp > ago(5m)
   | count
   ```
5. ✅ Monitorear réplicas:
   ```powershell
   az containerapp replica list --name sentiment-analyzer-backend-aca --resource-group ACA-DEMO-RG --query "[].{Name:name, Status:properties.runningState, Created:properties.createdTime}" --output table
   ```
6. ✅ Ver logs de KEDA (si disponible en ACA logs)

### 7. Docker: Platform mismatch warnings

**Causa**: Estás en Windows/Mac pero los contenedores de ACA requieren Linux.

**Solución**: Usa `--platform linux/amd64` en todos los comandos Docker:
```powershell
docker build --platform linux/amd64 -t imagen:tag .
```

## 🛠️ Desarrollo y Extensibilidad

### Agregar nuevo endpoint al backend

1. **Crear método en `SentimentController.cs`**:
   ```csharp
   [HttpPost("custom-endpoint")]
   public async Task<IActionResult> CustomEndpoint([FromBody] CustomRequest request)
   {
       var result = await _customService.ProcessAsync(request);
       return Ok(result);
   }
   ```

2. **Implementar servicio**:
   - Definir interfaz en `Services/ICustomService.cs`
   - Implementar lógica en `Services/CustomService.cs`
   - Registrar en `Program.cs`: `builder.Services.AddScoped<ICustomService, CustomService>();`

3. **Actualizar frontend**:
   - Agregar método en `services/sentiment.service.ts`
   - Crear modelo en `models/` si es necesario
   - Actualizar componente para llamar al nuevo servicio

### Personalizar el prompt de análisis

Edita `backend/Services/AzureOpenAISentimentService.cs`:

```csharp
var systemMessage = new SystemChatMessage(
    "Eres un experto en análisis de sentimientos. " +
    "Clasifica el texto como Positive, Negative o Neutral. " +
    "Agrega tu personalización aquí..."
);
```

### Agregar nuevas métricas a Application Insights

En `ApplicationInsightsTelemetryService.cs`:

```csharp
public void TrackCustomMetric(string metricName, double value)
{
    _telemetryClient.GetMetric(metricName).TrackValue(value);
    _telemetryClient.TrackEvent($"{metricName}Tracked", new Dictionary<string, string>
    {
        { "Value", value.ToString() }
    });
    _telemetryClient.Flush();
}
```

## 📦 Deployment a Azure

### 🚀 Azure Container Apps (Recomendado)

**1. Construir y pushear imágenes a ACR**:

```powershell
# Backend
cd backend
az acr build --registry <tu-acr> --image sentiment-analyzer-backend:v1.0 --platform linux .

# Frontend
cd frontend
az acr build --registry <tu-acr> --image sentiment-analyzer-frontend:v1.0 --platform linux .
```

**2. Crear Container Apps**:

```powershell
# Backend
az containerapp create \
  --name sentiment-analyzer-backend-aca \
  --resource-group <tu-rg> \
  --environment <tu-aca-environment> \
  --image <tu-acr>.azurecr.io/sentiment-analyzer-backend:v1.0 \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 10 \
  --registry-server <tu-acr>.azurecr.io \
  --env-vars \
    AZURE_OPENAI_ENDPOINT='https://<tu-recurso>.openai.azure.com/' \
    AZURE_OPENAI_DEPLOYMENT_NAME='gpt-4' \
    APPLICATIONINSIGHTS_CONNECTION_STRING='<tu-connection-string>' \
    ASPNETCORE_ENVIRONMENT='Production'

# Frontend
az containerapp create \
  --name sentiment-analyzer-frontend-aca \
  --resource-group <tu-rg> \
  --environment <tu-aca-environment> \
  --image <tu-acr>.azurecr.io/sentiment-analyzer-frontend:v1.0 \
  --target-port 8080 \
  --ingress external \
  --registry-server <tu-acr>.azurecr.io
```

**3. Configurar Managed Identity para Azure OpenAI**:

```powershell
# Habilitar System-Assigned Managed Identity
az containerapp identity assign \
  --name sentiment-analyzer-backend-aca \
  --resource-group <tu-rg> \
  --system-assigned

# Obtener el Principal ID
$principalId = az containerapp identity show `
  --name sentiment-analyzer-backend-aca `
  --resource-group <tu-rg> `
  --query principalId -o tsv

# Asignar rol en Azure OpenAI
az role assignment create \
  --assignee $principalId \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.CognitiveServices/accounts/<openai-name>
```

**4. Configurar KEDA Auto-Scaling con Log Analytics**:

```powershell
# Asignar rol "Log Analytics Reader" al Managed Identity
az role assignment create \
  --assignee $principalId \
  --role "Log Analytics Reader" \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.OperationalInsights/workspaces/<workspace-name>

# Aplicar configuración YAML de KEDA (ver aca-log-analytics-mi.yaml)
az containerapp update \
  --name sentiment-analyzer-backend-aca \
  --resource-group <tu-rg> \
  --yaml aca-log-analytics-mi.yaml
```

**Configuración de Escalado**: El backend escala automáticamente cuando se detectan **≥ 5 sentimientos negativos en los últimos 5 minutos** mediante queries KQL en Log Analytics. KEDA polling cada 30 segundos.

> 📖 **Para más detalles**, consulta:
> - [`ACR-DEPLOYMENT.md`](ACR-DEPLOYMENT.md) - Deployment en Container Registry
> - [`ACA-DEPLOYMENT-SUMMARY.md`](ACA-DEPLOYMENT-SUMMARY.md) - Estado actual de ACA
> - [`APP-INSIGHTS-SCALING.md`](APP-INSIGHTS-SCALING.md) - Configuración de auto-scaling

### ☁️ Azure App Service (Alternativa)

```powershell
# Backend
cd backend
dotnet publish -c Release -o ./publish
az webapp up --name sentiment-analyzer-api --resource-group <rg> --runtime "DOTNET|8.0"

# Configurar variables
az webapp config appsettings set \
  --name sentiment-analyzer-api \
  --resource-group <rg> \
  --settings \
    AZURE_OPENAI_ENDPOINT='https://<tu-recurso>.openai.azure.com/' \
    AZURE_OPENAI_DEPLOYMENT_NAME='gpt-4' \
    APPLICATIONINSIGHTS_CONNECTION_STRING='<connection-string>'

# Frontend
cd frontend
npm run build --prod
az webapp up --name sentiment-analyzer-web --resource-group <rg> --html
```

## 🧪 Testing

### Probar el backend localmente

```powershell
# Con Invoke-RestMethod (PowerShell)
Invoke-RestMethod -Uri http://localhost:5079/api/sentiment/analyze `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"text":"I love this product!"}'

# Con curl
curl -X POST http://localhost:5079/api/sentiment/analyze `
  -H "Content-Type: application/json" `
  -d '{"text":"This is terrible and disappointing"}'
```

### Probar el backend en Azure Container Apps

```powershell
$backendUrl = "https://sentiment-analyzer-backend-aca.redcliff-8b51d058.centralus.azurecontainerapps.io"

Invoke-RestMethod -Uri "$backendUrl/api/sentiment/analyze" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"text":"Amazing experience!"}'
```

### Test de carga para auto-scaling

Usa el script PowerShell incluido `test-load.ps1` para generar eventos de sentimientos negativos:

```powershell
# Test en producción (Azure Container Apps) con textos negativos
.\test-load.ps1 -RequestCount 20

# El script envía textos con sentimientos variados
# Para forzar scaling, necesitas generar ≥5 sentimientos negativos en 5 minutos
```

El script:
- ✅ Envía múltiples peticiones concurrentes al backend
- ✅ Usa textos de prueba con diferentes sentimientos (positivo, negativo, neutral)
- ✅ Muestra resultados en tiempo real con emojis y colores
- ✅ Calcula estadísticas (distribución de sentimientos, tiempo promedio de respuesta)
- ✅ Detecta errores y muestra logs útiles

**Monitorear auto-scaling durante el test**:
```powershell
# Ver réplicas actuales
az containerapp replica list \
  --name sentiment-analyzer-backend-aca \
  --resource-group ACA-DEMO-RG \
  --query "[].{Name:name, Status:properties.runningState, Created:properties.createdTime}" \
  --output table

# Verificar eventos negativos en Log Analytics
# (Ir a Azure Portal → Log Analytics → Logs)
app("appinsights-sentiment-analyzer").customEvents
| where name == "SentimentAnalyzed"
| where customDimensions.Sentiment == "Negative"
| where timestamp > ago(10m)
| summarize Count = count() by bin(timestamp, 1m)
| render timechart
```

### Probar el frontend

1. Abre el navegador en `http://localhost:4200` (local) o la URL de ACA (producción)
2. Escribe diferentes textos en el input
3. Observa la clasificación de sentimiento y el score
4. Verifica que los eventos se registran en Application Insights

## 📊 Monitoreo y Observabilidad

### Application Insights Queries (Kusto/KQL)

**Ver todas las métricas de sentimientos**:
```kql
customMetrics
| where name in ("Sentiment_Positive", "Sentiment_Negative", "Sentiment_Neutral", "NegativeSentimentCount")
| order by timestamp desc
| take 100
```

**Contar sentimientos negativos por minuto (para KEDA)**:
```kql
app("appinsights-sentiment-analyzer").customEvents
| where name == "SentimentAnalyzed"
| where customDimensions.Sentiment == "Negative"
| where timestamp > ago(5m)
| summarize Count = count() by bin(timestamp, 1m)
| order by timestamp desc
```

**Ver total de eventos negativos en ventana de 5 minutos (query KEDA)**:
```kql
app("appinsights-sentiment-analyzer").customEvents
| where name == "SentimentAnalyzed"
| where customDimensions.Sentiment == "Negative"
| where timestamp > ago(5m)
| count
```

**Ver eventos de análisis de sentimiento**:
```kql
customEvents
| where name == "SentimentAnalyzed"
| extend Sentiment = tostring(customDimensions.Sentiment)
| extend Score = todouble(customDimensions.Score)
| project timestamp, Sentiment, Score
| order by timestamp desc
| take 50
```

**Detectar picos de sentimientos negativos**:
```kql
customEvents
| where name == "NegativeSentimentDetected"
| summarize Count = count() by bin(timestamp, 5m)
| where Count > 3  // Más de 3 negativos en 5 minutos (activation threshold)
| order by timestamp desc
```

**Correlacionar escalado con eventos negativos**:
```kql
let negativeEvents = customEvents
| where name == "SentimentAnalyzed"
| where customDimensions.Sentiment == "Negative"
| where timestamp > ago(1h)
| summarize NegativeCount = count() by bin(timestamp, 1m);
let requests = requests
| where timestamp > ago(1h)
| summarize RequestCount = count() by bin(timestamp, 1m);
negativeEvents
| join kind=inner requests on timestamp
| project timestamp, NegativeCount, RequestCount
| render timechart
```

### Dashboards recomendados

Crea un dashboard en Azure Portal con estos widgets:
1. **Gráfico de líneas**: Sentimientos por tipo (Positive/Negative/Neutral) en las últimas 24 horas
2. **Métrica**: Cuenta total de `NegativeSentimentCount`
3. **Gráfico de área**: Réplicas de ACA vs eventos de sentimientos negativos en tiempo real
4. **Tabla**: Últimos 20 eventos `SentimentAnalyzed` con detalles
5. **KPI**: Umbral de KEDA - muestra si hay ≥5 negativos en 5 minutos (trigger de scaling)

**Query para monitorear threshold de KEDA**:
```kql
app("appinsights-sentiment-analyzer").customEvents
| where name == "SentimentAnalyzed"
| where customDimensions.Sentiment == "Negative"
| where timestamp > ago(5m)
| summarize Count = count()
| extend Status = iff(Count >= 5, "🔴 SCALING UP", iff(Count >= 3, "🟡 ACTIVATION", "🟢 NORMAL"))
| project Count, Status
```

> 📖 **Ver más**: [`APP-INSIGHTS-SCALING.md`](APP-INSIGHTS-SCALING.md) para queries avanzadas y alertas

## 👥 Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/MiNuevaFeature`)
3. Commit tus cambios (`git commit -m 'feat: agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/MiNuevaFeature`)
5. Abre un Pull Request con descripción detallada

### Convenciones de código

- **Backend (.NET)**: Sigue las [convenciones de C#](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- **Frontend (Angular/TypeScript)**: Sigue las [guías de estilo de Angular](https://angular.io/guide/styleguide)
- **JavaScript**: Métodos en **kebab-case** para archivos `.js`
- **Commits**: Usa [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, etc.)

## 📄 Licencia

MIT License - Ver el archivo `LICENSE` para más detalles

## 📚 Documentación Adicional

- [`ARCHITECTURE.md`](ARCHITECTURE.md) - 🏗️ Arquitectura completa con 10+ diagramas Mermaid interactivos
- [`KEDA-LOG-ANALYTICS-SETUP.md`](KEDA-LOG-ANALYTICS-SETUP.md) - 🔧 Guía completa de configuración de KEDA con Log Analytics
- [`APP-INSIGHTS-SCALING.md`](APP-INSIGHTS-SCALING.md) - ⚙️ Configuración de auto-scaling con Application Insights
- [`SCALING-SCENARIOS.md`](SCALING-SCENARIOS.md) - 📈 Escenarios avanzados de escalado
- [`ACR-DEPLOYMENT.md`](ACR-DEPLOYMENT.md) - 🐳 Deployment en Azure Container Registry
- [`ACA-DEPLOYMENT-SUMMARY.md`](ACA-DEPLOYMENT-SUMMARY.md) - ☁️ Estado actual de Azure Container Apps
- [`UPGRADE-DOTNET8.md`](UPGRADE-DOTNET8.md) - 🔄 Guía de migración .NET 6 → .NET 8
- [`RUN-LOCAL.md`](RUN-LOCAL.md) - 💻 Guía rápida de desarrollo local

## 🆘 Soporte

Si encuentras algún problema:

1. **Revisa la documentación**:
   - Sección [⚠️ Solución de Problemas](#️-solución-de-problemas) en este README
   - [`APP-INSIGHTS-SCALING.md`](APP-INSIGHTS-SCALING.md) para problemas de telemetría
   - [`ARCHITECTURE.md`](ARCHITECTURE.md) para entender el flujo completo

2. **Verifica los logs**:
   ```powershell
   # Logs del backend (ACA)
   az containerapp logs show --name sentiment-analyzer-backend-aca --resource-group ACA-DEMO-RG --follow
   
   # Logs del frontend (ACA)
   az containerapp logs show --name <frontend-aca-name> --resource-group ACA-DEMO-RG --follow
   
   # Logs locales (Docker)
   docker logs <container-id> --follow
   ```

3. **Consulta Application Insights**:
   - Azure Portal → Application Insights → Transaction search
   - Failures → Ver stack traces de excepciones
   - Live Metrics → Monitoreo en tiempo real

4. **Abre un issue**:
   - Ve a la sección "Issues" en GitHub
   - Incluye: descripción del error, logs relevantes, pasos para reproducir
   - Agrega etiquetas: `bug`, `help wanted`, `documentation`, etc.

---

## 🎯 Próximos Pasos Recomendados

1. ✅ **Configurar regla KEDA Log Analytics** (ver archivos `aca-log-analytics-*.yaml`)
2. ✅ **Crear alertas en Azure Monitor** para sentimientos negativos > umbral
3. ✅ **Implementar CI/CD con GitHub Actions** (ver arquitectura propuesta en [`ARCHITECTURE.md`](ARCHITECTURE.md))
4. ✅ **Agregar autenticación/autorización** con Microsoft Entra ID (Azure AD)
5. ✅ **Implementar caché** (Redis) para respuestas frecuentes
6. ✅ **Agregar más modelos de análisis** (Azure Text Analytics, custom models)
7. ✅ **Optimizar queries KQL** para mejor performance de KEDA
8. ✅ **Configurar múltiples reglas de scaling** (HTTP + Log Analytics) para escalado híbrido

---

**Desarrollado con ❤️ para Microsoft Reactor** | **Última actualización**: 2024