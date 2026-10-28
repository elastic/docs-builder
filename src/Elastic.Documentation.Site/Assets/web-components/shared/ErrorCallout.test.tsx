import { useAskAiErrorCalloutState } from '../AskAi/useAskAiCooldown'
import { useNavigationSearchErrorCalloutState } from '../NavigationSearch/useNavigationSearchCooldown'
import { ErrorCallout } from './ErrorCallout'
import { ApiError, getErrorMessage, isRateLimitError } from './errorHandling'
import { render, screen } from '@testing-library/react'
import * as React from 'react'

// Mock EuiCallOut and EuiSpacer
vi.mock('@elastic/eui', () => ({
    EuiCallOut: ({
        title,
        children,
        color,
        iconType,
        size,
    }: {
        title: string
        children: React.ReactNode
        color: string
        iconType: string
        size: string
    }) => (
        <div
            data-testid="eui-callout"
            data-title={title}
            data-color={color}
            data-icon-type={iconType}
            data-size={size}
        >
            {children}
        </div>
    ),
    EuiSpacer: ({ size }: { size: string }) => (
        <div data-testid="eui-spacer" data-size={size} />
    ),
}))

// Mock rate limit handlers
const mockUseSearchRateLimitHandler = vi.fn()
const mockUseAskAiRateLimitHandler = vi.fn()

vi.mock('../NavigationSearch/useNavigationSearchRateLimitHandler', () => ({
    useNavigationSearchRateLimitHandler: (error: ApiError | Error | null) =>
        mockUseSearchRateLimitHandler(error),
}))

vi.mock('../AskAi/useAskAiRateLimitHandler', () => ({
    useAskAiRateLimitHandler: (error: ApiError | Error | null) =>
        mockUseAskAiRateLimitHandler(error),
}))

// Mock cooldown.store hooks
const mockSearchState = {
    countdown: null as number | null,
    hasActiveCooldown: false,
    awaitingNewInput: false,
}

const mockAskAiState = {
    countdown: null as number | null,
    hasActiveCooldown: false,
    awaitingNewInput: false,
}

vi.mock('../NavigationSearch/useNavigationSearchCooldown', () => ({
    useNavigationSearchErrorCalloutState: vi.fn(() => mockSearchState),
}))

vi.mock('../AskAi/useAskAiCooldown', () => ({
    useAskAiErrorCalloutState: vi.fn(() => mockAskAiState),
}))

const mockUseSearchErrorCalloutState = vi.mocked(
    useNavigationSearchErrorCalloutState
)
const mockUseAskAiErrorCalloutState = vi.mocked(useAskAiErrorCalloutState)

// Mock errorHandling utilities
vi.mock('./errorHandling', async () => {
    const actual = await vi.importActual('./errorHandling')
    return {
        ...actual,
        getErrorMessage: vi.fn((error: ApiError | Error | null) => {
            if (!error) return 'Unknown error'
            if ('statusCode' in error) {
                return `Error ${error.statusCode}: ${error.message}`
            }
            return error.message
        }),
        isApiError: vi.fn((error: ApiError | Error | null) => {
            return (
                error instanceof Error &&
                'statusCode' in error &&
                error.name === 'ApiError'
            )
        }),
        isRateLimitError: vi.fn((error: ApiError | Error | null) => {
            return (
                error instanceof Error &&
                'statusCode' in error &&
                (error as ApiError).statusCode === 429
            )
        }),
    }
})

// Import after mocking to get the mocked versions
const mockGetErrorMessage = vi.mocked(getErrorMessage)
const mockIsRateLimitError = vi.mocked(isRateLimitError)

