/**
 * Semantic Conventions for Documentation Site Telemetry
 *
 * This file defines custom attribute names for search telemetry.
 * Standard OpenTelemetry semconv attributes are imported from @opentelemetry/semantic-conventions.
 *
 * References:
 * - https://opentelemetry.io/docs/specs/semconv/
 * - https://opentelemetry.io/docs/specs/semconv/attributes-registry/
 */

// Re-export standard OpenTelemetry semantic conventions
export {
    ATTR_SERVICE_NAME,
    ATTR_SERVICE_VERSION,
    ATTR_HTTP_RESPONSE_STATUS_CODE,
    ATTR_ERROR_TYPE,
    ATTR_EXCEPTION_MESSAGE,
} from '@opentelemetry/semantic-conventions'

// ============================================================================
// SEARCH ATTRIBUTES (Custom)
// ============================================================================

/**
 * The search query string entered by the user
 * @example "elasticsearch aggregations"
 */
export const ATTR_SEARCH_QUERY = 'search.query'

/**
 * Length of the search query string
 * @example 25
 */
export const ATTR_SEARCH_QUERY_LENGTH = 'search.query.length'

/**
 * Current page number in search results (0-based)
 * @example 0
 */
export const ATTR_SEARCH_PAGE = 'search.page'

/**
 * Total number of search results found
 * @example 142
 */
export const ATTR_SEARCH_RESULTS_TOTAL = 'search.results.total'

/**
 * Number of results returned in current page
 * @example 10
 */
export const ATTR_SEARCH_RESULTS_COUNT = 'search.results.count'

/**
 * Total number of pages available
 * @example 15
 */
export const ATTR_SEARCH_PAGE_COUNT = 'search.page.count'

/**
 * Whether the search query was empty
 * @example true
 */
export const ATTR_SEARCH_EMPTY_QUERY = 'search.empty_query'

/**
 * Whether the search resulted in an error
 * @example false
 */
export const ATTR_SEARCH_ERROR = 'search.error'

// ============================================================================
// SEARCH RESULT CLICK ATTRIBUTES (Custom)
// ============================================================================

/**
 * URL of the clicked search result
 * @example "/docs/elasticsearch/reference/current/search-aggregations.html"
 */
export const ATTR_SEARCH_RESULT_URL = 'search.result.url'

/**
 * Title of the clicked search result
 * @example "Aggregations"
 */
export const ATTR_SEARCH_RESULT_TITLE = 'search.result.title'

/**
 * Absolute position of the result across all pages (0-based)
 * @example 23
 */
export const ATTR_SEARCH_RESULT_POSITION = 'search.result.position'

/**
 * Position of the result within the current page (0-based)
 * @example 3
 */
export const ATTR_SEARCH_RESULT_POSITION_ON_PAGE =
    'search.result.position_on_page'

/**
 * Relevance score of the search result
 * @example 0.85
 */
export const ATTR_SEARCH_RESULT_SCORE = 'search.result.score'

// ============================================================================
// FIND IN DOCS ATTRIBUTES (Custom)
// These are for the navigation "Find in Docs" feature (quick search in sidebar).
// Separate from the advanced Search page telemetry.
// ============================================================================

/**
 * The query string entered by the user in Find in Docs
 * @example "elasticsearch aggregations"
 */
export const ATTR_FIND_IN_DOCS_QUERY = 'find_in_docs.query'

/**
 * Length of the query string
 * @example 25
 */
export const ATTR_FIND_IN_DOCS_QUERY_LENGTH = 'find_in_docs.query.length'

/**
 * Total number of results found
 * @example 142
 */
export const ATTR_FIND_IN_DOCS_RESULTS_TOTAL = 'find_in_docs.results.total'

/**
 * How the Find in Docs was initiated
 * @example "keyboard_shortcut" | "focus" | "click"
 */
export const ATTR_FIND_IN_DOCS_TRIGGER = 'find_in_docs.trigger'

/**
 * Reason the Find in Docs was closed
 * @example "escape" | "blur" | "navigate" | "clear"
 */
export const ATTR_FIND_IN_DOCS_CLOSE_REASON = 'find_in_docs.close_reason'

/**
 * Whether Find in Docs had results when closed
 * @example true
 */
export const ATTR_FIND_IN_DOCS_HAD_RESULTS = 'find_in_docs.had_results'

/**
 * Whether a result was selected when Find in Docs closed
 * @example true
 */
export const ATTR_FIND_IN_DOCS_HAD_SELECTION = 'find_in_docs.had_selection'

/**
 * Navigation method used (keyboard or mouse)
 * @example "keyboard" | "mouse"
 */
export const ATTR_FIND_IN_DOCS_NAVIGATION_METHOD =
    'find_in_docs.navigation.method'

/**
 * Navigation direction
 * @example "up" | "down"
 */
export const ATTR_FIND_IN_DOCS_NAVIGATION_DIRECTION =
    'find_in_docs.navigation.direction'

/**
 * URL of the clicked result
 * @example "/docs/elasticsearch/reference/current/search-aggregations.html"
 */
export const ATTR_FIND_IN_DOCS_RESULT_URL = 'find_in_docs.result.url'

/**
 * Position of the clicked result (0-based)
 * @example 3
 */
export const ATTR_FIND_IN_DOCS_RESULT_POSITION = 'find_in_docs.result.position'

/**
 * Relevance score of the clicked result
 * @example 0.85
 */
export const ATTR_FIND_IN_DOCS_RESULT_SCORE = 'find_in_docs.result.score'

/**
 * Retry-After header value in seconds (for rate limiting)
 * @example 30
 */
export const ATTR_FIND_IN_DOCS_RETRY_AFTER = 'find_in_docs.retry_after'

// ============================================================================
// EVENT ATTRIBUTES (Custom)
// ============================================================================

/**
 * Name of the event being tracked
 * @example "search_result_clicked"
 */
export const ATTR_EVENT_NAME = 'event.name'

/**
 * Category of the event
 * @example "ui"
 */
export const ATTR_EVENT_CATEGORY = 'event.category'
