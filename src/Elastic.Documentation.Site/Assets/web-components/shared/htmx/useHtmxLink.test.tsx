import { useHtmxLink } from './useHtmxLink'
import { getPathFromUrl, isExternalDocsUrl } from './utils'
import { render, renderHook, screen } from '@testing-library/react'
import htmx from 'htmx.org'

// Mock htmx - it uses XPath which jsdom doesn't support properly
jest.mock('htmx.org', () => ({
    on: jest.fn(),
    off: jest.fn(),
    process: jest.fn(),
    ajax: jest.fn(),
}))

const mockHtmx = htmx as jest.Mocked<typeof htmx>

// Test component that uses the hook and renders an anchor
const TestLink = ({ url }: { url: string }) => {
    const { ref, href } = useHtmxLink(url)
    return (
        <a ref={ref} href={href} data-testid="test-link">
            Test Link
        </a>
    )
}

describe('isExternalDocsUrl', () => {
    it('should return true for /docs/api', () => {
        expect(isExternalDocsUrl('/docs/api')).toBe(true)
    })

    it('should return true for /docs/api/ paths', () => {
        expect(isExternalDocsUrl('/docs/api/')).toBe(true)
        expect(isExternalDocsUrl('/docs/api/elasticsearch')).toBe(true)
        expect(isExternalDocsUrl('/docs/api/kibana/some/path')).toBe(true)
    })

    it('should return false for regular docs paths', () => {
        expect(isExternalDocsUrl('/docs/elasticsearch')).toBe(false)
        expect(isExternalDocsUrl('/docs/kibana/dashboard')).toBe(false)
        expect(isExternalDocsUrl('/docs')).toBe(false)
        expect(isExternalDocsUrl('/docs/')).toBe(false)
    })

    it('should return false for paths that contain api but not under /docs/api/', () => {
        expect(isExternalDocsUrl('/docs/elasticsearch/api-reference')).toBe(
            false
        )
        expect(isExternalDocsUrl('/docs/api-guide')).toBe(false)
    })
})

describe('getPathFromUrl', () => {
    describe('paths (starting with /)', () => {
        it('should return paths as-is', () => {
            expect(getPathFromUrl('/docs/elasticsearch')).toBe(
                '/docs/elasticsearch'
            )
            expect(getPathFromUrl('/docs/api/kibana')).toBe('/docs/api/kibana')
            expect(getPathFromUrl('/')).toBe('/')
        })
    })

    describe('full elastic.co URLs', () => {
        it('should extract pathname from elastic.co docs URLs', () => {
            expect(
                getPathFromUrl('https://www.elastic.co/docs/elasticsearch')
            ).toBe('/docs/elasticsearch')
            expect(
                getPathFromUrl(
                    'https://elastic.co/docs/kibana/dashboard/overview'
                )
            ).toBe('/docs/kibana/dashboard/overview')
        })

        it('should return null for non-docs elastic.co URLs', () => {
            expect(getPathFromUrl('https://www.elastic.co/products')).toBe(null)
            expect(getPathFromUrl('https://elastic.co/about')).toBe(null)
        })

        it('should return null for elastic.co root URL', () => {
            expect(getPathFromUrl('https://www.elastic.co')).toBe(null)
            expect(getPathFromUrl('https://www.elastic.co/')).toBe(null)
        })

        it('should return null for elastic.co/guide URLs', () => {
            expect(getPathFromUrl('https://www.elastic.co/guide')).toBe(null)
            expect(
                getPathFromUrl('https://www.elastic.co/guide/en/elasticsearch')
            ).toBe(null)
        })
    })

    describe('external non-elastic.co URLs', () => {
        it('should return null for external URLs', () => {
            expect(getPathFromUrl('https://github.com/elastic/docs')).toBe(null)
            expect(getPathFromUrl('https://google.com')).toBe(null)
            expect(
                getPathFromUrl('https://developer.mozilla.org/docs/Web')
            ).toBe(null)
        })
    })

    describe('elastic.co subdomains without /docs path', () => {
        it('should return null for cloud.elastic.co URLs', () => {
            expect(getPathFromUrl('https://cloud.elastic.co/login')).toBe(null)
            expect(getPathFromUrl('https://cloud.elastic.co/deployments')).toBe(
                null
            )
        })

        it('should return null for discuss.elastic.co URLs', () => {
            expect(getPathFromUrl('https://discuss.elastic.co/t/topic')).toBe(
                null
            )
        })
    })

    describe('invalid URLs', () => {
        it('should return null for invalid URLs', () => {
            expect(getPathFromUrl('not-a-valid-url')).toBe(null)
            expect(getPathFromUrl('')).toBe(null)
        })
    })
})

