# Test de Carga para Auto-Scaling
# Este script envía múltiples peticiones concurrentes al backend para probar el auto-scaling

param(
    [string]$BackendUrl = "https://sentiment-analyzer-backend-aca.redcliff-8b51d058.centralus.azurecontainerapps.io",
    [int]$RequestCount = 20,
    [switch]$Local
)

# Si se especifica -Local, usar URL local
if ($Local) {
    $BackendUrl = "http://localhost:5079"
    Write-Host "🏠 Modo LOCAL activado" -ForegroundColor Cyan
}

$endpoint = "$BackendUrl/api/sentiment/analyze"

# Textos de prueba con sentimiento negativo
$negativeTexts = @(
    "This is terrible and I hate it",
    "Worst experience ever, completely disappointed",
    "Horrible service, would not recommend",
    "This is awful and frustrating",
    "Very bad, totally unsatisfied",
    "Absolutely disgusting, never again",
    "Poor quality and terrible support",
    "I regret buying this, waste of money",
    "Disappointing results, not what I expected",
    "Terrible product, breaks easily"
)

# Textos de prueba con sentimiento positivo
$positiveTexts = @(
    "I love this product! It's amazing!",
    "Excellent service, highly recommend",
    "Best purchase I've made this year",
    "Outstanding quality and great value",
    "Fantastic experience, will buy again"
)

# Textos de prueba con sentimiento neutral
$neutralTexts = @(
    "The product arrived on time",
    "It works as described",
    "Average experience, nothing special",
    "Standard quality for the price",
    "Met my basic expectations"
)

# Combinar todos los textos
$allTexts = $negativeTexts + $positiveTexts + $neutralTexts

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "🚀 Test de Carga - Sentiment Analyzer" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host ""
Write-Host "📍 Endpoint: " -ForegroundColor Yellow -NoNewline
Write-Host $endpoint -ForegroundColor White
Write-Host "📊 Peticiones: " -ForegroundColor Yellow -NoNewline
Write-Host $RequestCount -ForegroundColor White
Write-Host "⏱️  Modo: " -ForegroundColor Yellow -NoNewline
Write-Host "Concurrente (múltiples jobs en paralelo)" -ForegroundColor White
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host ""

# Verificar conectividad
try {
    Write-Host "🔍 Verificando conectividad..." -ForegroundColor Yellow
    $testResponse = Invoke-RestMethod -Uri $endpoint -Method Post -ContentType "application/json" -Body '{"text":"test connection"}' -TimeoutSec 10
    Write-Host "✅ Conectividad OK" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ Error de conectividad: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Verifica que el backend esté ejecutándose en: $BackendUrl" -ForegroundColor Yellow
    exit 1
}

# Crear jobs para peticiones concurrentes
$jobs = 1..$RequestCount | ForEach-Object {
    Start-Job -ScriptBlock {
        param($url, $texts, $iteration)
        
        $text = $texts[$iteration % $texts.Length]
        $body = @{ text = $text } | ConvertTo-Json
        
        $startTime = Get-Date
        
        try {
            $response = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $body -TimeoutSec 30
            $endTime = Get-Date
            $duration = ($endTime - $startTime).TotalMilliseconds
            
            $emoji = switch ($response.sentiment) {
                "Positive" { "😊" }
                "Negative" { "😞" }
                "Neutral" { "😐" }
                default { "❓" }
            }
            
            return @{
                Success = $true
                Iteration = $iteration
                Sentiment = $response.sentiment
                Score = $response.score
                Duration = [math]::Round($duration, 0)
                Text = $text
                Emoji = $emoji
            }
        }
        catch {
            $endTime = Get-Date
            $duration = ($endTime - $startTime).TotalMilliseconds
            
            return @{
                Success = $false
                Iteration = $iteration
                Error = $_.Exception.Message
                Duration = [math]::Round($duration, 0)
                Text = $text
            }
        }
    } -ArgumentList $endpoint, $allTexts, $_
}

Write-Host "⏳ Esperando respuestas..." -ForegroundColor Yellow
Write-Host ""

# Mostrar progreso
$completed = 0
$total = $jobs.Count
while ($completed -lt $total) {
    $completed = ($jobs | Where-Object { $_.State -ne 'Running' }).Count
    $percent = [math]::Round(($completed / $total) * 100)
    Write-Progress -Activity "Procesando peticiones" -Status "$completed de $total completadas" -PercentComplete $percent
    Start-Sleep -Milliseconds 100
}

Write-Progress -Activity "Procesando peticiones" -Completed

# Recopilar resultados
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

# Analizar resultados
$successful = $results | Where-Object { $_.Success -eq $true }
$failed = $results | Where-Object { $_.Success -eq $false }

