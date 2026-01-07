import DOMPurify from 'dompurify'
import { memo, useMemo } from 'react'

interface SanitizedHtmlContentProps {
    htmlContent: string
    ellipsis?: boolean
}

export const SanitizedHtmlContent = memo(
    ({ htmlContent, ellipsis }: SanitizedHtmlContentProps) => {
        const processed = useMemo(() => {
            if (!htmlContent) return ''

            const sanitized = DOMPurify.sanitize(htmlContent, {
                ALLOWED_TAGS: ['mark'],
                ALLOWED_ATTR: [],
                KEEP_CONTENT: true,
            })

            if (!ellipsis) {
                return sanitized
            }

            const temp = document.createElement('div')
            temp.innerHTML = sanitized

            const text = temp.textContent || ''
            const firstChar = text.trim()[0]

            // Add ellipsis when text starts mid-sentence to indicate continuation
            if (firstChar && /[a-z]/.test(firstChar)) {
                return '… ' + sanitized
            }

            return sanitized
        }, [htmlContent, ellipsis])

        return <span dangerouslySetInnerHTML={{ __html: processed }} />
    }
)

SanitizedHtmlContent.displayName = 'SanitizedHtmlContent'
