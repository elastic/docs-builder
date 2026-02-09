const createRect = () => ({
	width: 100,
	height: 60,
	top: 0,
	left: 0,
	bottom: 60,
	right: 100,
	x: 0,
	y: 0,
	toJSON: () => ({}),
})

const setVisibleRect = (element: HTMLElement) => {
	Object.defineProperty(element, 'getBoundingClientRect', {
		value: () => createRect(),
	})
	Object.defineProperty(element, 'getClientRects', {
		value: () => [createRect()],
	})
}

class MockIntersectionObserver {
	callback: IntersectionObserverCallback
	observe = jest.fn()
	unobserve = jest.fn()
	disconnect = jest.fn()

	constructor(callback: IntersectionObserverCallback) {
		this.callback = callback
	}
}

const setupMermaid = () => {
	window.mermaid = {
		initialize: jest.fn(),
		render: jest.fn().mockResolvedValue({ svg: '<svg></svg>' }),
	}
}

const setupScriptLoad = () => {
	return jest
		.spyOn(document.head, 'appendChild')
		.mockImplementation((node) => {
			if (node instanceof HTMLScriptElement && node.onload) {
				node.onload(new Event('load'))
			}

			return node
		})
}

describe('initMermaid', () => {
	beforeEach(() => {
		jest.resetModules()
		document.body.innerHTML = ''
		global.IntersectionObserver =
			MockIntersectionObserver as unknown as typeof IntersectionObserver
	})

	afterEach(() => {
		jest.restoreAllMocks()
	})

	it('renders visible mermaid blocks immediately', async () => {
		setupMermaid()
		setupScriptLoad()

		const pre = document.createElement('pre')
		pre.className = 'mermaid'
		pre.textContent = 'flowchart LR\nA-->B'
		setVisibleRect(pre)
		document.body.appendChild(pre)

		const { initMermaid } = await import('./mermaid')
		await initMermaid()

		expect(window.mermaid.render).toHaveBeenCalledTimes(1)
	})

	it('renders mermaid blocks when a tab is activated', async () => {
		setupMermaid()
		setupScriptLoad()

		const tabs = document.createElement('div')
		tabs.className = 'tabs'

		const input = document.createElement('input')
		input.className = 'tabs-input'
		input.type = 'radio'
		input.checked = false

		const label = document.createElement('label')
		label.className = 'tabs-label'
		label.textContent = 'Rendered'

		const panel = document.createElement('div')
		panel.className = 'tabs-content'

		const pre = document.createElement('pre')
		pre.className = 'mermaid'
		pre.textContent = 'flowchart LR\nA-->B'
		setVisibleRect(pre)

		panel.appendChild(pre)
		tabs.append(input, label, panel)
		document.body.appendChild(tabs)

		const { initMermaid } = await import('./mermaid')
		await initMermaid()

		expect(window.mermaid.render).not.toHaveBeenCalled()

		input.checked = true
		input.dispatchEvent(new Event('change', { bubbles: true }))
		await new Promise((resolve) => setTimeout(resolve, 0))

		expect(window.mermaid.render).toHaveBeenCalledTimes(1)
	})
})