describe('useHtmxLink', () => {
    describe('href normalization', () => {
        it('should return path as-is for paths', () => {
            const { result } = renderHook(() =>
                useHtmxLink('/docs/elasticsearch')
            )
            expect(result.current.href).toBe('/docs/elasticsearch')
        })

        it('should normalize full elastic.co URLs to paths', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://www.elastic.co/docs/elasticsearch')
            )
            expect(result.current.href).toBe('/docs/elasticsearch')
        })

        it('should return the path for /docs/api paths', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://www.elastic.co/docs/api/elasticsearch')
            )
            expect(result.current.href).toBe('/docs/api/elasticsearch')
        })
    })

    describe('external URLs should use full link', () => {
        it('should keep GitHub URLs as-is', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://github.com/elastic/docs')
            )
            expect(result.current.href).toBe('https://github.com/elastic/docs')
        })

        it('should keep other external URLs as-is', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://developer.mozilla.org/en-US/docs/Web')
            )
            expect(result.current.href).toBe(
                'https://developer.mozilla.org/en-US/docs/Web'
            )
        })

        it('should keep non-docs elastic.co URLs as-is', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://www.elastic.co/products/elasticsearch')
            )
            expect(result.current.href).toBe(
                'https://www.elastic.co/products/elasticsearch'
            )
        })

        it('should keep cloud.elastic.co URLs as-is', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://cloud.elastic.co/login')
            )
            expect(result.current.href).toBe('https://cloud.elastic.co/login')
        })

        it('should keep elastic.co root URL as-is', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://www.elastic.co')
            )
            expect(result.current.href).toBe('https://www.elastic.co')
        })

        it('should keep elastic.co/guide URLs as-is', () => {
            const { result } = renderHook(() =>
                useHtmxLink('https://www.elastic.co/guide/en/elasticsearch')
            )
            expect(result.current.href).toBe(
                'https://www.elastic.co/guide/en/elasticsearch'
            )
        })
    })

    describe('ref', () => {
        it('should return a ref object', () => {
            const { result } = renderHook(() =>
                useHtmxLink('/docs/elasticsearch')
            )
            expect(result.current.ref).toBeDefined()
            expect(result.current.ref.current).toBe(null) // Not attached yet
        })
    })

    describe('HTMX attribute handling', () => {
        beforeEach(() => {
            jest.clearAllMocks()
        })

        it('should call htmx.process for internal docs links', () => {
            render(<TestLink url="/docs/elasticsearch" />)

            const anchor = screen.getByTestId('test-link')

            // For internal links, htmx.process should be called
            expect(mockHtmx.process).toHaveBeenCalledWith(anchor)
            expect(anchor).not.toHaveAttribute('hx-disable')
        })

        it('should add hx-disable for /docs/api paths', () => {
            render(<TestLink url="/docs/api/elasticsearch" />)

            const anchor = screen.getByTestId('test-link')

            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for external URLs', () => {
            render(<TestLink url="https://github.com/elastic/docs" />)

            const anchor = screen.getByTestId('test-link')

            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for non-docs elastic.co URLs', () => {
            render(<TestLink url="https://www.elastic.co/products" />)

            const anchor = screen.getByTestId('test-link')

            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for elastic.co root URL', () => {
            render(<TestLink url="https://www.elastic.co" />)

            const anchor = screen.getByTestId('test-link')

            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })

        it('should add hx-disable for elastic.co/guide URLs', () => {
            render(
                <TestLink url="https://www.elastic.co/guide/en/elasticsearch" />
            )

            const anchor = screen.getByTestId('test-link')

            expect(anchor).toHaveAttribute('hx-disable', 'true')
            expect(mockHtmx.process).not.toHaveBeenCalled()
        })
    })
})
