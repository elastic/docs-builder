import {
    buildResult,
    evalWhen,
    follow,
    replayRemembered,
    resolve,
    splitInlineCode,
    startEdit,
    type WizardConfig,
} from './engine'

const doc = (title: string) => ({ title, href: `https://example.com/${title}` })

// A deployment-style flow: two variables, a route, scoring, a multi-select and a fixed (answered) variable.
const config: WizardConfig = {
    meta: { title: 'Test', start: 'where' },
    screens: {
        where: {
            type: 'question',
            progress: 10,
            question: 'Where?',
            options: [
                { label: 'Cloud', set: { family: 'cloud' }, next: 'goal' },
                {
                    label: 'On-prem',
                    set: { family: 'self', deployment: 'self_download' },
                    next: 'goal',
                },
            ],
        },
        goal: {
            type: 'question',
            progress: 30,
            question: 'Goal?',
            options: [
                {
                    label: 'Search',
                    set: { solution: 'elasticsearch' },
                    next: 'after_goal',
                },
                {
                    label: 'Security',
                    set: { solution: 'security' },
                    next: 'after_goal',
                },
            ],
        },
        after_goal: {
            type: 'route',
            progress: 0,
            rules: [{ when: { key: 'family', equals: 'self' }, next: 'done' }],
            else: 'control',
        },
        control: {
            type: 'question',
            progress: 60,
            question: 'Hands-off?',
            options: [
                { label: 'Yes', add: { serverless: 2 }, next: 'reqs' },
                { label: 'No', add: { hosted: 2 }, next: 'reqs' },
                { label: 'Unsure', add: { serverless: 1 }, next: 'reqs' },
            ],
        },
        reqs: {
            type: 'question',
            progress: 80,
            question: 'Requirements?',
            multi: true,
            next: 'done',
            options: [
                { label: 'Region', add: { hosted: 1 }, flag: 'check_region' },
                { label: 'Marketplace', flag: 'marketplace' },
                { label: 'Nothing', exclusive: true },
            ],
        },
        done: { type: 'result', progress: 100 },
    },
    results: {
        heading: '{solution} on {deployment}',
        variables: {
            solution: {
                answer: 'solution',
                items: {
                    elasticsearch: {
                        name: 'Elasticsearch',
                        blurb: 'Search.',
                        docs: doc('es'),
                    },
                    security: { name: 'Security', docs: doc('sec') },
                },
            },
            deployment: {
                answer: 'deployment',
                score: ['serverless', 'hosted'],
                items: {
                    serverless: {
                        name: 'Serverless',
                        blurb: 'Hands-off.',
                        docs: doc('serverless'),
                    },
                    hosted: { name: 'Hosted', docs: doc('hosted') },
                    self_download: { name: 'self-managed', docs: doc('self') },
                },
            },
        },
        cta: [
            {
                when: { key: 'deployment', starts_with: 'self' },
                label: 'Read the guide',
                href_from: 'deployment',
            },
            {
                when: { key: 'deployment', equals: 'hosted' },
                label: 'Create a deployment',
                href: 'https://example.com/deploy',
            },
            {
                label: 'Create your {solution} project',
                href: 'https://example.com/create',
            },
        ],
        extra_docs: [
            {
                when: { key: 'solution', equals: 'security' },
                title: 'Migrate',
                href: 'https://example.com/migrate',
            },
        ],
        flag_notes: {
            check_region: 'Check region.',
            marketplace: 'Marketplace works.',
        },
        close_call_note: 'Also consider {other}.',
    },
}

const cloud = (control: number, reqs: number[], goal = 0) =>
    resolve(config, [
        { id: 'where', picks: [0] },
        { id: 'goal', picks: [goal] },
        { id: 'control', picks: [control] },
        { id: 'reqs', picks: reqs },
    ])

describe('evalWhen', () => {
    it('supports equals, starts_with and in', () => {
        const answers = { deployment: 'self_eck' }
        expect(
            evalWhen({ key: 'deployment', equals: 'self_eck' }, answers)
        ).toBe(true)
        expect(
            evalWhen({ key: 'deployment', starts_with: 'self' }, answers)
        ).toBe(true)
        expect(
            evalWhen({ key: 'deployment', in: ['a', 'self_eck'] }, answers)
        ).toBe(true)
        expect(evalWhen({ key: 'deployment', equals: 'x' }, answers)).toBe(
            false
        )
        expect(evalWhen({ key: 'missing', in: ['a'] }, answers)).toBe(false)
    })
})

describe('resolve', () => {
    it('starts on the first screen', () => {
        expect(resolve(config, []).current).toBe('where')
    })

    it('skips cloud-only questions on self-managed paths', () => {
        const state = resolve(config, [
            { id: 'where', picks: [1] },
            { id: 'goal', picks: [0] },
        ])
        expect(state.current).toBe('done')
        expect(state.answers.deployment).toBe('self_download')
    })

    it('records a recap with the chosen labels', () => {
        const state = resolve(config, [{ id: 'where', picks: [0] }])
        expect(state.recap).toEqual([
            { index: 0, id: 'where', question: 'Where?', labels: ['Cloud'] },
        ])
    })

    it('throws on a bad option index', () => {
        expect(() => resolve(config, [{ id: 'where', picks: [9] }])).toThrow(
            'Bad option index'
        )
    })

    it('detects route loops', () => {
        const loop: WizardConfig = {
            ...config,
            screens: {
                a: { type: 'route', progress: 0, rules: [], else: 'a' },
            },
        }
        expect(() => follow(loop, 'a', {})).toThrow('Route loop')
    })
})

