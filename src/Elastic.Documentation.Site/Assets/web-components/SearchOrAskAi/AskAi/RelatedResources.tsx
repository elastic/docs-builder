/** @jsxImportSource @emotion/react */
import {
    EuiFlexGroup,
    EuiFlexItem,
    EuiText,
    EuiLink,
    EuiSpacer,
    EuiIcon,
    EuiPanel,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

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
        console.error('Failed to parse references JSON:', e)
        return []
    }
}

export const References = ({ referencesJson }: ReferencesProps) => {
    const references = parseReferences(referencesJson)
    const { euiTheme } = useEuiTheme()

    if (references.length === 0) {
        return null
    }

    return (
        <>
            <EuiSpacer size="m" />
            <EuiPanel
                hasShadow={false}
                paddingSize="m"
                grow={false}
                css={css`
                    background-color: ${euiTheme.colors.backgroundBaseSubdued};
                `}
            >
                <div>
                    <EuiText size="s">
                        <strong>Related resources:</strong>
                    </EuiText>
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
            </EuiPanel>
        </>
    )
}
