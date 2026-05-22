import { defineConfig } from 'vitepress'
import { withLikeC4 } from '@leberkas-org/vitepress-likec4'

export default withLikeC4({ likec4: { source: './likec4' } }, defineConfig({
  title: 'TurboModbus',
  description: 'High-performance Modbus TCP client built on Akka.Streams',
  base: '/',
  appearance: 'dark',
  themeConfig: {
    logo: { light: '/logo.svg', dark: '/logo-dark.svg' },
    siteTitle: false,
    nav: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'API', link: '/api' },
      { text: 'NuGet', link: 'https://www.nuget.org/packages/TurboModbus' },
    ],
    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Polling', link: '/polling' },
          { text: 'Streams', link: '/streams' },
        ],
      },
      {
        text: 'Reference',
        items: [
          { text: 'API', link: '/api' },
          { text: 'Architecture', link: '/architecture' },
          { text: 'TurboModbus vs NModbus', link: '/comparison' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/leberkas-org/TurboModbus' },
    ],
    footer: {
      message: 'Released under the Apache 2.0 License.',
    },
  },
}))
