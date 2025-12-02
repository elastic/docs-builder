import AiIcon from './ai-icon.svg'
import {
    EuiButton,
    EuiButtonIcon,
    EuiButtonIconPropsForButton,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { ComponentProps } from 'react'

type ButtonProps = ComponentProps<typeof EuiButton>

// Reusable gradient values
export const aiGradients = {
    light: 'linear-gradient(99deg, #d9e8ff 3.97%, #ece2fe 65.6%)',
    dark: 'linear-gradient(131deg, #1750ba 2.98%, #731dcf 66.24%)',
} as const

const buttonLightCss = css`
    background: ${aiGradients.light};
`

const buttonFillCss = css`
    background: ${aiGradients.dark};

    /* Make the icon white */
    .euiIcon {
        filter: brightness(0) invert(1);
    }
`

const textGradientCss = css`
    background: ${aiGradients.dark};
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
`

const textWhiteCss = css`
    color: white;
`

const buttonIconLightCss = css`
    background: ${aiGradients.light};
`

const buttonIconFillCss = css`
    background: ${aiGradients.dark};

    /* Make the icon white */
    .euiIcon {
        filter: brightness(0) invert(1);
    }
`

export const ElasticAiAssistantButton = ({
    children,
    fill = false,
    ...rest
}: ButtonProps) => {
    return (
        <EuiButton
            css={fill ? buttonFillCss : buttonLightCss}
            size="s"
            fill
            iconType={AiIcon}
            {...rest}
        >
            <span css={fill ? textWhiteCss : textGradientCss}>{children}</span>
        </EuiButton>
    )
}

type AiButtonIconProps = {
    fill?: boolean
} & Partial<Pick<EuiButtonIconPropsForButton, 'iconType'>> &
    Omit<EuiButtonIconPropsForButton, 'iconType'>

export const ElasticAiAssistantButtonIcon = ({
    fill = false,
    iconType = AiIcon,
    'aria-label': ariaLabel = 'AI Assistant',
    ...rest
}: AiButtonIconProps) => {
    return (
        <EuiButtonIcon
            css={fill ? buttonIconFillCss : buttonIconLightCss}
            iconType={iconType}
            aria-label={ariaLabel}
            {...rest}
        />
    )
}
