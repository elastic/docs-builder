declare module '@elastic/highlightjs-esql' {
    import { LanguageFn } from 'highlight.js'
    const esql: LanguageFn
    export default esql
}

declare module '*.svg' {
    import { ComponentType, SVGProps } from 'react'
    const component: ComponentType<SVGProps<SVGSVGElement>>
    export default component
}