describe('buildResult', () => {
    it('scores the deployment: hands-off wins Serverless', () => {
        const result = buildResult(config, cloud(0, [2]))
        expect(result.values).toEqual({
            solution: 'elasticsearch',
            deployment: 'serverless',
        })
        expect(result.heading).toBe('Elasticsearch on Serverless')
        expect(result.cta).toEqual({
            label: 'Create your Elasticsearch project',
            href: 'https://example.com/create',
        })
    })

    it('scores the deployment: wanting control wins Hosted and picks the matching CTA', () => {
        const result = buildResult(config, cloud(1, [2]))
        expect(result.values.deployment).toBe('hosted')
        expect(result.cta).toEqual({
            label: 'Create a deployment',
            href: 'https://example.com/deploy',
        })
    })

    it('breaks ties toward the first scored candidate', () => {
        // Unsure (serverless 1) + Region (hosted 1)
        expect(buildResult(config, cloud(2, [0])).values.deployment).toBe(
            'serverless'
        )
    })

    it('adds flag notes and a close-call note naming the runner-up', () => {
        const notes = buildResult(config, cloud(2, [0, 1])).notes
        expect(notes).toContain('Check region.')
        expect(notes).toContain('Marketplace works.')
        expect(notes).toContain('Also consider Hosted.')
    })

    it('has no close-call note when the margin is wide', () => {
        expect(buildResult(config, cloud(0, [2])).notes).toEqual([])
    })

    it('lets an answer win over scoring and uses href_from for the CTA', () => {
        const state = resolve(config, [
            { id: 'where', picks: [1] },
            { id: 'goal', picks: [0] },
        ])
        const result = buildResult(config, state)
        expect(result.values.deployment).toBe('self_download')
        expect(result.cta).toEqual({
            label: 'Read the guide',
            href: 'https://example.com/self',
        })
        expect(result.notes).toEqual([])
    })

    it('links docs for each variable plus the first matching extra card', () => {
        expect(
            buildResult(config, cloud(0, [2], 1)).docs.map((d) => d.title)
        ).toEqual(['sec', 'serverless', 'Migrate'])
    })

    it('throws when a variable cannot be resolved', () => {
        const state = resolve(config, [{ id: 'where', picks: [0] }])
        expect(() => buildResult(config, state)).toThrow(
            'No result for solution'
        )
    })
})

describe('editing an answer keeps the later ones', () => {
    const full = [
        { id: 'where', picks: [0] },
        { id: 'goal', picks: [0] },
        { id: 'control', picks: [0] },
        { id: 'reqs', picks: [2] },
    ]

    it('startEdit keeps earlier answers and remembers the rest', () => {
        const split = startEdit(full, 1)
        expect(split.history).toEqual([full[0]])
        expect(split.remembered).toEqual({ goal: [0], control: [0], reqs: [2] })
    })

    it('re-applies remembered answers after a change, landing back on the result', () => {
        const { history, remembered } = startEdit(full, 1)
        // the reader switches Search to Security; the component drops the answer they just gave
        delete remembered.goal
        const replayed = replayRemembered(
            config,
            [...history, { id: 'goal', picks: [1] }],
            remembered
        )
        expect(replayed.history.map((e) => e.id)).toEqual([
            'where',
            'goal',
            'control',
            'reqs',
        ])
        expect(resolve(config, replayed.history).current).toBe('done')
        expect(
            buildResult(config, resolve(config, replayed.history)).values
                .solution
        ).toBe('security')
        expect(replayed.remembered).toEqual({})
    })

    it('stops at the first question that has no remembered answer', () => {
        // Editing the first answer from Cloud to On-prem removes the cloud-only questions.
        const { history, remembered } = startEdit(full, 0)
        delete remembered.where
        const replayed = replayRemembered(
            config,
            [{ id: 'where', picks: [1] }],
            remembered
        )
        expect(history).toEqual([])
        expect(replayed.history.map((e) => e.id)).toEqual(['where', 'goal'])
        expect(resolve(config, replayed.history).current).toBe('done')
        // The cloud-only answers are not needed on this path, so they are simply left over.
        expect(Object.keys(replayed.remembered).sort()).toEqual([
            'control',
            'reqs',
        ])
    })

    it('asks only the genuinely new question when the path changes', () => {
        const short = [
            { id: 'where', picks: [1] },
            { id: 'goal', picks: [0] },
        ]
        const { remembered } = startEdit(short, 0)
        // Switching On-prem to Cloud adds the control question, which has no remembered answer.
        const replayed = replayRemembered(
            config,
            [{ id: 'where', picks: [0] }],
            remembered
        )
        expect(replayed.history.map((e) => e.id)).toEqual(['where', 'goal'])
        expect(resolve(config, replayed.history).current).toBe('control')
    })

    it('ignores remembered picks that no longer fit the screen', () => {
        const replayed = replayRemembered(
            config,
            [{ id: 'where', picks: [0] }],
            { goal: [7] }
        )
        expect(replayed.history).toHaveLength(1)
        expect(resolve(config, replayed.history).current).toBe('goal')
    })
})

