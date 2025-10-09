# Sentiment Analyzer App

## 📖 Overview
Aplicación web de análisis de sentimientos que utiliza Azure OpenAI (GPT-4.1) para analizar el tono emocional de un texto. Construida con Angular para el frontend y .NET 6 para el backend, con autenticación a Azure mediante DefaultAzureCredential.

## 🏗️ Arquitectura

- **Frontend**: Angular 12 + TypeScript
- **Backend**: ASP.NET Core 6 Web API + C#
- **IA**: Azure OpenAI Service (GPT-4.1)
- **Autenticación**: Azure DefaultAzureCredential (Azure CLI, Service Principal, etc.)
- **Containerización**: Docker + Docker Compose

## 📋 Prerrequisitos

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Node.js 16+](https://nodejs.org/)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- Acceso a un recurso de Azure OpenAI Service

## 🔐 Configuración de Autenticación (¡MUY IMPORTANTE!)

### ⚠️ Problema Común: Tenant Mismatch

Si ves este error:
```
Token tenant 6833a30b-e3e1-48c4-9292-b3702e22aeba does not match resource tenant
```

**Causa**: Tu sesión de Azure CLI está usando un tenant diferente al del recurso de Azure OpenAI.

### ✅ Solución 1: Usar Azure CLI con el tenant correcto

1. **Cerrar sesión de Azure CLI**:
   ```powershell
   az logout
   ```

2. **Iniciar sesión especificando el tenant del recurso de Azure OpenAI**:
   ```powershell
   az login --tenant <TENANT_ID_DEL_RECURSO_OPENAI>
   ```

3. **Verificar la suscripción correcta**:
   ```powershell
   az account show
   az account set --subscription <SUBSCRIPTION_ID>
   ```

4. **Verificar acceso al recurso**:
   ```powershell
   az cognitiveservices account show --name <OPENAI_RESOURCE_NAME> --resource-group <RESOURCE_GROUP>
   ```

### ✅ Solución 2: Service Principal (Recomendado para CI/CD)

1. **Crear Service Principal**:
   ```powershell
   az ad sp create-for-rbac --name "sentiment-analyzer-sp" --role "Cognitive Services OpenAI User" --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP>/providers/Microsoft.CognitiveServices/accounts/<OPENAI_RESOURCE_NAME>
   ```

2. **Guardar el output JSON** (aparecerá algo como):
   ```json
   {
     "appId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
     "displayName": "sentiment-analyzer-sp",
     "password": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
     "tenant": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
   }
   ```

3. **Configurar variables de entorno**:
   ```powershell
   $env:AZURE_TENANT_ID='<tenant del output>'
   $env:AZURE_CLIENT_ID='<appId del output>'
   $env:AZURE_CLIENT_SECRET='<password del output>'
   $env:AZURE_OPENAI_ENDPOINT='https://eus2-devia-openia-2w36.openai.azure.com/'
   $env:AZURE_OPENAI_DEPLOYMENT='gpt-4.1'
   ```

## 🚀 Ejecutar Localmente

### Backend

```powershell
# Navegar al directorio del backend
cd backend

# Restaurar dependencias
dotnet restore

# Compilar
dotnet build

# Configurar variables de entorno (si usas Azure CLI)
$env:AZURE_OPENAI_ENDPOINT='https://eus2-devia-openia-2w36.openai.azure.com/'
$env:AZURE_OPENAI_DEPLOYMENT='gpt-4.1'

# O si usas Service Principal, configura todas las variables:
# $env:AZURE_TENANT_ID='...'
# $env:AZURE_CLIENT_ID='...'
# $env:AZURE_CLIENT_SECRET='...'

# Ejecutar
dotnet run
```

✅ **Backend disponible en**:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### Frontend

```powershell
# Navegar al directorio del frontend
cd frontend

# Instalar dependencias
npm install

# Si usas Node.js 17+ necesitas esta variable de entorno
$env:NODE_OPTIONS='--openssl-legacy-provider'

# Ejecutar
npm start
```

✅ **Frontend disponible en**: `http://localhost:4200`

## 🐳 Ejecutar con Docker

```powershell
# En la raíz del proyecto
docker-compose up --build
```

- Frontend: `http://localhost:4200`
- Backend: `http://localhost:5000`

## 📁 Estructura del Proyecto

```
sentiment-analyzer-app/
├── backend/
│   ├── Controllers/
│   │   └── SentimentController.cs       # API endpoint para análisis
│   ├── Models/
│   │   ├── SentimentRequest.cs          # Modelo de entrada
│   │   └── SentimentResponse.cs         # Modelo de salida
│   ├── Services/
│   │   ├── ISentimentService.cs         # Interfaz del servicio
│   │   └── AzureOpenAISentimentService.cs  # Implementación con Azure OpenAI
│   ├── Program.cs                       # Configuración de la app
│   ├── appsettings.json                 # Configuración
│   ├── Dockerfile                       # Imagen Docker del backend
│   └── .gitignore                       # Archivos a ignorar
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   └── sentiment-analyzer/  # Componente principal
│   │   │   ├── services/
│   │   │   │   └── sentiment.service.ts # Servicio HTTP
│   │   │   └── models/
│   │   │       └── sentiment-response.model.ts  # Modelo TypeScript
│   │   ├── environments/                # Variables de entorno
│   │   └── index.html                   # HTML principal
│   ├── package.json                     # Dependencias npm
│   ├── Dockerfile                       # Imagen Docker del frontend
│   └── .gitignore                       # Archivos a ignorar
├── docker-compose.yml                   # Orquestación de contenedores
├── .gitignore                           # Git ignore raíz
└── README.md                            # Este archivo
```

## 🔑 Variables de Entorno

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `AZURE_OPENAI_ENDPOINT` | URL del recurso de Azure OpenAI | `https://your-resource.openai.azure.com/` |
| `AZURE_OPENAI_DEPLOYMENT` | Nombre del deployment del modelo | `gpt-4.1` |
| `AZURE_TENANT_ID` | (Opcional) ID del tenant | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_CLIENT_ID` | (Opcional) ID del Service Principal | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_CLIENT_SECRET` | (Opcional) Secret del Service Principal | `your-secret` |

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

**Solución**: Ya está configurado CORS en `Program.cs` para permitir `http://localhost:4200`. Si necesitas otros orígenes:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.WithOrigins("http://localhost:4200", "http://otro-origen:puerto")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
```

### 2. Error: "ERR_OSSL_EVP_UNSUPPORTED" en Node.js 17+

**Causa**: Webpack en Angular 12 usa algoritmos de OpenSSL legacy.

**Solución**:
```powershell
$env:NODE_OPTIONS='--openssl-legacy-provider'
npm start
```

### 3. Error: "Azure OpenAI client failed to initialize"

**Verificar**:
1. Las variables de entorno están configuradas
2. Tienes acceso al recurso de Azure OpenAI
3. El deployment name es correcto

```powershell
# Verificar variables
echo $env:AZURE_OPENAI_ENDPOINT
echo $env:AZURE_OPENAI_DEPLOYMENT

# Verificar acceso con Azure CLI
az cognitiveservices account show --name <RESOURCE_NAME> --resource-group <RG_NAME>
```

### 4. Compilación TypeScript falla

**Solución**: Asegúrate de tener los archivos de configuración correctos:
- `tsconfig.json`
- `tsconfig.app.json`
- `tsconfig.spec.json`

### 5. Backend no puede leer las variables de entorno

**Solución**: En PowerShell, las variables de entorno solo duran la sesión. Para persistirlas:

```powershell
# Crear archivo .env en la raíz
@"
AZURE_OPENAI_ENDPOINT=https://eus2-devia-openia-2w36.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT=gpt-4.1
"@ | Out-File -FilePath .env -Encoding UTF8

# Luego en docker-compose.yml ya está configurado para leerlo
```

## 🛠️ Desarrollo

### Agregar nuevo endpoint

1. Crear método en `SentimentController.cs`
2. Implementar lógica en `ISentimentService` y `AzureOpenAISentimentService`
3. Actualizar servicio Angular en `sentiment.service.ts`
4. Actualizar modelo en `sentiment-response.model.ts`

### Modificar el prompt del modelo

Edita `AzureOpenAISentimentService.cs`:

```csharp
new SystemChatMessage("Tu prompt personalizado aquí")
```

## 📦 Deployment a Azure

### Azure App Service

```powershell
# Backend
az webapp up --name sentiment-analyzer-api --resource-group <rg-name> --runtime "DOTNET|6.0"

# Frontend (después de build)
cd frontend
npm run build --prod
az webapp up --name sentiment-analyzer-web --resource-group <rg-name> --html
```

### Configurar variables en Azure:

```powershell
az webapp config appsettings set `
  --name sentiment-analyzer-api `
  --resource-group <rg-name> `
  --settings `
    AZURE_OPENAI_ENDPOINT='https://your-resource.openai.azure.com/' `
    AZURE_OPENAI_DEPLOYMENT='gpt-4.1'
```

## 🧪 Testing

### Probar el backend directamente

```powershell
# Con curl
curl -X POST http://localhost:5000/api/sentiment/analyze `
  -H "Content-Type: application/json" `
  -d '{"text":"I love this!"}'

# Con PowerShell
Invoke-RestMethod -Uri http://localhost:5000/api/sentiment/analyze `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"text":"I love this!"}'
```

## 👥 Contribuir

1. Fork el proyecto
2. Crea una rama (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

MIT License

## 🆘 Soporte

Si encuentras algún problema:
1. Revisa la sección "Solución de Problemas"
2. Verifica los logs del backend y frontend
3. Abre un issue en GitHub con los detalles del error