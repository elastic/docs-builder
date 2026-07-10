import type { SyntheticsConfig } from '@elastic/synthetics'

export default () => {
    const DOCS_ENVIRONMENT = process.env.DOCS_ENV ?? 'local'
    const config: SyntheticsConfig = {
        params: {
            baseUrl: 'http://localhost:4000',
            environment: DOCS_ENVIRONMENT,
        },
        playwrightOptions: {
            ignoreHTTPSErrors: false,
            // Lets the backend exclude our own monitor traffic from tracing/metrics.
            // Keep in sync with TelemetryConstants.SyntheticMonitorHeaderName (.NET).
            // Applies to both the browser context (page) and the API request context (request),
            // so it covers every journey in ./journeys automatically.
            extraHTTPHeaders: {
                'X-Docs-Synthetic-Monitor': 'true',
            },
        },
        /**
         * Configure global monitor settings
         */
        monitor: {
            schedule: 15,
            locations: ['germany', 'us_east'],
            privateLocations: [],
        },
        /**
         * Project monitors settings
         */
        project: {
            id: `elastic-co-docs-${DOCS_ENVIRONMENT}`,
            url: 'https://elastic-docs-v3-prod-e6053a.kb.us-east-1.aws.elastic.cloud/',
            space: 'default',
        },
    }

    switch (DOCS_ENVIRONMENT) {
        case 'prod':
            config.params.baseUrl = 'https://www.elastic.co'
            break
        case 'edge':
            config.params.baseUrl = 'https://d34ipnu52o64md.cloudfront.net'
            break
        case 'staging':
            config.params.baseUrl = 'https://dwnz7p9ulv07a.cloudfront.net'
            break
    }

    console.log(`Using docs environment: ${config.params.environment}`)

    return config
}
