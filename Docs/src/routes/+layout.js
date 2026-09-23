// Docs (.md) rely on the SveltePress theme, whose components are not
// SSR-safe — keep them client-rendered (SPA fallback), as before.
export const prerender = false
