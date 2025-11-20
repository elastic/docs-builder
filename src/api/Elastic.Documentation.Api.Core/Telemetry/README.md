# OTLP Proxy for Frontend Telemetry

This OTLP (OpenTelemetry Protocol) proxy allows frontend JavaScript code to send telemetry (logs, traces, metrics) to the same OTLP collector used by the backend **without exposing authentication credentials to the browser**.

## Security Model

### ✅ Secure: Backend handles authentication

```
Frontend (Browser) → API Proxy → OTLP Collector (Elastic APM/OTel)
                       ↑
                       Adds auth headers
                       (credentials stay secure)
```

The proxy:
- Reads credentials from environment variables on the backend
- Automatically adds authentication headers to forwarded requests
- Prevents credential exposure to browser DevTools or network inspection

### ❌ Insecure: Direct frontend connection

```
Frontend (Browser) → OTLP Collector
        ↑
        Requires auth credentials
        (exposed in browser code)
```

## Configuration

The proxy uses standard OpenTelemetry environment variables:

```bash
# Required: OTLP collector endpoint
OTEL_EXPORTER_OTLP_ENDPOINT=https://your-apm-server.elastic.co:443

# Optional: Authentication headers (multiple headers separated by comma)
OTEL_EXPORTER_OTLP_HEADERS="Authorization=Bearer secret-token"

# Or for Elastic APM with API Key:
OTEL_EXPORTER_OTLP_HEADERS="Authorization=ApiKey base64-encoded-api-key"

# Or multiple headers:
OTEL_EXPORTER_OTLP_HEADERS="Authorization=Bearer token,X-Custom-Header=value"
```

## API Endpoints

The proxy provides three endpoints matching the OTLP specification:

```
POST /docs/_api/v1/otlp/v1/traces   - Forward trace spans
POST /docs/_api/v1/otlp/v1/logs     - Forward log records
POST /docs/_api/v1/otlp/v1/metrics  - Forward metrics
```

### Content Types Supported

- `application/json` - OTLP JSON encoding (recommended for browser)
- `application/x-protobuf` - OTLP protobuf encoding (smaller but requires encoding)

## Frontend Usage

### Option 1: Using OpenTelemetry JS SDK (Recommended)

```typescript
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions';

// Configure the tracer to use the proxy endpoint
const provider = new WebTracerProvider({
  resource: new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: 'docs-frontend',
    [SemanticResourceAttributes.SERVICE_VERSION]: '1.0.0',
  }),
});

// Point the exporter to the proxy endpoint (no credentials needed!)
const exporter = new OTLPTraceExporter({
  url: 'https://docs.elastic.co/_api/v1/otlp/v1/traces',
  headers: {}, // No auth headers needed - proxy handles it
});

provider.addSpanProcessor(new BatchSpanProcessor(exporter));
provider.register();

// Now you can create spans
const tracer = provider.getTracer('docs-frontend');
const span = tracer.startSpan('page-load');
span.end();
```

### Option 2: Using OpenTelemetry Logs API

```typescript
import { LoggerProvider } from '@opentelemetry/sdk-logs';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http';

const loggerProvider = new LoggerProvider({
  resource: new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: 'docs-frontend',
  }),
});

const exporter = new OTLPLogExporter({
  url: 'https://docs.elastic.co/_api/v1/otlp/v1/logs',
});

loggerProvider.addLogRecordProcessor(new BatchLogRecordProcessor(exporter));

const logger = loggerProvider.getLogger('docs-frontend');
logger.emit({
  severityNumber: 9,
  severityText: 'INFO',
  body: 'User clicked button',
  attributes: {
    'user.action': 'click',
    'button.id': 'submit',
  },
});
```

### Option 3: Manual Fetch (for debugging)

```typescript
// Send logs manually
await fetch('https://docs.elastic.co/_api/v1/otlp/v1/logs', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    resourceLogs: [
      {
        resource: {
          attributes: [
            { key: 'service.name', value: { stringValue: 'docs-frontend' } },
          ],
        },
        scopeLogs: [
          {
            logRecords: [
              {
                timeUnixNano: String(Date.now() * 1000000),
                severityNumber: 9,
                severityText: 'INFO',
                body: {
                  stringValue: 'Test log from browser',
                },
                attributes: [
                  { key: 'page.url', value: { stringValue: window.location.href } },
                ],
              },
            ],
          },
        ],
      },
    ],
  }),
});
```

## CORS Configuration

If your frontend is served from a different domain, you'll need to configure CORS:

```csharp
// In Program.cs or startup configuration
app.UseCors(policy => policy
    .WithOrigins("https://docs.elastic.co")
    .AllowAnyMethod()
    .AllowAnyHeader());
```

## Monitoring the Proxy

The proxy creates its own spans under the `Elastic.Documentation.Api.OtlpProxy` activity source.

Each proxied request includes:
- `otel.signal_type` - The signal type (traces/logs/metrics)
- `otel.content_type` - The content type of the request
- `otel.target_url` - The target OTLP collector URL
- `http.response.status_code` - Response status from collector

## Example: Full Frontend Integration

```typescript
// frontend/telemetry.ts
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { registerInstrumentations } from '@opentelemetry/instrumentation';

export function initTelemetry() {
  const provider = new WebTracerProvider({
    resource: new Resource({
      [SemanticResourceAttributes.SERVICE_NAME]: 'docs-frontend',
      [SemanticResourceAttributes.DEPLOYMENT_ENVIRONMENT]: 
        window.location.hostname.includes('localhost') ? 'dev' : 'prod',
    }),
  });

  // Use the proxy endpoint - no credentials needed!
  const exporter = new OTLPTraceExporter({
    url: `${window.location.origin}/_api/v1/otlp/v1/traces`,
  });

  provider.addSpanProcessor(new BatchSpanProcessor(exporter, {
    maxQueueSize: 100,
    scheduledDelayMillis: 5000,
  }));

  provider.register({
    contextManager: new ZoneContextManager(),
  });

  // Auto-instrument page loads and user interactions
  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new UserInteractionInstrumentation(),
    ],
  });

  console.log('OpenTelemetry initialized with proxy');
}
```

## Troubleshooting

### Proxy returns 503 "OTLP proxy is not configured"

The `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable is not set. Configure it in your Lambda environment variables.

### Proxy returns 401/403 from collector

The authentication headers in `OTEL_EXPORTER_OTLP_HEADERS` are invalid or expired.

### Frontend gets CORS errors

Add CORS configuration to allow requests from your frontend domain.

### Data not appearing in Elastic APM

1. Check the proxy logs for errors
2. Verify the OTLP endpoint is correct
3. Ensure the collector is configured to accept OTLP/HTTP on `/v1/traces`, `/v1/logs`, `/v1/metrics`
4. Check that your OTLP payload format is correct (use the OpenTelemetry SDK to avoid formatting errors)

## Performance Considerations

- The proxy uses streaming to avoid buffering large payloads in memory
- Batch telemetry in the frontend before sending (use `BatchSpanProcessor` and `BatchLogRecordProcessor`)
- Consider sampling high-volume traces in production
- Monitor proxy latency via the `Elastic.Documentation.Api.OtlpProxy` spans

## Security Best Practices

✅ **DO:**
- Use HTTPS for the proxy endpoint in production
- Set appropriate rate limits on the proxy endpoint
- Monitor for unusual traffic patterns
- Use resource attributes to identify the frontend service

❌ **DON'T:**
- Expose OTLP collector credentials in frontend code
- Allow unauthenticated access to the collector directly
- Send PII (personally identifiable information) in telemetry without user consent
- Forget to configure CORS for cross-origin requests

