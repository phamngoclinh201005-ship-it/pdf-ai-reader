import type { Config } from 'tailwindcss'
export default <Partial<Config>>{
  content: ['./components/**/*.{vue,js,ts}', './pages/**/*.{vue,js,ts}', './app.vue'],
  theme: { extend: {} },
}
