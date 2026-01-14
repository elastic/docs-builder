import { useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import DOMPurify from 'dompurify'
import { Marked, RendererObject, Tokens } from 'marked'
import { useEffect, useMemo, useRef } from 'react'
import { initCopyButton } from '../../copybutton'
import { hljs } from '../../hljs'

// Create the marked instance once globally
const createMarkedInstance = () => {
    const renderer: RendererObject = {
        code({ text, lang }: Tokens.Code): string {
            let highlighted: string
            try {
                highlighted = lang
                    ? hljs.highlight(text, { language: lang }).value
                    : hljs.highlightAuto(text).value
            } catch {
                highlighted = hljs.highlightAuto(text).value
            }
            return `<div class="highlight">
                <pre>
                    <code class="language-${lang}">${highlighted}</code>
                </pre>
            </div>`
        },
        table(token: Tokens.Table): string {
            const defaultMarked = new Marked()
            const defaultTableHtml = defaultMarked.parse(token.raw)
            return `<div class="table-wrapper">${defaultTableHtml}</div>`
        },
    }
    return new Marked({ renderer })
}

const markedInstance = createMarkedInstance()

interface MarkdownContentProps {
    content: string
    enableCopyButtons?: boolean
    copyButtonPrefix?: string
}

export const MarkdownContent = ({
    content,
    enableCopyButtons = true,
    copyButtonPrefix = 'markdown-codecell-',
}: MarkdownContentProps) => {
    const { euiTheme } = useEuiTheme()
    const ref = useRef<HTMLDivElement>(null)

    const parsed = useMemo(() => {
        const html = markedInstance.parse(content) as string
        return DOMPurify.sanitize(html)
    }, [content])

    useEffect(() => {
        if (enableCopyButtons && ref.current && content) {
            const timer = setTimeout(() => {
                try {
                    initCopyButton(
                        '.highlight pre',
                        ref.current!,
                        copyButtonPrefix
                    )
                } catch (error) {
                    console.error('Failed to initialize copy buttons:', error)
                }
            }, 100)
            return () => clearTimeout(timer)
        }
    }, [content, enableCopyButtons, copyButtonPrefix])

    return (
        <div
            ref={ref}
            className="markdown-content"
            css={css`
                font-size: 14px;
                line-height: 1.6;

                & > *:first-child {
                    margin-top: 0;
                }

                & > *:last-child {
                    margin-bottom: 0;
                }

                p {
                    margin: 0.75em 0;
                }

                code {
                    background: ${euiTheme.colors.lightestShade};
                    padding: 0.125em 0.25em;
                    border-radius: ${euiTheme.border.radius.small};
                    font-size: 0.9em;
                }

                pre {
                    background: ${euiTheme.colors.lightestShade};
                    padding: ${euiTheme.size.m};
                    border-radius: ${euiTheme.border.radius.medium};
                    overflow-x: auto;
                    margin: 1em 0;

                    code {
                        background: none;
                        padding: 0;
                    }
                }

                ul,
                ol {
                    margin: 0.75em 0;
                    padding-left: 1.5em;
                }

                li {
                    margin: 0.25em 0;
                }

                a {
                    color: ${euiTheme.colors.primary};
                    text-decoration: none;

                    &:hover {
                        text-decoration: underline;
                    }
                }

                blockquote {
                    border-left: 3px solid ${euiTheme.colors.lightShade};
                    margin: 1em 0;
                    padding-left: 1em;
                    color: ${euiTheme.colors.subduedText};
                }

                h1,
                h2,
                h3,
                h4,
                h5,
                h6 {
                    margin-top: 1.25em;
                    margin-bottom: 0.5em;
                    font-weight: ${euiTheme.font.weight.semiBold};
                }

                h1 {
                    font-size: 1.5em;
                }
                h2 {
                    font-size: 1.3em;
                }
                h3 {
                    font-size: 1.15em;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin: 1em 0;
                }

                th,
                td {
                    border: 1px solid ${euiTheme.colors.lightShade};
                    padding: ${euiTheme.size.s};
                    text-align: left;
                }

                th {
                    background: ${euiTheme.colors.lightestShade};
                    font-weight: ${euiTheme.font.weight.semiBold};
                }
            `}
            dangerouslySetInnerHTML={{ __html: parsed }}
        />
    )
}
