async function highlightHttp(source: string, language = 'http') {
    vi.resetModules()
    const { initHighlight } = await import('./hljs')
    document.body.innerHTML =
        '<div id="api-examples-panel"><pre><code></code></pre></div>'
    const code = document.querySelector('code')!
    code.className = `language-${language}`
    code.textContent = source
    await initHighlight()
    expect(code.textContent).toBe(source)
    return code
}

it('highlights the generated sample: a bare separator, then the request', async () => {
    const code = await highlightHttp(`###
POST {{ELASTICSEARCH_URL}}/_search
Authorization: ApiKey {{API_KEY}}
Content-Type: application/json

{"size": 1}
`)

    expect(code.querySelector('.hljs-comment')?.textContent).toBe('###')
    expect(code.querySelector('.hljs-keyword')?.textContent).toBe('POST')
    expect(
        [...code.querySelectorAll('.hljs-subst')].map((e) => e.textContent)
    ).toEqual(['{{ELASTICSEARCH_URL}}', '{{API_KEY}}'])
    expect(
        [...code.querySelectorAll('.hljs-attribute')].map((e) => e.textContent)
    ).toEqual(['Authorization', 'Content-Type'])
    expect(code.querySelector('.hljs-attr')?.textContent).toBe('"size"')
})

it('highlights an authored HTTP file including variables and its JSON body', async () => {
    const code =
        await highlightHttp(`@BASE_URL = https://your-deployment.example
@API_KEY = REPLACE_ME

### Clone an API key
PUT {{BASE_URL}}/_security/api_key/clone
Authorization: ApiKey {{API_KEY}}
Content-Type: application/json

{
  "api_key": "string",
  "enabled": true,
  "count": 2
}
`)

    expect(code.querySelector('.hljs-keyword')?.textContent).toBe('PUT')
    expect(code.querySelector('.hljs-variable')?.textContent).toBe('@BASE_URL')
    expect(code.querySelector('.hljs-subst')?.textContent).toBe('{{BASE_URL}}')
    expect(code.querySelector('.hljs-comment')?.textContent).toBe(
        '### Clone an API key'
    )
    expect(code.querySelector('.hljs-attribute')?.textContent).toBe(
        'Authorization'
    )
    expect(code.querySelector('.hljs-attr')?.textContent).toBe('"api_key"')
    expect(code.querySelector('.hljs-string')?.textContent).toContain(
        '{{BASE_URL}}/_security/api_key/clone'
    )
    expect(code.querySelector('.hljs-literal')?.textContent).toBe('true')
    expect(code.querySelector('.hljs-number')?.textContent).toBe('2')
})

it('keeps request separators and bodyless requests outside JSON bodies', async () => {
    const code = await highlightHttp(`@BASE_URL = https://example.test

### First request
POST {{BASE_URL}}/first
Content-Type: application/json

{"first": 1}

### Second request
GET {{BASE_URL}}/second
X-Trace: {{TRACE}}

### Third request
PUT {{BASE_URL}}/third HTTP/1.1
Content-Type: application/json

{"third": false}
`)

    expect(
        [...code.querySelectorAll(':scope > .hljs-keyword')].map(
            (e) => e.textContent
        )
    ).toEqual(['POST', 'GET', 'PUT'])
    expect(
        [...code.querySelectorAll('.hljs-attr')].map((e) => e.textContent)
    ).toEqual(['"first"', '"third"'])
    expect(
        [...code.querySelectorAll('.hljs-subst')].map((e) => e.textContent)
    ).toContain('{{TRACE}}')
    expect(code.querySelector('.hljs-meta')?.textContent).toBe('HTTP/1.1')
})

it('preserves raw HTTP response highlighting and the https alias', async () => {
    const code = await highlightHttp(
        `HTTP/1.1 200 OK
Content-Type: application/json

{"count": 1}
`,
        'https'
    )

    expect(code.querySelector('.hljs-meta')?.textContent).toBe('HTTP/1.1')
    expect(code.querySelector('.hljs-number')?.textContent).toBe('200')
    expect(code.querySelector('.hljs-attribute')?.textContent).toBe(
        'Content-Type'
    )
})

it('preserves raw versioned HTTP request body highlighting', async () => {
    const source = `POST /documents HTTP/1.1
Content-Type: application/xml

<document id="1"><title>Example</title></document>
`
    const code = await highlightHttp(source)
    const { hljs, toLanguageFn } = await import('./hljs')
    const http = await import('highlight.js/lib/languages/http')
    hljs.registerLanguage('http-wire', toLanguageFn(http))
    const wire = document.createElement('code')
    wire.innerHTML = hljs.highlight(source, { language: 'http-wire' }).value
    const body = 'span[class^="language-"]'

    expect(code.querySelector('.hljs-keyword')?.textContent).toBe('POST')
    expect(code.querySelector('.hljs-meta')?.textContent).toBe('HTTP/1.1')
    expect(wire.querySelector(body)).not.toBeNull()
    expect(code.querySelector(body)?.innerHTML.trim()).toBe(
        wire.querySelector(body)?.innerHTML.trim()
    )
})
