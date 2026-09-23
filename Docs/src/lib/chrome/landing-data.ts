// Landing page content model. Single responsibility: translate i18n copy
// + route base into render data. The translator `t` and `base` are injected
// this module never imports paraglide or routing,
// so it stays pure and locale agnostic.

export type Translator = (key: string) => string

export interface HexCard {
  icon: string
  num: string
  title: string
  sub: string
  body: string
  code: string
  link: string
  linkLabel: string
}

export interface FeatureCard {
  t: string
  d: string
  icon: string
}

export interface ThirdNav {
  label: string
  href: string
}

/** Flow hexes for the landing "hive" section. */
export function buildHexes(t: Translator, base: string): HexCard[] {
  return [
    {
      icon: 'M12 2l7 4v6c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V6l7-4z',
      num: t('landing.hex1Num'),
      title: t('landing.hex1Title'),
      sub: t('landing.hex1Sub'),
      body: t('landing.hex1Body'),
      code: 'Authorization: Bearer <keycloak-jwt>',
      link: `${base}/guide/introduction/`,
      linkLabel: t('landing.hex1LinkLabel'),
    },
    {
      icon: 'M8 12a4 4 0 1 0 0.01 0 M12 12h9 M18 12v4 M21 12v3',
      num: t('landing.hex2Num'),
      title: t('landing.hex2Title'),
      sub: t('landing.hex2Sub'),
      body: t('landing.hex2Body'),
      code: 'POST /sdk/developer-tokens\n→ { "jwt": "eyJhbG…", "key": "rk_live_…",\n    "scopes": ["write:packages"] }',
      link: `${base}/reference/rest-api/`,
      linkLabel: t('landing.hex2LinkLabel'),
    },
    {
      icon: 'M4 12l5 5L20 6',
      num: t('landing.hex3Num'),
      title: t('landing.hex3Title'),
      sub: t('landing.hex3Sub'),
      body: t('landing.hex3Body'),
      code: 'X-Developer-Token: <authkit-jwt>\nPOST /sdk/tokens/verify → { "valid": true }',
      link: `${base}/reference/rest-api/`,
      linkLabel: t('landing.hex3LinkLabel'),
    },
  ]
}

/** Feature cards below the hive. */
export function buildFeatures(t: Translator): FeatureCard[] {
  return [
    {
      t: t('landing.feat1T'),
      d: t('landing.feat1D'),
      icon: 'M12 2l7 4v6c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V6l7-4z',
    },
    {
      t: t('landing.feat2T'),
      d: t('landing.feat2D'),
      icon: 'M7 7h10v10H7z M10 2v3 M14 2v3 M10 19v3 M14 19v3 M2 10h3 M2 14h3 M19 10h3 M19 14h3',
    },
    {
      t: t('landing.feat3T'),
      d: t('landing.feat3D'),
      icon: 'M4 6h16 M4 12h16 M4 18h10',
    },
  ]
}

/** Third nav item per locale registry (ADR index or configuration). */
export function buildThirdNav(t: Translator, base: string, navThird: string): ThirdNav {
  return navThird === 'config'
    ? { label: t('landing.navConfig'), href: `${base}/guide/configuration/` }
    : { label: t('landing.navAdrs'), href: `${base}/adr/` }
}
