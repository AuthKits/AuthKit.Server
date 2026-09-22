import adapter from '@sveltejs/adapter-static';
import type { Config } from '@sveltejs/kit';

const config: Config = {
	extensions: ['.svelte', '.md'],
	kit: {
		adapter: adapter({
			fallback: 'index.html'
		}),
		prerender: {
			entries: ['/', '/en/', '/pl/']
		}
	}
};

export default config;
