import { cooldownStore } from '../web-components/shared/cooldown.store'

export const resetCooldownStore = () =>
    cooldownStore.setState({
        cooldowns: {
            search: { cooldown: null, awaitingNewInput: false },
            askAi: { cooldown: null, awaitingNewInput: false },
        },
    })
