/** @type {import('tailwindcss').Config} */
export default {
  content: ['./src/**/*.{html,js,svelte,ts}', '../svc/*'],
  theme: {
    extend: {
      maxWidth: {
        'custom': '740px', 
      }
    }
  },
  plugins: [],
}

