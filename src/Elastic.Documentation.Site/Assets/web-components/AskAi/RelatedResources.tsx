/** @jsxImportSource @emotion/react */
import { useHtmxLink } from '../shared/htmx/useHtmxLink'
import { getPathFromUrl } from '../shared/htmx/utils'
import { EuiText, EuiSpacer, useEuiTheme, useEuiFontSize } from '@elastic/eui'
import { css } from '@emotion/react'

interface Reference {
    url: string
    title: string
    description: string
}

interface ReferencesProps {
    referencesJson: string
}

const parseReferences = (jsonString: string): Reference[] => {
    try {
        // Remove markdown code fences if present
        let cleanedJson = jsonString.trim()

        // Check for ```json or ``` at the start
        if (cleanedJson.startsWith('```json')) {
            cleanedJson = cleanedJson.substring(7) // Remove ```json
        } else if (cleanedJson.startsWith('```')) {
            cleanedJson = cleanedJson.substring(3) // Remove ```
        }

        // Remove closing ``` at the end
        if (cleanedJson.endsWith('```')) {
            cleanedJson = cleanedJson.substring(0, cleanedJson.length - 3)
        }

        cleanedJson = cleanedJson.trim()

        const parsed = JSON.parse(cleanedJson)
        if (Array.isArray(parsed)) {
            return parsed
        }
        return []
    } catch (e) {
        console.warn('Failed to parse references JSON:', e)
        return []
    }
}

export const References = ({ referencesJson }: ReferencesProps) => {
    const references = parseReferences(referencesJson)
    const { euiTheme } = useEuiTheme()
    const smallFontsize = useEuiFontSize('xs')

    if (references.length === 0) {
        return null
    }

    return (
        <>
            <EuiSpacer size="s" />
            <ul>
                {references.map((ref, index) => (
                    <ReferenceItem
                        key={index}
                        reference={ref}
                        euiTheme={euiTheme}
                        smallFontsize={smallFontsize}
                    />
                ))}
            </ul>
            {/* <EuiPanel
                hasShadow={false}
                paddingSize="m"
                grow={false}
                css={css`
                    background-color: ${euiTheme.colors.backgroundBaseSubdued};
                `}
            >
                <div>
                    <EuiSpacer size="s" />
                    <EuiFlexGroup direction="column" gutterSize="m">
                        {references.map((ref, index) => (
                            <EuiFlexItem key={index}>
                                <EuiFlexGroup
                                    gutterSize="xs"
                                    alignItems="flexStart"
                                    responsive={false}
                                >
                                    <EuiFlexItem grow={false}>
                                        <EuiIcon
                                            type="document"
                                            size="s"
                                            css={css`
                                                margin-top: 6px;
                                            `}
                                        />
                                    </EuiFlexItem>
                                    <EuiFlexItem>
                                        <div>
                                            <EuiLink
                                                href={ref.url}
                                                target="_blank"
                                                css={css`
                                                    font-size: 12px;
                                                    font-weight: 600;
                                                `}
                                            >
                                                {ref.title}
                                            </EuiLink>
                                            <EuiText
                                                size="xs"
                                                color="subdued"
                                                css={css`
                                                    margin-top: 2px;
                                                `}
                                            >
                                                {ref.description}
                                            </EuiText>
                                        </div>
                                    </EuiFlexItem>
                                </EuiFlexGroup>
                            </EuiFlexItem>
                        ))}
                    </EuiFlexGroup>
                </div>
            </EuiPanel> */}
        </>
    )
}

interface ReferenceItemProps {
    reference: Reference
    euiTheme: ReturnType<typeof useEuiTheme>['euiTheme']
    smallFontsize: ReturnType<typeof useEuiFontSize>
}

const ReferenceItem = ({
    reference,
    euiTheme,
    smallFontsize,
}: ReferenceItemProps) => {
    // Extract path from URL, falling back to original URL if extraction fails
    const path = getPathFromUrl(reference.url) ?? reference.url
    const anchorRef = useHtmxLink(path)

    return (
        <li>
            <a
                ref={anchorRef}
                href={path}
                css={css`
                    width: 100%;
                    border: 1px solid ${euiTheme.border.color};
                    padding: ${euiTheme.size.s};
                    border-radius: ${euiTheme.border.radius.small};
                    display: inline-block;
                    margin-top: ${euiTheme.size.s};
                    text-decoration: none;
                    &:hover {
                        background-color: ${euiTheme.colors
                            .backgroundBaseSubdued};
                    }
                    &:hover .reference-title {
                        text-decoration: underline;
                    }
                `}
            >
                <div
                    className="reference-title"
                    css={css`
                        ${smallFontsize};
                        color: ${euiTheme.colors.link};
                        margin-bottom: ${euiTheme.size.xxs};
                    `}
                >
                    {reference.title}
                </div>
                <EuiText size="xs" color="subdued">
                    {reference.description}
                </EuiText>
            </a>
        </li>
    )
}
