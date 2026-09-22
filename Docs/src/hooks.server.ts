import { sequence } from '@sveltejs/kit/hooks'
import { createLocaleHandle } from '@sveltepress/vite/hooks'
import { paraglideMiddleware } from './lib/paraglide/server'
// side effect: registers the "custom-sveltepress" strategy (URL prefix → locale)
import './lib/locale-strategy'
import { locales } from './locales'

const paraglideHandle = ({ event, resolve }) =>
  paraglideMiddleware(event.request, ({ request, locale }) => {
    event.request = request
    return resolve(event, {
      transformPageChunk: ({ html }) =>
        html.replace('%paraglide.lang%', locale),
    })
  })

export const handle = sequence(paraglideHandle, createLocaleHandle(locales))