$positiveCount = ($successful | Where-Object { $_.Sentiment -eq "Positive" }).Count
$negativeCount = ($successful | Where-Object { $_.Sentiment -eq "Negative" }).Count
$neutralCount = ($successful | Where-Object { $_.Sentiment -eq "Neutral" }).Count

$avgDuration = ($successful | Measure-Object -Property Duration -Average).Average

# Mostrar resultados
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "📊 RESULTADOS DEL TEST" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host ""

Write-Host "✅ Exitosas: " -ForegroundColor Green -NoNewline
Write-Host "$($successful.Count) / $($results.Count)" -ForegroundColor White

if ($failed.Count -gt 0) {
    Write-Host "❌ Fallidas: " -ForegroundColor Red -NoNewline
    Write-Host $failed.Count -ForegroundColor White
}

Write-Host ""
Write-Host "Distribución de Sentimientos:" -ForegroundColor Yellow
Write-Host "  😊 Positive: " -ForegroundColor Green -NoNewline
Write-Host "$positiveCount ($([math]::Round($positiveCount / $successful.Count * 100, 1))%)" -ForegroundColor White
Write-Host "  😞 Negative: " -ForegroundColor Red -NoNewline
Write-Host "$negativeCount ($([math]::Round($negativeCount / $successful.Count * 100, 1))%)" -ForegroundColor White
Write-Host "  😐 Neutral:  " -ForegroundColor Gray -NoNewline
Write-Host "$neutralCount ($([math]::Round($neutralCount / $successful.Count * 100, 1))%)" -ForegroundColor White

Write-Host ""
Write-Host "⏱️  Tiempo promedio de respuesta: " -ForegroundColor Yellow -NoNewline
Write-Host "$([math]::Round($avgDuration, 0)) ms" -ForegroundColor White

# Mostrar primeros 10 resultados
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "📋 PRIMEROS 10 RESULTADOS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host ""

$successful | Select-Object -First 10 | ForEach-Object {
    $color = switch ($_.Sentiment) {
        "Positive" { "Green" }
        "Negative" { "Red" }
        "Neutral" { "Gray" }
        default { "White" }
    }
    
    Write-Host "[$($_.Iteration)] " -ForegroundColor DarkGray -NoNewline
    Write-Host "$($_.Emoji) $($_.Sentiment) " -ForegroundColor $color -NoNewline
    Write-Host "| Score: " -ForegroundColor Gray -NoNewline
    Write-Host "$($_.Score) " -ForegroundColor White -NoNewline
    Write-Host "| " -ForegroundColor Gray -NoNewline
    Write-Host "$($_.Duration)ms" -ForegroundColor Cyan -NoNewline
    Write-Host " | " -ForegroundColor Gray -NoNewline
    $truncatedText = if ($_.Text.Length -gt 50) { $_.Text.Substring(0, 47) + "..." } else { $_.Text }
    Write-Host $truncatedText -ForegroundColor DarkGray
}

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "❌ ERRORES" -ForegroundColor Red
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host ""
    
    $failed | ForEach-Object {
        Write-Host "[$($_.Iteration)] " -ForegroundColor DarkGray -NoNewline
        Write-Host "Error: $($_.Error)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "🔍 PRÓXIMOS PASOS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host ""

if (!$Local) {
    Write-Host "1️⃣  Verifica el auto-scaling en Azure Portal:" -ForegroundColor Yellow
    Write-Host "   - Ve a Azure Container Apps → sentiment-analyzer-backend-aca" -ForegroundColor Gray
    Write-Host "   - Revisa el número de réplicas activas" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2️⃣  Consulta métricas en Application Insights:" -ForegroundColor Yellow
    Write-Host "   - Portal → Application Insights → Live Metrics" -ForegroundColor Gray
    Write-Host "   - Busca customMetrics: NegativeSentimentCount" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3️⃣  Ejecuta consultas KQL:" -ForegroundColor Yellow
    Write-Host "   customMetrics" -ForegroundColor Cyan
    Write-Host "   | where name == 'NegativeSentimentCount'" -ForegroundColor Cyan
    Write-Host "   | order by timestamp desc" -ForegroundColor Cyan
    Write-Host "   | take 50" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "4️⃣  Revisa logs de ACA:" -ForegroundColor Yellow
    Write-Host "   az containerapp logs show --name sentiment-analyzer-backend-aca --resource-group ACA-DEMO-RG --follow" -ForegroundColor Cyan
}
else {
    Write-Host "📍 Estás en modo LOCAL" -ForegroundColor Yellow
    Write-Host "   - Verifica los logs en la consola del backend" -ForegroundColor Gray
    Write-Host "   - El auto-scaling no está activo en modo local" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ Test completado!" -ForegroundColor Green
Write-Host ""