describe('generic: a wizard that has nothing to do with deployments', () => {
    const contentWizard: WizardConfig = {
        meta: { title: 'Which page type do you need?', start: 'purpose' },
        screens: {
            purpose: {
                type: 'question',
                progress: 50,
                question: 'What should the reader do?',
                options: [
                    {
                        label: 'Learn a concept',
                        set: { type: 'overview' },
                        next: 'done',
                    },
                    {
                        label: 'Complete a task',
                        set: { type: 'how-to' },
                        next: 'done',
                    },
                ],
            },
            done: { type: 'result', progress: 100 },
        },
        results: {
            heading: 'Write a {type}',
            variables: {
                type: {
                    answer: 'type',
                    items: {
                        overview: { name: 'overview', docs: doc('overview') },
                        'how-to': { name: 'how-to guide', docs: doc('how-to') },
                    },
                },
            },
            cta: [{ label: 'Open the {type} template', href_from: 'type' }],
        },
    }

    it('works with one variable, no scoring and a single fallback CTA', () => {
        const state = resolve(contentWizard, [{ id: 'purpose', picks: [1] }])
        const result = buildResult(contentWizard, state)
        expect(result.heading).toBe('Write a how-to guide')
        expect(result.cta).toEqual({
            label: 'Open the how-to guide template',
            href: 'https://example.com/how-to',
        })
        expect(result.docs).toEqual([doc('how-to')])
    })
})

describe('optional and conditional variables', () => {
    // `tier` is only set on one path and only applies on Serverless.
    const withTier: WizardConfig = {
        ...config,
        screens: {
            ...config.screens,
            where: {
                ...config.screens.where,
                options: [
                    ...(config.screens.where.options ?? []),
                    {
                        label: 'Tiered',
                        set: { family: 'cloud', tier: 'gold' },
                        next: 'goal',
                    },
                ],
            },
        },
        results: {
            ...config.results,
            variables: {
                ...config.results.variables,
                tier: {
                    answer: 'tier',
                    optional: true,
                    when: { key: 'deployment', equals: 'serverless' },
                    items: {
                        gold: {
                            name: 'Gold',
                            blurb: 'Gold tier.',
                            docs: doc('tier'),
                        },
                    },
                },
            },
        },
    }
    const via = (where: number, control: number) =>
        buildResult(
            withTier,
            resolve(withTier, [
                { id: 'where', picks: [where] },
                { id: 'goal', picks: [0] },
                { id: 'control', picks: [control] },
                { id: 'reqs', picks: [2] },
            ])
        )

    it('resolves the variable when its condition holds and an answer is set', () => {
        const result = via(2, 0) // Tiered + hands-off => Serverless
        expect(result.values.tier).toBe('gold')
        expect(result.docs.map((d) => d.title)).toContain('tier')
        expect(result.blurbs).toContain('Gold tier.')
    })

    it('skips the variable when its condition does not hold', () => {
        const result = via(2, 1) // Tiered + wants control => Hosted
        expect(result.values.deployment).toBe('hosted')
        expect(result.values.tier).toBeUndefined()
    })

    it('skips an optional variable that has nothing to show', () => {
        const result = via(0, 0) // Cloud, no tier answer
        expect(result.values.tier).toBeUndefined()
    })

    it('still fails for a required variable that has nothing to show', () => {
        const required: WizardConfig = {
            ...withTier,
            results: {
                ...withTier.results,
                variables: {
                    ...withTier.results.variables,
                    tier: {
                        ...withTier.results.variables.tier,
                        optional: false,
                        when: undefined,
                    },
                },
            },
        }
        const state = resolve(required, [
            { id: 'where', picks: [0] },
            { id: 'goal', picks: [0] },
            { id: 'control', picks: [0] },
            { id: 'reqs', picks: [2] },
        ])
        expect(() => buildResult(required, state)).toThrow('No result for tier')
    })
})

describe('splitInlineCode', () => {
    it('marks backticked parts as code and keeps the rest as text', () => {
        expect(splitInlineCode('Use a `semantic_text` field.')).toEqual([
            { text: 'Use a ', code: false },
            { text: 'semantic_text', code: true },
            { text: ' field.', code: false },
        ])
    })

    it('leaves an unmatched backtick alone', () => {
        expect(splitInlineCode('a ` b')).toEqual([
            { text: 'a ` b', code: false },
        ])
    })

    it('handles several spans and empty text', () => {
        expect(
            splitInlineCode('`a` and `b`').filter((p) => p.code)
        ).toHaveLength(2)
        expect(splitInlineCode('')).toEqual([])
    })
})
