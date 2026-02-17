import { useHtmxContainer } from './useHtmxContainer'
import { render, screen } from '@testing-library/react'
import htmx from 'htmx.org'
import { useRef } from 'react'

// Mock config to use assembler build type (tests were written for assembler behavior)
jest.mock('../../../config', () => ({
    config: {
        buildType: 'assembler',
        serviceName: 'docs-frontend',
        telemetryEnabled: true,
        rootPath: '/docs',
    },
}))

// Mock htmx - it uses XPath which jsdom doesn't support properly
jest.mock('htmx.org', () => ({
    on: jest.fn(),
    off: jest.fn(),
    process: jest.fn(),
    ajax: jest.fn(),
}))

const mockHtmx = htmx as jest.Mocked<typeof htmx>

// Test component that uses the hook
const TestContainer = ({ html }: { html: string }) => {
    const containerRef = useRef<HTMLDivElement>(null)
    useHtmxContainer(containerRef, [html])
    return (
        <div
            ref={containerRef}
            data-testid="container"
            dangerouslySetInnerHTML={{ __html: html }}
        />
    )
}

describe('useHtmxContainer', () => {
    beforeEach(() => {
        jest.clearAllMocks()
    })

    describe('internal docs links', () => {
        it('should apply htmx attributes and call htmx.process for internal links', () => {
            render(
                <TestContainer html='<a href="/docs/elasticsearch">Link</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute('href', '/docs/elasticsearch')
            expect(anchor).not.toHaveAttribute('hx-disable')
            expect(mockHtmx.process).toHaveBeenCalledWith(container)
        })

        it('should normalize full elastic.co docs URLs to paths', () => {
            render(
                <TestContainer html='<a href="https://www.elastic.co/docs/kibana">Link</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute('href', '/docs/kibana')
            expect(anchor).not.toHaveAttribute('hx-disable')
            expect(mockHtmx.process).toHaveBeenCalled()
        })
    })

    describe('/docs/api paths should be disabled', () => {
        it('should add hx-disable for /docs/api paths', () => {
            render(
                <TestContainer html='<a href="/docs/api/elasticsearch">API Link</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute('href', '/docs/api/elasticsearch')
            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for full elastic.co/docs/api URLs', () => {
            render(
                <TestContainer html='<a href="https://www.elastic.co/docs/api/kibana">API Link</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute('href', '/docs/api/kibana')
            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })
    })

    describe('external URLs should be disabled', () => {
        it('should add hx-disable for GitHub URLs', () => {
            render(
                <TestContainer html='<a href="https://github.com/elastic/docs">GitHub</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute(
                'href',
                'https://github.com/elastic/docs'
            )
            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for elastic.co non-docs URLs', () => {
            render(
                <TestContainer html='<a href="https://www.elastic.co/products">Products</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute(
                'href',
                'https://www.elastic.co/products'
            )
            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for elastic.co/guide URLs', () => {
            render(
                <TestContainer html='<a href="https://www.elastic.co/guide/en/elasticsearch">Guide</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute(
                'href',
                'https://www.elastic.co/guide/en/elasticsearch'
            )
            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for cloud.elastic.co URLs', () => {
            render(
                <TestContainer html='<a href="https://cloud.elastic.co/login">Cloud</a>' />
            )

            const container = screen.getByTestId('container')
            const anchor = container.querySelector('a')

            expect(anchor).toHaveAttribute(
                'href',
                'https://cloud.elastic.co/login'
            )
            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })
    })

    describe('mixed content', () => {
        it('should handle container with both internal and external links', () => {
            render(
                <TestContainer
                    html={`
                        <a href="/docs/elasticsearch" data-testid="internal">Internal</a>
                        <a href="/docs/api/kibana" data-testid="api">API</a>
                        <a href="https://github.com/elastic" data-testid="external">External</a>
                    `}
                />
            )

            const container = screen.getByTestId('container')
            const internalLink = screen.getByTestId('internal')
            const apiLink = screen.getByTestId('api')
            const externalLink = screen.getByTestId('external')

            // Internal link - htmx enabled
            expect(internalLink).toHaveAttribute('href', '/docs/elasticsearch')
            expect(internalLink).not.toHaveAttribute('hx-disable')

            // API link - htmx disabled
            expect(apiLink).toHaveAttribute('href', '/docs/api/kibana')
            expect(apiLink).toHaveAttribute('hx-disable', 'true')

            // External link - htmx disabled
            expect(externalLink).toHaveAttribute(
                'href',
                'https://github.com/elastic'
            )
            expect(externalLink).toHaveAttribute('hx-disable', 'true')

            // htmx.process should be called because there's at least one internal link
            expect(mockHtmx.process).toHaveBeenCalledWith(container)
        })

        it('should not call htmx.process if all links are external', () => {
            render(
                <TestContainer
                    html={`
                        <a href="/docs/api/elasticsearch">API</a>
                        <a href="https://github.com/elastic">GitHub</a>
                    `}
                />
            )

            expect(mockHtmx.process).not.toHaveBeenCalled()
        })
    })
})
