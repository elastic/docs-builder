import { icon as EuiIconVisualizeApp } from '@elastic/eui/es/components/icon/assets/app_visualize'
import { icon as EuiIconArrowDown } from '@elastic/eui/es/components/icon/assets/arrow_down'
import { icon as EuiIconArrowLeft } from '@elastic/eui/es/components/icon/assets/arrow_left'
import { icon as EuiIconArrowRight } from '@elastic/eui/es/components/icon/assets/arrow_right'
import { icon as EuiIconCheck } from '@elastic/eui/es/components/icon/assets/check'
import { icon as EuiIconCopyClipboard } from '@elastic/eui/es/components/icon/assets/copy_clipboard'
import { icon as EuiIconCross } from '@elastic/eui/es/components/icon/assets/cross'
import { icon as EuiIconDocument } from '@elastic/eui/es/components/icon/assets/document'
import { icon as EuiIconError } from '@elastic/eui/es/components/icon/assets/error'
import { icon as EuiIconFaceHappy } from '@elastic/eui/es/components/icon/assets/face_happy'
import { icon as EuiIconFaceSad } from '@elastic/eui/es/components/icon/assets/face_sad'
import { icon as EuiIconNewChat } from '@elastic/eui/es/components/icon/assets/new_chat'
import { icon as EuiIconRefresh } from '@elastic/eui/es/components/icon/assets/refresh'
import { icon as EuiIconSearch } from '@elastic/eui/es/components/icon/assets/search'
import { icon as EuiIconSparkles } from '@elastic/eui/es/components/icon/assets/sparkles'
import { icon as EuiIconTrash } from '@elastic/eui/es/components/icon/assets/trash'
import { icon as EuiIconUser } from '@elastic/eui/es/components/icon/assets/user'
import { icon as EuiIconWrench } from '@elastic/eui/es/components/icon/assets/wrench'
import { appendIconComponentCache } from '@elastic/eui/es/components/icon/icon'

appendIconComponentCache({
    newChat: EuiIconNewChat,
    arrowDown: EuiIconArrowDown,
    arrowLeft: EuiIconArrowLeft,
    arrowRight: EuiIconArrowRight,
    document: EuiIconDocument,
    search: EuiIconSearch,
    trash: EuiIconTrash,
    user: EuiIconUser,
    wrench: EuiIconWrench,
    visualizeApp: EuiIconVisualizeApp,
    check: EuiIconCheck,
    sparkles: EuiIconSparkles,
    cross: EuiIconCross,
    copyClipboard: EuiIconCopyClipboard,
    faceHappy: EuiIconFaceHappy,
    faceSad: EuiIconFaceSad,
    refresh: EuiIconRefresh,
    error: EuiIconError,
})
