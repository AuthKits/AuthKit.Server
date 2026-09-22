import { defaultTheme } from '@sveltepress/theme-default'
import { sveltepress } from '@sveltepress/vite'
import tailwindcss from '@tailwindcss/vite'
import { paraglideVitePlugin } from '@inlang/paraglide-js'
import { defineConfig } from 'vite'
import { locales, codeLanguages, enSidebar } from './src/locales'

export default defineConfig({
  plugins: [
    tailwindcss(),
    paraglideVitePlugin({
      project: './project.inlang',
      outdir: './src/lib/paraglide'
    }),
    sveltepress({
      theme: defaultTheme({
        highlighter: { languages: [...codeLanguages] },
        themeColor: {
          primary: '#e4e4e7',
          primaryDeep: '#18181b',
          hover: '#f4f4f5',
        },
        navbar: [
          {
            label: 'AuthKit',
            items: [
              { label: 'Home', link: '/en/' },
              { label: 'Introduction', link: '/en/guide/introduction/' },
            ],
          },
        ],
        sidebar: enSidebar,
        github: 'https://github.com/AuthKits/AuthKit.Server'
      }),
      locales,
      siteConfig: {
        title: 'AuthKit Documentation',
        description: 'Documentation for AuthKit.Server'
      },
      llms: {
        enabled: true
      }
    })
  ]
})