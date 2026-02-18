// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-nocheck: EUI icons do not have types
import { icon as EuiIconVisualizeApp } from '@elastic/eui/es/components/icon/assets/app_visualize'
import { icon as EuiIconArrowDown } from '@elastic/eui/es/components/icon/assets/arrow_down'
import { icon as EuiIconArrowLeft } from '@elastic/eui/es/components/icon/assets/arrow_left'
import { icon as EuiIconArrowRight } from '@elastic/eui/es/components/icon/assets/arrow_right'
import { icon as EuiIconArrowUp } from '@elastic/eui/es/components/icon/assets/arrow_up'
import { icon as EuiIconBranch } from '@elastic/eui/es/components/icon/assets/branch'
import { icon as EuiIconCheck } from '@elastic/eui/es/components/icon/assets/check'
import { icon as EuiIconCheckCircle } from '@elastic/eui/es/components/icon/assets/check_circle'
import { icon as EuiIconChevronSingleDown } from '@elastic/eui/es/components/icon/assets/chevron_single_down'
import { icon as EuiIconChevronSingleUp } from '@elastic/eui/es/components/icon/assets/chevron_single_up'
import { icon as EuiIconCode } from '@elastic/eui/es/components/icon/assets/code'
import { icon as EuiIconComment } from '@elastic/eui/es/components/icon/assets/comment'
import { icon as EuiIconConsole } from '@elastic/eui/es/components/icon/assets/console'
import { icon as EuiIconCopy } from '@elastic/eui/es/components/icon/assets/copy'
import { icon as EuiIconCopyClipboard } from '@elastic/eui/es/components/icon/assets/copy_clipboard'
import { icon as EuiIconCross } from '@elastic/eui/es/components/icon/assets/cross'
import { icon as EuiIconDocument } from '@elastic/eui/es/components/icon/assets/document'
import { icon as EuiIconDocumentation } from '@elastic/eui/es/components/icon/assets/documentation'
import { icon as EuiIconDot } from '@elastic/eui/es/components/icon/assets/dot'
import { icon as EuiIconDotInCircle } from '@elastic/eui/es/components/icon/assets/dot_in_circle'
import { icon as EuiIconRedo } from '@elastic/eui/es/components/icon/assets/editor_redo'
import { icon as EuiIconEmpty } from '@elastic/eui/es/components/icon/assets/empty'
import { icon as EuiIconError } from '@elastic/eui/es/components/icon/assets/error'
import { icon as EuiIconFaceHappy } from '@elastic/eui/es/components/icon/assets/face_happy'
import { icon as EuiIconFaceSad } from '@elastic/eui/es/components/icon/assets/face_sad'
import { icon as EuiIconFilter } from '@elastic/eui/es/components/icon/assets/filter'
import { icon as EuiIconGlobe } from '@elastic/eui/es/components/icon/assets/globe'
import { icon as EuiIconInfo } from '@elastic/eui/es/components/icon/assets/info'
import { icon as EuiIconKqlFunction } from '@elastic/eui/es/components/icon/assets/kql_function'
import { icon as EuiIconLogoElastic } from '@elastic/eui/es/components/icon/assets/logo_elastic'
import { icon as EuiIconMoon } from '@elastic/eui/es/components/icon/assets/moon'
import { icon as EuiIconNewChat } from '@elastic/eui/es/components/icon/assets/new_chat'
import { icon as EuiIconPlay } from '@elastic/eui/es/components/icon/assets/play'
import { icon as EuiIconPopout } from '@elastic/eui/es/components/icon/assets/popout'
import { icon as EuiIconRefresh } from '@elastic/eui/es/components/icon/assets/refresh'
import { icon as EuiIconReturnKey } from '@elastic/eui/es/components/icon/assets/return_key'
import { icon as EuiIconSearch } from '@elastic/eui/es/components/icon/assets/search'
import { icon as EuiIconSortDown } from '@elastic/eui/es/components/icon/assets/sort_down'
import { icon as EuiIconSortUp } from '@elastic/eui/es/components/icon/assets/sort_up'
import { icon as EuiIconSparkles } from '@elastic/eui/es/components/icon/assets/sparkles'
import { icon as EuiIconStop } from '@elastic/eui/es/components/icon/assets/stop'
import { icon as EuiIconSun } from '@elastic/eui/es/components/icon/assets/sun'
import { icon as EuiIconThumbDown } from '@elastic/eui/es/components/icon/assets/thumb_down'
import { icon as EuiIconThumbUp } from '@elastic/eui/es/components/icon/assets/thumb_up'
import { icon as EuiIconTrash } from '@elastic/eui/es/components/icon/assets/trash'
import { icon as EuiIconUser } from '@elastic/eui/es/components/icon/assets/user'
import { icon as EuiIconWarning } from '@elastic/eui/es/components/icon/assets/warning'
import { icon as EuiIconWrench } from '@elastic/eui/es/components/icon/assets/wrench'
import { appendIconComponentCache } from '@elastic/eui/es/components/icon/icon'

const iconMapping = {
    newChat: EuiIconNewChat,
    arrowUp: EuiIconArrowUp,
    arrowDown: EuiIconArrowDown,
    arrowLeft: EuiIconArrowLeft,
    arrowRight: EuiIconArrowRight,
    code: EuiIconCode,
    document: EuiIconDocument,
    documentation: EuiIconDocumentation,
    dot: EuiIconDot,
    empty: EuiIconEmpty,
    search: EuiIconSearch,
    trash: EuiIconTrash,
    user: EuiIconUser,
    wrench: EuiIconWrench,
    visualizeApp: EuiIconVisualizeApp,
    checkCircle: EuiIconCheckCircle,
    check: EuiIconCheck,
    sparkles: EuiIconSparkles,
    cross: EuiIconCross,
    copyClipboard: EuiIconCopyClipboard,
    faceHappy: EuiIconFaceHappy,
    faceSad: EuiIconFaceSad,
    refresh: EuiIconRefresh,
    error: EuiIconError,
    thumbUp: EuiIconThumbUp,
    thumbDown: EuiIconThumbDown,
    popout: EuiIconPopout,
    returnKey: EuiIconReturnKey,
    logoElastic: EuiIconLogoElastic,
    copy: EuiIconCopy,
    play: EuiIconPlay,
    sortUp: EuiIconSortUp,
    sortDown: EuiIconSortDown,
    stop: EuiIconStop,
    comment: EuiIconComment,
    kqlFunction: EuiIconKqlFunction,
    globe: EuiIconGlobe,
    console: EuiIconConsole,
    editorRedo: EuiIconRedo,
    info: EuiIconInfo,
    warning: EuiIconWarning,
    error: EuiIconError,
    chevronSingleDown: EuiIconChevronSingleDown,
    chevronSingleUp: EuiIconChevronSingleUp,
    dotInCircle: EuiIconDotInCircle,
    branch: EuiIconBranch,
    filter: EuiIconFilter,
    moon: EuiIconMoon,
    sun: EuiIconSun,
}

appendIconComponentCache(iconMapping)

export const availableIcons = Object.keys(iconMapping)