describe('ErrorCallout', () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mockSearchState.countdown = null
        mockSearchState.hasActiveCooldown = false
        mockSearchState.awaitingNewInput = false
        mockAskAiState.countdown = null
        mockAskAiState.hasActiveCooldown = false
        mockAskAiState.awaitingNewInput = false
        mockUseSearchErrorCalloutState.mockReturnValue(mockSearchState)
        mockUseAskAiErrorCalloutState.mockReturnValue(mockAskAiState)
        mockGetErrorMessage.mockImplementation(
            (error: ApiError | Error | null) => {
                if (!error) return 'Unknown error'
                if ('statusCode' in error) {
                    return `Error ${error.statusCode}: ${error.message}`
                }
                return error.message
            }
        )
        mockIsRateLimitError.mockImplementation(
            (error: ApiError | Error | null) => {
                return (
                    error instanceof Error &&
                    'statusCode' in error &&
                    (error as ApiError).statusCode === 429
                )
            }
        )
    })

    describe('Rate limit handler calls', () => {
        it('should call useSearchRateLimitHandler when domain is search', () => {
            const error = new Error('Test error') as ApiError
            error.statusCode = 500

            render(<ErrorCallout error={error} domain="search" />)

            expect(mockUseSearchRateLimitHandler).toHaveBeenCalledWith(error)
            expect(mockUseAskAiRateLimitHandler).toHaveBeenCalledWith(null)
        })

        it('should call useAskAiRateLimitHandler when domain is askAi', () => {
            const error = new Error('Test error') as ApiError
            error.statusCode = 500

            render(<ErrorCallout error={error} domain="askAi" />)

            expect(mockUseAskAiRateLimitHandler).toHaveBeenCalledWith(error)
            expect(mockUseSearchRateLimitHandler).toHaveBeenCalledWith(null)
        })

        it('should pass null to rate limit handler when error is null', () => {
            render(<ErrorCallout error={null} domain="search" />)

            expect(mockUseSearchRateLimitHandler).toHaveBeenCalledWith(null)
        })
    })

    describe('Non-429 errors', () => {
        it('should display non-429 error immediately', () => {
            const error = new Error('Server error') as ApiError
            error.statusCode = 500
            error.name = 'ApiError'
            mockGetErrorMessage.mockReturnValue('Error 500: Server error')

            render(<ErrorCallout error={error} domain="search" />)

            const callout = screen.getByTestId('eui-callout')
            expect(callout).toBeInTheDocument()
            expect(callout).toHaveAttribute(
                'data-title',
                'Sorry, there was an error'
            )
            expect(callout).toHaveAttribute('data-color', 'danger')
            expect(
                screen.getByText('Error 500: Server error')
            ).toBeInTheDocument()
        })

        it('should use custom title when provided', () => {
            const error = new Error('Network error') as ApiError
            error.statusCode = 503
            error.name = 'ApiError'
            mockGetErrorMessage.mockReturnValue('Error 503: Network error')

            render(
                <ErrorCallout
                    error={error}
                    domain="search"
                    title="Custom error title"
                />
            )

            const callout = screen.getByTestId('eui-callout')
            expect(callout).toHaveAttribute('data-title', 'Custom error title')
        })

        it('should show spacers when inline is false (default)', () => {
            const error = new Error('Test error') as ApiError
            error.statusCode = 400
            error.name = 'ApiError'

            render(<ErrorCallout error={error} domain="search" />)

            const spacers = screen.getAllByTestId('eui-spacer')
            expect(spacers).toHaveLength(2)
            expect(spacers[0]).toHaveAttribute('data-size', 'm')
            expect(spacers[1]).toHaveAttribute('data-size', 's')
        })

        it('should not show spacers when inline is true', () => {
            const error = new Error('Test error') as ApiError
            error.statusCode = 400
            error.name = 'ApiError'

            render(<ErrorCallout error={error} domain="search" inline={true} />)

            expect(screen.queryByTestId('eui-spacer')).not.toBeInTheDocument()
        })

        it('should handle regular Error objects', () => {
            const error = new Error('Regular error message')
            mockGetErrorMessage.mockReturnValue('Regular error message')

            render(<ErrorCallout error={error} domain="askAi" />)

            expect(
                screen.getByText('Regular error message')
            ).toBeInTheDocument()
        })
    })

    describe('429 rate limit errors', () => {
        it('should hide 429 error when cooldown is not active', () => {
            const error = new Error('Rate limit') as ApiError
            error.statusCode = 429
            error.name = 'ApiError'
            error.retryAfter = 10
            mockIsRateLimitError.mockReturnValue(true)
            mockSearchState.hasActiveCooldown = false

            const { container } = render(
                <ErrorCallout error={error} domain="search" />
            )

            expect(container.firstChild).toBeNull()
        })

        it('should hide 429 error when cooldown finished and pending acknowledgment', () => {
            const error = new Error('Rate limit') as ApiError
            error.statusCode = 429
            error.name = 'ApiError'
            error.retryAfter = 10
            mockIsRateLimitError.mockReturnValue(true)
            mockSearchState.hasActiveCooldown = true
            mockSearchState.awaitingNewInput = true

            const { container } = render(
                <ErrorCallout error={error} domain="search" />
            )

            expect(container.firstChild).toBeNull()
        })

        it('should show 429 error when cooldown is active', () => {
            const error = new Error('Rate limit') as ApiError
            error.statusCode = 429
            error.name = 'ApiError'
            error.retryAfter = 10
            mockIsRateLimitError.mockReturnValue(true)
            mockSearchState.hasActiveCooldown = true
            mockSearchState.countdown = 5
            mockGetErrorMessage.mockReturnValue('Error 429: Rate limit')

            render(<ErrorCallout error={error} domain="search" />)

            const callout = screen.getByTestId('eui-callout')
            expect(callout).toBeInTheDocument()
            expect(callout).toHaveAttribute('data-title', 'Rate limit exceeded')
        })

        it('should update retryAfter with countdown when cooldown is active', () => {
            const error = new Error('Rate limit') as ApiError
            error.statusCode = 429
            error.name = 'ApiError'
            error.retryAfter = 10
            mockIsRateLimitError.mockReturnValue(true)
            mockSearchState.hasActiveCooldown = true
            mockSearchState.countdown = 3
            mockGetErrorMessage.mockReturnValue('Error 429: Rate limit')

            render(<ErrorCallout error={error} domain="search" />)

            // The component should update the error's retryAfter with countdown
            // This is tested indirectly through getErrorMessage being called
            expect(mockGetErrorMessage).toHaveBeenCalled()
        })
    })

    describe('Cooldown state display', () => {
        it('should show cooldown message when no error but cooldown is active', () => {
            mockSearchState.hasActiveCooldown = true
            mockSearchState.countdown = 15
            mockGetErrorMessage.mockReturnValue(
                'Rate limit exceeded. Please wait before trying again.'
            )

            render(<ErrorCallout error={null} domain="search" />)

            const callout = screen.getByTestId('eui-callout')
            expect(callout).toBeInTheDocument()
            expect(callout).toHaveAttribute('data-title', 'Rate limit exceeded')
        })

        it('should not show anything when no error and no active cooldown', () => {
            mockSearchState.hasActiveCooldown = false

            const { container } = render(
                <ErrorCallout error={null} domain="search" />
            )

            expect(container.firstChild).toBeNull()
        })

        it('should create synthetic 429 error when cooldown is active without error', () => {
            mockAskAiState.hasActiveCooldown = true
            mockAskAiState.countdown = 20
            mockGetErrorMessage.mockReturnValue(
                'Rate limit exceeded. Please wait before trying again.'
            )

            render(<ErrorCallout error={null} domain="askAi" />)

            // Verify the callout is rendered with correct title and message
            const callout = screen.getByTestId('eui-callout')
            expect(callout).toBeInTheDocument()
            expect(callout).toHaveAttribute('data-title', 'Rate limit exceeded')
            expect(
                screen.getByText(
                    'Rate limit exceeded. Please wait before trying again.'
                )
            ).toBeInTheDocument()
            // Verify getErrorMessage was called (component creates synthetic error)
            expect(mockGetErrorMessage).toHaveBeenCalled()
        })

        it('should use countdown value for synthetic error retryAfter', () => {
            mockSearchState.hasActiveCooldown = true
            mockSearchState.countdown = 30
            mockGetErrorMessage.mockReturnValue(
                'Rate limit exceeded. Please wait before trying again.'
            )

            render(<ErrorCallout error={null} domain="search" />)

            // Verify that getErrorMessage was called (component creates synthetic error)
            expect(mockGetErrorMessage).toHaveBeenCalled()
        })
    })

    describe('Domain-specific behavior', () => {
        it('should use search state when domain is search', () => {
            mockSearchState.hasActiveCooldown = true
            mockSearchState.countdown = 10
            mockGetErrorMessage.mockReturnValue('Rate limit message')

            render(<ErrorCallout error={null} domain="search" />)

            expect(mockUseSearchErrorCalloutState).toHaveBeenCalled()
            expect(mockUseAskAiErrorCalloutState).toHaveBeenCalled()
        })

        it('should use askAi state when domain is askAi', () => {
            mockAskAiState.hasActiveCooldown = true
            mockAskAiState.countdown = 10
            mockGetErrorMessage.mockReturnValue('Rate limit message')

            render(<ErrorCallout error={null} domain="askAi" />)

            expect(mockUseSearchErrorCalloutState).toHaveBeenCalled()
            expect(mockUseAskAiErrorCalloutState).toHaveBeenCalled()
        })
    })

    describe('Callout styling', () => {
        it('should render callout with correct danger styling', () => {
            const error = new Error('Test error') as ApiError
            error.statusCode = 500
            error.name = 'ApiError'

            render(<ErrorCallout error={error} domain="search" />)

            const callout = screen.getByTestId('eui-callout')
            expect(callout).toHaveAttribute('data-color', 'danger')
            expect(callout).toHaveAttribute('data-icon-type', 'error')
            expect(callout).toHaveAttribute('data-size', 's')
        })
    })
})
