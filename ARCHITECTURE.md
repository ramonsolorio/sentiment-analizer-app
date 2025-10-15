# 🏗️ Arquitectura del Sistema - Sentiment Analyzer

Este documento describe la arquitectura completa del sistema de análisis de sentimientos con escalado automático basado en telemetría de Application Insights.

---

## 📋 Índice

1. [Diagrama de Arquitectura General](#diagrama-de-arquitectura-general)
2. [Flujo de Análisis de Sentimientos](#flujo-de-análisis-de-sentimientos)
3. [Flujo de Escalado Automático](#flujo-de-escalado-automático)
4. [Componentes de Azure](#componentes-de-azure)
5. [Stack Tecnológico](#stack-tecnológico)
6. [Flujo de Despliegue CI/CD](#flujo-de-despliegue-cicd)

---

## 1️⃣ Diagrama de Arquitectura General

```mermaid
graph TB
    subgraph "Usuario Final"
        USER["👤 Usuario<br/>Navegador Web"]
    end

    subgraph "Azure Container Apps Environment"
        subgraph "Frontend Container App"
            FRONTEND["🌐 Angular 12 Frontend<br/>Port 8080<br/>Nginx Alpine<br/>academosampl1"]
        end
        
        subgraph "Backend Container App - Auto-Scaling"
            BACKEND1["⚙️ .NET 8 API<br/>Instance 1<br/>Port 8080"]
            BACKEND2["⚙️ .NET 8 API<br/>Instance 2<br/>Port 8080"]
            BACKEND3["⚙️ .NET 8 API<br/>Instance N<br/>Port 8080"]
            SCALER["📊 KEDA Log Analytics Scaler<br/>Min: 1, Max: 10<br/>Negative Sentiments > 5"]
        end
    end

    subgraph "Azure Services"
        ACR["🐳 Azure Container Registry<br/>acrdemoaca.azurecr.io<br/>- Frontend: v1.0.3<br/>- Backend: v1.1.0"]
        OPENAI["🤖 Azure OpenAI Service<br/>(tu-recurso).openai.azure.com<br/>Model: GPT-4"]
        APPINSIGHTS["📈 Application Insights<br/>appinsights-sentiment-analyzer<br/>Telemetría + Métricas"]
        LOGANALYTICS["📝 Log Analytics Workspace<br/>Logs centralizados"]
    end

    subgraph "Authentication & Identity"
        AZUREAD["🔐 Azure AD / Entra ID<br/>DefaultAzureCredential<br/>Azure CLI Login"]
    end

    USER -->|HTTPS Request| FRONTEND
    FRONTEND -->|API Call<br/>POST /api/sentiment/analyze| BACKEND1
    FRONTEND -->|API Call| BACKEND2
    FRONTEND -->|API Call| BACKEND3
    
    BACKEND1 -->|Authenticate| AZUREAD
    BACKEND2 -->|Authenticate| AZUREAD
    BACKEND3 -->|Authenticate| AZUREAD
    
    BACKEND1 -->|Analyze Text| OPENAI
    BACKEND2 -->|Analyze Text| OPENAI
    BACKEND3 -->|Analyze Text| OPENAI
    
    BACKEND1 -->|Send Telemetry<br/>NegativeSentimentCount| APPINSIGHTS
    BACKEND2 -->|Send Telemetry| APPINSIGHTS
    BACKEND3 -->|Send Telemetry| APPINSIGHTS
    
    APPINSIGHTS -->|Store Logs| LOGANALYTICS
    
    SCALER -->|Query KQL<br/>Every 30s| LOGANALYTICS
    SCALER -->|Scale Up/Down<br/>Based on Sentiment| BACKEND1
    SCALER -.->|Create New Instance| BACKEND3
    
    ACR -.->|Pull Images| FRONTEND
    ACR -.->|Pull Images| BACKEND1
    
    style USER fill:#e1f5ff
    style FRONTEND fill:#b3d9ff
    style BACKEND1 fill:#66b3ff
    style BACKEND2 fill:#66b3ff
    style BACKEND3 fill:#66b3ff
    style SCALER fill:#ff9933
    style OPENAI fill:#00cc66
    style APPINSIGHTS fill:#ff6699
    style ACR fill:#9966ff
    style AZUREAD fill:#ffcc00
```

---

## 2️⃣ Flujo de Análisis de Sentimientos

```mermaid
sequenceDiagram
    actor Usuario
    participant Frontend as 🌐 Angular Frontend
    participant Backend as ⚙️ .NET 8 Backend API
    participant OpenAI as 🤖 Azure OpenAI<br/>(GPT-4.1)
    participant AppInsights as 📈 Application Insights
    participant AzureAD as 🔐 Azure AD

    Usuario->>Frontend: 1. Ingresa texto para analizar
    Frontend->>Frontend: 2. Valida entrada (max 2000 chars)
    Frontend->>Backend: 3. POST /api/sentiment/analyze<br/>{text: "Este producto es horrible"}
    
    Backend->>Backend: 4. Valida request
    Backend->>AzureAD: 5. Obtiene token con<br/>DefaultAzureCredential
    AzureAD-->>Backend: 6. Access Token
    
    Backend->>OpenAI: 7. Analiza sentimiento<br/>ChatCompletions.CreateAsync()
    Note over OpenAI: Deployment: gpt-4.1<br/>System Prompt: Analiza sentimiento
    OpenAI-->>Backend: 8. Respuesta:<br/>{sentiment: "Negative", score: -0.85}
    
    Backend->>AppInsights: 9. Track Custom Metrics:<br/>- NegativeSentimentCount += 1<br/>- Sentiment_Negative += 1<br/>- Event: SentimentAnalyzed
    AppInsights-->>Backend: 10. Telemetry confirmada
    
    Backend-->>Frontend: 11. JSON Response:<br/>{sentiment, score, message}
    Frontend->>Frontend: 12. Renderiza resultado<br/>con icono y color
    Frontend-->>Usuario: 13. Muestra análisis visual
    
    Note over AppInsights: Métricas acumuladas<br/>trigger auto-scaling si<br/>requests > threshold
```

---

## 3️⃣ Flujo de Escalado Automático

```mermaid
graph TD
    START([Inicio del Sistema]) --> IDLE[Backend: 1 réplica activa<br/>Min Replicas: 1]
    
    IDLE --> ANALYZE[Usuario envía texto<br/>para análisis]
    
    ANALYZE --> DETECT_SENTIMENT{Sentimiento<br/>detectado}
    
    DETECT_SENTIMENT -->|Positive/Neutral| SEND_TELEMETRY[📊 Enviar telemetría<br/>a App Insights]
    DETECT_SENTIMENT -->|Negative| SEND_NEGATIVE
    
    SEND_NEGATIVE[📊 Enviar evento:<br/>SentimentAnalyzed<br/>Sentiment=Negative] --> APPINSIGHTS_STORE
    SEND_TELEMETRY --> APPINSIGHTS_STORE
    
    APPINSIGHTS_STORE[Application Insights<br/>almacena eventos] --> LOG_ANALYTICS
    
    LOG_ANALYTICS[📝 Log Analytics Workspace<br/>indexa eventos] --> KEDA_POLL
    
    KEDA_POLL[🔍 KEDA polling cada 30s<br/>Query KQL en Log Analytics] --> RUN_QUERY
    
    RUN_QUERY[Ejecuta query:<br/>customEvents Negative<br/>últimos 5 minutos] --> CHECK_COUNT
    
    CHECK_COUNT{Count de<br/>negativos}
    
    CHECK_COUNT -->|< 3 eventos| IDLE
    CHECK_COUNT -->|3-4 eventos| ACTIVATION[⚠️ Activation threshold<br/>Preparar para escalar]
    CHECK_COUNT -->|≥ 5 eventos| SCALE_UP
    
    ACTIVATION --> KEDA_POLL
    
    SCALE_UP[⬆️ ACA escala UP<br/>Crea nueva réplica] --> NEW_INSTANCE
    
    NEW_INSTANCE[✅ Nueva instancia iniciada<br/>Total: 2-10 réplicas] --> DISTRIBUTE
    
    DISTRIBUTE[🔄 Load Balancer distribuye<br/>requests entre réplicas] --> PROCESS
    
    PROCESS[⚙️ Procesamiento paralelo<br/>de requests] --> MORE_ANALYSIS
    
    MORE_ANALYSIS[Más análisis de<br/>sentimientos] --> CHECK_SENTIMENT
    
    CHECK_SENTIMENT{Negativos siguen<br/>llegando?}
    
    CHECK_SENTIMENT -->|Sí| MAINTAIN[Mantiene réplicas<br/>actuales]
    CHECK_SENTIMENT -->|No, < 5 en 5 min| SCALE_DOWN
    
    SCALE_DOWN[⬇️ ACA escala DOWN<br/>Elimina réplicas<br/>Cooldown: 5 min] --> IDLE
    
    MAINTAIN --> KEDA_POLL
    
    style START fill:#00cc66
    style SEND_NEGATIVE fill:#ff6699
    style SCALE_UP fill:#ff6699
    style SCALE_DOWN fill:#3399ff
    style LOG_ANALYTICS fill:#ff9933
    style KEDA_POLL fill:#9966ff
```

---

## 4️⃣ Componentes de Azure - Vista Detallada

```mermaid
graph LR
    subgraph "Resource Group: ACA-DEMO-RG"
        subgraph "Compute"
            ACA_ENV["🏢 ACA Managed Environment<br/>managedEnvironment-ACADEMORG-9062<br/>Region: Central US"]
            
            ACA_FRONT["📱 Frontend Container App<br/>academosampl1<br/>Image: v1.0.3<br/>Replicas: 1<br/>CPU: 0.5, RAM: 1Gi"]
            
            ACA_BACK["🔧 Backend Container App<br/>sentiment-analyzer-backend-aca<br/>Image: v1.1.0<br/>Replicas: 1-10 auto-scale<br/>CPU: 0.5, RAM: 1Gi"]
        end
        
        subgraph "Container Registry"
            ACR["🐳 Azure Container Registry<br/>acrdemoaca<br/>SKU: Standard<br/>Images:<br/>- frontend: latest, v1.0.3<br/>- backend: latest, v1.1.0"]
        end
        
        subgraph "Monitoring & Observability"
            APPINS["📊 Application Insights<br/>appinsights-sentiment-analyzer<br/>Retention: 30 days<br/>Custom Metrics:<br/>- NegativeSentimentCount<br/>- Sentiment_Positive/Negative<br/>- HTTP Request Rate"]
            
            LOGWORK["📝 Log Analytics Workspace<br/>Linked to App Insights<br/>Query Language: KQL"]
        end
        
        subgraph "AI Services"
            AOAI["🤖 Azure OpenAI<br/>(tu-recurso).openai.azure.com<br/>Deployment: gpt-4<br/>Model: GPT-4<br/>Auth: Azure AD"]
        end
    end
    
    ACA_FRONT --> ACA_ENV
    ACA_BACK --> ACA_ENV
    
    ACA_FRONT -.->|Pull Image| ACR
    ACA_BACK -.->|Pull Image| ACR
    
    ACA_BACK -->|Send Telemetry| APPINS
    APPINS -->|Store Logs| LOGWORK
    
    ACA_BACK -->|API Calls| AOAI
    
    style ACA_ENV fill:#e6f3ff
    style ACA_FRONT fill:#b3d9ff
    style ACA_BACK fill:#66b3ff
    style ACR fill:#9966ff
    style APPINS fill:#ff6699
    style AOAI fill:#00cc66
```

---

## 5️⃣ Stack Tecnológico Completo

```mermaid
mindmap
  root((Sentiment<br/>Analyzer))
    Frontend
      Angular 12
        TypeScript 4.2
        Bootstrap 5.3
        Bootstrap Icons
        RxJS
      Nginx Alpine
        Non-root user
        Port 8080
        Custom config
      Build Tools
        Node.js 21.6.1
        npm
        webpack
    Backend
      .NET 8.0
        ASP.NET Core Web API
        C# 12
        Minimal APIs
      Dependencies
        Azure.AI.OpenAI 2.0.0
        Azure.Identity 1.12.0
        Microsoft.ApplicationInsights.AspNetCore 2.22.0
        Newtonsoft.Json
      Authentication
        DefaultAzureCredential
        Azure CLI
        Managed Identity ready
    Azure Services
      Container Apps
        HTTP Auto-scaling
        KEDA runtime
        Managed Environment
      Container Registry
        Linux AMD64 images
        Multi-stage builds
      Application Insights
        Custom metrics
        Event tracking
        Live metrics
      Azure OpenAI
        GPT-4.1 model
        Chat Completions API
      Log Analytics
        KQL queries
        Workbooks
    Infrastructure
      Docker
        Multi-stage builds
        Alpine Linux base
        Non-privileged users
      Networking
        HTTPS only
        CORS enabled
        Public endpoints
      Security
        No root containers
        Non-privileged ports
        Azure AD auth
```

---

## 6️⃣ Flujo de Despliegue CI/CD (Propuesto)

```mermaid
graph TD
    DEV[👨‍💻 Developer] -->|git push| GITHUB[📦 GitHub Repository]
    
    GITHUB -->|Trigger| ACTIONS[⚡ GitHub Actions<br/>Workflow]
    
    ACTIONS --> BUILD_FRONT[🔨 Build Frontend<br/>Docker image]
    ACTIONS --> BUILD_BACK[🔨 Build Backend<br/>Docker image]
    
    BUILD_FRONT --> TEST_FRONT[🧪 Run Unit Tests<br/>npm test]
    BUILD_BACK --> TEST_BACK[🧪 Run Unit Tests<br/>dotnet test]
    
    TEST_FRONT -->|Pass| TAG_FRONT[🏷️ Tag Image<br/>v1.0.x]
    TEST_BACK -->|Pass| TAG_BACK[🏷️ Tag Image<br/>v1.1.x]
    
    TAG_FRONT --> PUSH_ACR_F[📤 Push to ACR<br/>frontend:latest]
    TAG_BACK --> PUSH_ACR_B[📤 Push to ACR<br/>backend:latest]
    
    PUSH_ACR_F --> DEPLOY_FRONT[🚀 Deploy to ACA<br/>Frontend]
    PUSH_ACR_B --> DEPLOY_BACK[🚀 Deploy to ACA<br/>Backend]
    
    DEPLOY_FRONT --> HEALTH_FRONT[💚 Health Check<br/>/health]
    DEPLOY_BACK --> HEALTH_BACK[💚 Health Check<br/>/health]
    
    HEALTH_FRONT -->|Healthy| COMPLETE
    HEALTH_BACK -->|Healthy| COMPLETE
    
    COMPLETE[✅ Deployment Complete<br/>Monitor in App Insights]
    
    COMPLETE --> MONITOR[👁️ Monitoring]
    
    MONITOR --> APPINSIGHTS_MON[📊 Application Insights<br/>- Performance<br/>- Errors<br/>- Custom Metrics]
    MONITOR --> ACA_MON[📈 ACA Metrics<br/>- Replicas<br/>- CPU/Memory<br/>- Requests]
    
    style GITHUB fill:#333
    style ACTIONS fill:#2088FF
    style COMPLETE fill:#00cc66
    style APPINSIGHTS_MON fill:#ff6699
```

---

## 7️⃣ Arquitectura de Datos - Telemetría

```mermaid
erDiagram
    APPLICATION_INSIGHTS ||--o{ CUSTOM_METRICS : contains
    APPLICATION_INSIGHTS ||--o{ CUSTOM_EVENTS : contains
    APPLICATION_INSIGHTS ||--o{ TRACES : contains
    APPLICATION_INSIGHTS ||--o{ REQUESTS : contains
    APPLICATION_INSIGHTS ||--|| LOG_ANALYTICS : stores_in
    
    CUSTOM_METRICS {
        string metricName
        double value
        datetime timestamp
        string cloudRoleName
    }
    
    CUSTOM_EVENTS {
        string eventName
        json properties
        json metrics
        datetime timestamp
    }
    
    TRACES {
        string message
        string severityLevel
        datetime timestamp
        string operation
    }
    
    REQUESTS {
        string name
        string url
        int resultCode
        double duration
        bool success
    }
    
    LOG_ANALYTICS {
        string query
        json results
        string timeRange
    }
```

**Métricas Personalizadas Enviadas:**

1. **NegativeSentimentCount**: Contador de sentimientos negativos detectados
2. **Sentiment_Positive**: Contador de sentimientos positivos
3. **Sentiment_Negative**: Contador de sentimientos negativos
4. **Sentiment_Neutral**: Contador de sentimientos neutrales

**Eventos Personalizados:**

1. **SentimentAnalyzed**: 
   - Properties: Sentiment, Message
   - Metrics: Score

2. **NegativeSentimentDetected**:
   - Properties: Text, Severity
   - Metrics: NegativeScore

---

## 8️⃣ Configuración de Auto-Scaling

```mermaid
graph TB
    subgraph "Scaling Configuration"
        CONFIG[⚙️ Scale Rule Configuration]
        
        CONFIG --> KEDA_RULE[📊 KEDA Log Analytics Rule<br/>Type: azure-log-analytics<br/>Metadata:<br/>- workspaceId<br/>- query (KQL)<br/>- threshold: 5<br/>- activationThreshold: 3]
        
        KEDA_RULE --> MIN[Min Replicas: 1<br/>Siempre activa para<br/>respuestas rápidas]
        
        KEDA_RULE --> MAX[Max Replicas: 10<br/>Límite de costo y<br/>recursos]
        
        KEDA_RULE --> POLL[Polling Interval: 30s<br/>Revisa Log Analytics<br/>cada 30 segundos]
        
        KEDA_RULE --> COOLDOWN[Cooldown Period: 300s<br/>Evita flapping<br/>entre scale up/down]
    end
    
    subgraph "KQL Query Details"
        QUERY[Query en Log Analytics:<br/>customEvents<br/>| where name == 'SentimentAnalyzed'<br/>| where customDimensions.Sentiment == 'Negative'<br/>| where timestamp > ago(5m)<br/>| count]
    end
    
    subgraph "Scaling Behavior"
        IDLE_STATE[Estado Idle<br/>1 réplica activa<br/>< 3 negativos en 5 min]
        
        IDLE_STATE -->|3+ negativos detectados| ACTIVATION_EVENT
        
        ACTIVATION_EVENT[⚠️ Activation Event<br/>KEDA se activa<br/>Prepara para escalar]
        
        ACTIVATION_EVENT -->|≥ 5 negativos en 5 min| SCALE_UP_EVENT
        
        SCALE_UP_EVENT[⬆️ Scale Up Event<br/>+1 réplica por cada<br/>5 eventos adicionales]
        
        SCALE_UP_EVENT --> ACTIVE_STATE[Estado Activo<br/>2-10 réplicas<br/>Procesando alta carga]
        
        ACTIVE_STATE -->|< 5 negativos<br/>por 5 minutos| SCALE_DOWN_EVENT
        
        SCALE_DOWN_EVENT[⬇️ Scale Down Event<br/>-1 réplica cada 5 min<br/>Gradualmente]
        
        SCALE_DOWN_EVENT --> IDLE_STATE
    end
    
    subgraph "Monitoring"
        APPINS_MON[📈 Application Insights<br/>Monitorea:<br/>- SentimentAnalyzed events<br/>- NegativeSentimentCount<br/>- Response Time]
        
        LOG_MON[📊 Log Analytics<br/>Ejecuta queries:<br/>- Event count<br/>- Time windows<br/>- Sentiment analysis]
        
        ACA_METRICS[📊 ACA Metrics<br/>Monitorea:<br/>- Replica Count<br/>- CPU Usage<br/>- Memory Usage]
    end
    
    ACTIVE_STATE -.->|Send Events| APPINS_MON
    APPINS_MON -.->|Store in| LOG_MON
    KEDA_RULE -.->|Query every 30s| LOG_MON
    ACTIVE_STATE -.->|Report Status| ACA_METRICS
    
    style MIN fill:#66b3ff
    style MAX fill:#ff6699
    style SCALE_UP_EVENT fill:#ff6699
    style SCALE_DOWN_EVENT fill:#3399ff
    style QUERY fill:#9966ff
    style KEDA_RULE fill:#ff9933
```

---

## 9️⃣ Seguridad y Autenticación

```mermaid
graph TD
    subgraph "Authentication Flow"
        CONTAINER[🐳 Container App<br/>Backend Instance]
        
        CONTAINER --> CRED_CHAIN[🔐 DefaultAzureCredential<br/>Credential Chain]
        
        CRED_CHAIN --> ENV_VAR{Environment<br/>Variables?}
        ENV_VAR -->|No| CLI
        ENV_VAR -->|Yes| SERVICE_PRINCIPAL[Service Principal<br/>Client ID + Secret]
        
        CLI[Azure CLI<br/>Credentials] --> AZURE_AD
        SERVICE_PRINCIPAL --> AZURE_AD
        
        AZURE_AD[🔑 Azure AD / Entra ID<br/>Authentication Service]
        
        AZURE_AD --> TOKEN[Access Token<br/>Scope: Azure OpenAI]
        
        TOKEN --> OPENAI_CALL[🤖 Azure OpenAI API Call<br/>Bearer Token in Header]
    end
    
    subgraph "Container Security"
        NON_ROOT[🚫 Non-root User<br/>UID: 1001<br/>No sudo access]
        
        NON_PRIV_PORT[🔒 Non-privileged Port<br/>Port 8080<br/>No CAP_NET_BIND_SERVICE]
        
        READONLY_FS[📁 Read-only Filesystem<br/>Except /tmp<br/>Immutable deployment]
    end
    
    subgraph "Network Security"
        HTTPS_ONLY[🔐 HTTPS Only<br/>TLS 1.2+<br/>Managed by ACA]
        
        CORS[🌐 CORS Policy<br/>AllowAll for demo<br/>Restrict in production]
        
        PRIVATE_NETWORK[🔒 Private Networking<br/>VNet integration ready<br/>Service-to-service]
    end
    
    style AZURE_AD fill:#ffcc00
    style TOKEN fill:#00cc66
    style NON_ROOT fill:#ff6699
    style HTTPS_ONLY fill:#9966ff
```

---

## 🔟 Configuración YAML de KEDA Log Analytics

### Opción 1: Managed Identity (Recomendado para Producción)

```yaml
properties:
  template:
    scale:
      minReplicas: 1
      maxReplicas: 10
      rules:
        - name: negative-sentiment-keda-scale
          custom:
            type: azure-log-analytics
            metadata:
              tenantId: "YOUR_TENANT_ID"
              workspaceId: "YOUR_WORKSPACE_ID"
              query: |
                app("appinsights-sentiment-analyzer").customEvents
                | where name == "SentimentAnalyzed"
                | where customDimensions.Sentiment == "Negative"
                | where timestamp > ago(5m)
                | count
              threshold: "5"
              activationThreshold: "3"
            auth:
              - identity: system
```

**Ventajas**:
- ✅ Sin gestión de credenciales
- ✅ Rotación automática de tokens
- ✅ Mejor seguridad (no hay secretos en el código)

### Opción 2: Service Principal (CI/CD o Dev)

```yaml
properties:
  template:
    scale:
      minReplicas: 1
      maxReplicas: 10
      rules:
        - name: negative-sentiment-keda-scale
          custom:
            type: azure-log-analytics
            metadata:
              tenantId: "YOUR_TENANT_ID"
              workspaceId: "YOUR_WORKSPACE_ID"
              query: |
                app("appinsights-sentiment-analyzer").customEvents
                | where name == "SentimentAnalyzed"
                | where customDimensions.Sentiment == "Negative"
                | where timestamp > ago(5m)
                | count
              threshold: "5"
              activationThreshold: "3"
            auth:
              - secretRef: keda-tenant-id
                triggerParameter: tenantId
              - secretRef: keda-client-id
                triggerParameter: clientId
              - secretRef: keda-client-secret
                triggerParameter: clientSecret
```

**Uso**: Para ambientes de CI/CD donde se necesitan credenciales específicas.

### Explicación de Parámetros

| Parámetro | Descripción | Valor Ejemplo |
|-----------|-------------|---------------|
| `tenantId` | ID del tenant de Azure | `2d86e60c-d872-...` |
| `workspaceId` | ID del Log Analytics Workspace | `67a4d5f2-2a34-...` |
| `query` | Query KQL para contar eventos | Ver ejemplo arriba |
| `threshold` | Umbral para scale up | `"5"` (5 eventos negativos) |
| `activationThreshold` | Umbral para activar KEDA | `"3"` (prepara escalado) |
| `pollingInterval` | Frecuencia de polling (opcional) | `30` (segundos) |
| `cooldownPeriod` | Tiempo antes de scale down (opcional) | `300` (segundos) |

### Comandos de Despliegue

**Con Managed Identity**:
```bash
# 1. Habilitar Managed Identity en ACA
az containerapp identity assign \
  --name sentiment-analyzer-backend-aca \
  --resource-group ACA-DEMO-RG \
  --system-assigned

# 2. Obtener Principal ID
principalId=$(az containerapp identity show \
  --name sentiment-analyzer-backend-aca \
  --resource-group ACA-DEMO-RG \
  --query principalId -o tsv)

# 3. Asignar rol "Log Analytics Reader" al Managed Identity
az role assignment create \
  --assignee $principalId \
  --role "Log Analytics Reader" \
  --scope /subscriptions/YOUR_SUB/resourceGroups/ACA-DEMO-RG/providers/Microsoft.OperationalInsights/workspaces/YOUR_WORKSPACE

# 4. Aplicar configuración de scaling
az containerapp update \
  --name sentiment-analyzer-backend-aca \
  --resource-group ACA-DEMO-RG \
  --yaml aca-log-analytics-mi.yaml
```

**Con Service Principal**:
```bash
# 1. Crear secretos en ACA
az containerapp secret set \
  --name sentiment-analyzer-backend-aca \
  --resource-group ACA-DEMO-RG \
  --secrets \
    keda-tenant-id=YOUR_TENANT_ID \
    keda-client-id=YOUR_CLIENT_ID \
    keda-client-secret=YOUR_CLIENT_SECRET

# 2. Aplicar configuración de scaling
az containerapp update \
  --name sentiment-analyzer-backend-aca \
  --resource-group ACA-DEMO-RG \
  --yaml aca-log-analytics-sp.yaml
```

---

## 🔟 Request/Response Flow - Detallado

```mermaid
sequenceDiagram
    autonumber
    
    actor User as 👤 Usuario
    participant Browser as 🌐 Navegador
    participant Frontend as 📱 Frontend Container<br/>(Nginx)
    participant LB as ⚖️ ACA Load Balancer
    participant Backend as 🔧 Backend API<br/>(.NET 8)
    participant Auth as 🔐 Azure AD
    participant OpenAI as 🤖 Azure OpenAI
    participant AppIns as 📊 App Insights
    
    User->>Browser: Ingresa texto y click "Analyze"
    Browser->>Browser: Validación cliente<br/>(max 2000 chars)
    Browser->>Frontend: HTTP GET /<br/>Cargar aplicación
    Frontend-->>Browser: index.html + JS/CSS
    
    Browser->>Frontend: POST /api/sentiment/analyze<br/>Content-Type: application/json<br/>{text: "..."}
    
    Frontend->>LB: Forward request<br/>https://backend-aca.../api/...
    
    Note over LB: Round-robin entre<br/>réplicas disponibles
    
    LB->>Backend: Route to Instance 1
    
    Backend->>Backend: Validate request body<br/>Check null/empty
    
    Backend->>Auth: Request Access Token<br/>DefaultAzureCredential
    Auth-->>Backend: Bearer Token<br/>(expires in 1h)
    
    Backend->>OpenAI: POST /chat/completions<br/>Authorization: Bearer ...<br/>Body: {messages, model}
    
    Note over OpenAI: GPT-4.1 procesa<br/>System: "Analyze sentiment"<br/>User: texto del usuario
    
    OpenAI-->>Backend: 200 OK<br/>{choices[0].message}
    
    Backend->>Backend: Parse OpenAI response<br/>Extract sentiment + score
    
    Backend->>AppIns: TrackEvent("SentimentAnalyzed")<br/>TrackMetric("NegativeSentimentCount")
    AppIns-->>Backend: 202 Accepted
    
    Backend-->>LB: 200 OK<br/>{sentiment, score, message}
    LB-->>Frontend: Return response
    Frontend-->>Browser: JSON response
    
    Browser->>Browser: Render result<br/>- Icon (😊/😐/😢)<br/>- Color coding<br/>- Score display
    
    Browser-->>User: Muestra análisis visual
    
    Note over AppIns: Acumula eventos<br/>Log Analytics ejecuta queries<br/>KEDA detecta ≥ 5 negativos<br/>Trigger auto-scaling
```

---

## 📊 Métricas y KPIs Monitoreados

| Métrica | Tipo | Threshold | Acción |
|---------|------|-----------|--------|
| **Negative Sentiment Events** | Custom | ≥ 5 en 5 min | Scale Up (KEDA) |
| **Activation Threshold** | Custom | ≥ 3 en 5 min | KEDA Preparación |
| **NegativeSentimentCount** | Custom | Acumulativo | Alertas + Dashboards |
| **Request Success Rate** | Availability | < 95% | Investigación |
| **Response Time (P95)** | Performance | > 2s | Optimización |
| **Replica Count** | Scaling | 1-10 | Costo/Performance balance |
| **CPU Usage** | Resource | > 80% | Considerar scale up |
| **Memory Usage** | Resource | > 85% | Considerar aumentar RAM |
| **OpenAI API Latency** | Dependency | > 3s | Revisar Azure OpenAI |
| **Error Rate 5xx** | Reliability | > 1% | Incident response |
| **KEDA Polling Frequency** | Scaling | 30s | Query Log Analytics |

---

## 🎯 Conclusiones Arquitectónicas

### ✅ Fortalezas del Diseño

1. **Escalabilidad Inteligente**: KEDA escala de 1 a 10 réplicas basado en eventos de sentimientos negativos en Log Analytics
2. **Observabilidad Completa**: Application Insights captura métricas personalizadas y eventos de telemetría
3. **Event-Driven Architecture**: Escalado basado en eventos de negocio (sentimientos negativos) no solo en carga técnica
4. **Costo-Efectivo**: Escala a 1 réplica en idle, evitando costos innecesarios
5. **Seguridad**: Contenedores no-root, puertos no-privilegiados, Azure AD auth, Managed Identity para KEDA
6. **Desacoplamiento**: Frontend y Backend independientes, permite actualizaciones separadas
7. **Cloud-Native**: Aprovecha servicios managed de Azure (OpenAI, App Insights, Log Analytics, KEDA)
8. **Query Flexibility**: Usa KQL (Kusto Query Language) para criterios de escalado complejos

### 🔄 Mejoras Futuras Recomendadas

1. **Multiple Scaling Rules**: Combinar KEDA Log Analytics con reglas HTTP para escalado híbrido
2. **Service Bus Queue**: Agregar cola para procesamiento asíncrono masivo de sentimientos
3. **Cosmos DB**: Almacenar histórico de análisis para analytics y ML
4. **API Management**: Gateway API con rate limiting y caching
5. **Azure Front Door**: CDN global para el frontend
6. **Key Vault**: Gestión centralizada de secretos (actualmente usando Managed Identity)
7. **VNet Integration**: Red privada para comunicación interna
8. **Dashboard**: Power BI o Grafana para visualización avanzada de métricas
9. **ML Model**: Entrenar modelo custom para detectar patrones de sentimientos
10. **Predictive Scaling**: Usar histórico para anticipar picos de carga

---

## 📚 Referencias

- **Azure Container Apps**: https://docs.microsoft.com/azure/container-apps/
- **Application Insights**: https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview
- **Azure OpenAI**: https://docs.microsoft.com/azure/ai-services/openai/
- **KEDA Scalers**: https://keda.sh/docs/scalers/
- **Mermaid Diagrams**: https://mermaid.js.org/

---

**Última actualización**: 14 de Octubre, 2025  
**Versión del documento**: 2.0 - Actualizado con KEDA Log Analytics Scaling  
**Autor**: Sistema de Documentación Automatizada
