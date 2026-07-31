import { journey, step, monitor, expect } from '@elastic/synthetics'

const MCP_PATH = '/docs/_mcp'
const MCP_PROTOCOL_VERSION = '2025-06-18'
const MCP_HEADERS = {
    Accept: 'application/json, text/event-stream',
    'Content-Type': 'application/json',
    'MCP-Protocol-Version': MCP_PROTOCOL_VERSION,
}
// Codex's RMCP client does not send a User-Agent. An empty value overrides the
// synthetic client's default so this request exercises the same WAF behavior.
const MCP_NO_USER_AGENT_HEADERS = { ...MCP_HEADERS, 'User-Agent': '' }

// ponytail: duplicated from navigation-test.journey.ts — a 7-line pure helper across 2 files
// isn't worth a shared module yet; extract to synthetics/lib.ts if a 3rd journey needs it.
function getSchedule(env: string) {
    const scheduleMapping = {
        local: 15,
        edge: 15,
        staging: 15,
        prod: 1,
    }
    return scheduleMapping[env] || 15
}

// MCP server is not hosted on the local docs server (:4000), so only register the journey
// for the deployed environments. env is read at module load, same as synthetics.config.ts.
const env = process.env.DOCS_ENV ?? 'local'
if (env !== 'local') {
    journey('mcp server', ({ request, params }) => {
        monitor.use({
            id: `elastic-co-docs-mcp-${params.environment}-v2`,
            schedule: getSchedule(params.environment),
            tags: [`env:${params.environment}`],
        })

        const mcpUrl = `${params.baseUrl}${MCP_PATH}`

        step('MCP liveness endpoint returns 200', async () => {
            const res = await request.get(`${mcpUrl}/alive`)
            expect(res.status()).toBe(200)
        })

        step('MCP initializes without a User-Agent', async () => {
            const res = await request.post(mcpUrl, {
                headers: MCP_NO_USER_AGENT_HEADERS,
                data: {
                    jsonrpc: '2.0',
                    id: 1,
                    method: 'initialize',
                    params: {
                        protocolVersion: MCP_PROTOCOL_VERSION,
                        capabilities: {},
                        clientInfo: {
                            name: 'elastic-docs-synthetic',
                            version: '1.0.0',
                        },
                    },
                },
            })
            expect(res.status()).toBe(200)
            expect(res.headers()['content-type']).toContain('text/event-stream')
            expect(await res.text()).toContain('"protocolVersion"')
        })

        step('MCP lists its tools', async () => {
            const res = await request.post(mcpUrl, {
                headers: MCP_HEADERS,
                data: {
                    jsonrpc: '2.0',
                    id: 2,
                    method: 'tools/list',
                    params: {},
                },
            })
            expect(res.ok()).toBeTruthy()
            expect(await res.text()).toContain('search_docs')
        })

        step('MCP search_docs returns results', async () => {
            const res = await request.post(mcpUrl, {
                headers: MCP_HEADERS,
                data: {
                    jsonrpc: '2.0',
                    id: 3,
                    method: 'tools/call',
                    params: {
                        name: 'search_docs',
                        arguments: { query: 'elasticsearch' },
                    },
                },
            })
            expect(res.ok()).toBeTruthy()
            const body = await res.text()
            // JSON-RPC success has a "result" field; an error reply has "error" instead.
            expect(body).toContain('"result"')
        })
    })
}
