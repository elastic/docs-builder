import githubSvg from './GitHub_Invertocat_Black.svg'
import commitSvg from './commit.svg'
import pullRequestSvg from './pull-request.svg'
import {
    EuiIcon,
    EuiHeaderSectionItem,
    EuiPopover,
    EuiPopoverTitle,
    EuiSpacer,
    EuiText,
    useEuiTheme,
    useGeneratedHtmlId,
    IconType,
    EuiThemeComputed,
    EuiButton,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState } from 'react'

interface DeploymentInfoProps {
    gitBranch: string
    gitCommit: string
    githubRepository: string
    githubRef?: string
}

export const DeploymentInfo = ({
    gitBranch,
    gitCommit,
    githubRepository,
    githubRef,
}: DeploymentInfoProps) => {
    const { euiTheme } = useEuiTheme()
    const gitInfoPopoverId = useGeneratedHtmlId({ prefix: 'gitInfo' })
    const links = getDeploymentLinks(
        githubRepository,
        gitBranch,
        gitCommit,
        githubRef
    )

    const [isOpen, setIsOpen] = useState(false)

    const popoverButton = (
        <EuiHeaderSectionItem>
            <EuiButton
                size="s"
                fill
                color="primary"
                // onClickAriaLabel="Show deployment info"
                onClick={() => setIsOpen((prev) => !prev)}
                css={css`
                    margin-inline: ${euiTheme.size.s};
                    font-family: ${euiTheme.font.familyCode};
                `}
            >
                <div
                    css={css`
                        display: flex;
                        gap: ${euiTheme.size.s};
                    `}
                >
                    <span
                        css={css`
                            display: inline-flex;
                            align-items: center;
                            gap: ${euiTheme.size.xs};
                        `}
                    >
                        <EuiIcon type="branch" />
                        {gitBranch}
                    </span>
                    <span
                        css={css`
                            display: inline-flex;
                            align-items: center;
                            gap: ${euiTheme.size.xs};
                        `}
                    >
                        <EuiIcon type={commitSvg} />
                        {gitCommit}
                    </span>
                </div>
            </EuiButton>
        </EuiHeaderSectionItem>
    )

    return (
        <EuiPopover
            id={gitInfoPopoverId}
            ownFocus
            button={popoverButton}
            closePopover={() => setIsOpen(false)}
            panelPaddingSize="none"
            anchorPosition="downRight"
            isOpen={isOpen}
        >
            <EuiPopoverTitle paddingSize="s">
                <EuiText color="text" size="xs">
                    <strong>Deployment info</strong>
                </EuiText>
                <EuiSpacer size="xs" />
                <EuiText color="subdued" size="xs">
                    {getDeploymentSubtitle(githubRef)}
                </EuiText>
            </EuiPopoverTitle>
            <div>
                {githubRef != null && (
                    <DeploymentInfoRow
                        label="PR"
                        value={formatGitHubRef(githubRef)}
                        icon={pullRequestSvg}
                        href={links.ref}
                    />
                )}
                <DeploymentInfoRow
                    label="Branch"
                    value={gitBranch}
                    icon="branch"
                    href={links.branch}
                />
                <DeploymentInfoRow
                    label="Commit"
                    value={gitCommit}
                    icon={commitSvg}
                    href={links.commit}
                />
                <DeploymentInfoRow
                    label="Repository"
                    value={githubRepository}
                    icon={githubSvg}
                    href={links.repository}
                />
            </div>
        </EuiPopover>
    )
}

const rowCss = (euiTheme: EuiThemeComputed) => css`
    display: flex;
    align-items: center;
    gap: ${euiTheme.size.s};
    padding-block: ${euiTheme.size.s};
    padding-inline: ${euiTheme.size.m};
    color: #fff;
    border-bottom: 1px solid ${euiTheme.border.color};
    text-decoration: none;
    &:last-child {
        border-bottom: none;
    }
    &:hover {
        background: ${euiTheme.colors.backgroundBaseHighlighted};
    }
`

const DeploymentInfoRow = ({
    label,
    value,
    icon,
    href,
}: {
    label: string
    value: string
    icon: IconType
    href?: string
}) => {
    const { euiTheme } = useEuiTheme()
    const content = (
        <>
            <EuiIcon type={icon} size="s" color="subdued" />
            <EuiText
                size="xs"
                color="subdued"
                css={css`
                    width: 10ch;
                `}
            >
                {label}
            </EuiText>
            <EuiText
                size="xs"
                css={css`
                    color: ${euiTheme.colors.ink};
                    font-family: ${euiTheme.font.familyCode};
                `}
            >
                {value}
            </EuiText>
        </>
    )
    if (href != null) {
        return (
            <a
                href={href}
                target="_blank"
                rel="noopener noreferrer"
                css={rowCss(euiTheme)}
            >
                {content}
            </a>
        )
    }
    return <div css={rowCss(euiTheme)}>{content}</div>
}

type ParsedRef =
    | { kind: 'pull'; number: string }
    | { kind: 'branch'; name: string }
    | { kind: 'tag'; name: string }
    | { kind: 'unknown'; raw: string }

function parseGitHubRef(ref: string): ParsedRef {
    const pull = ref.match(/^refs\/pull\/(\d+)\/merge$/)
    if (pull) return { kind: 'pull', number: pull[1] }
    if (ref.startsWith('refs/heads/'))
        return { kind: 'branch', name: ref.slice('refs/heads/'.length) }
    if (ref.startsWith('refs/tags/'))
        return { kind: 'tag', name: ref.slice('refs/tags/'.length) }
    return { kind: 'unknown', raw: ref }
}

/** e.g. refs/pull/123/merge -> PR #123 */
function formatGitHubRef(ref: string): string {
    const parsed = parseGitHubRef(ref)
    switch (parsed.kind) {
        case 'pull':
            return `#${parsed.number}`
        case 'branch':
            return parsed.name
        case 'tag':
            return parsed.name
        case 'unknown':
            return parsed.raw
    }
}

function getDeploymentSubtitle(githubRef?: string): string {
    if (githubRef == null) return 'Preview build'
    const parsed = parseGitHubRef(githubRef)
    switch (parsed.kind) {
        case 'pull':
            return `Preview build from pull request #${parsed.number}`
        case 'branch':
            return `Preview build from branch ${parsed.name}`
        case 'tag':
            return `Preview build from tag ${parsed.name}`
        case 'unknown':
            return 'Preview build'
    }
}

const GITHUB_BASE = 'https://github.com'

function getDeploymentLinks(
    githubRepository: string,
    gitBranch: string,
    gitCommit: string,
    githubRef?: string
): { ref?: string; branch: string; commit: string; repository: string } {
    const repo = githubRepository.startsWith('elastic/')
        ? githubRepository
        : `elastic/${githubRepository}`
    const base = `${GITHUB_BASE}/${repo}`
    const pull = githubRef != null ? parseGitHubRef(githubRef) : undefined
    const pullUrl =
        pull?.kind === 'pull' ? `${base}/pull/${pull.number}` : undefined
    return {
        ref: pullUrl,
        branch: `${base}/tree/${gitBranch}`,
        commit: `${base}/commit/${gitCommit}`,
        repository: base,
    }
}
