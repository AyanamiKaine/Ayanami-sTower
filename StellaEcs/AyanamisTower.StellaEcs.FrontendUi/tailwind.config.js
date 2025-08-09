/** @type {import('tailwindcss').Config} */
export default {
    content: [
        './src/**/*.{astro,html,js,jsx,ts,tsx,svelte}',
    ],
    theme: {
        extend: {
            colors: {
                brand: {
                    500: '#10b981'
                }
            }
        }
    },
    plugins: [require('@tailwindcss/forms'), require('@tailwindcss/typography')]
};
