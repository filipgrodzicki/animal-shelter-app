/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Ciepła beżowa paleta - profesjonalna i elegancka
        primary: {
          50: '#fdfbf7',
          100: '#f9f5eb',
          200: '#f2e8d5',
          300: '#e8d5b5',
          400: '#d4b896',
          500: '#c49a6c',
          600: '#a67c52',
          700: '#8b6344',
          800: '#74503a',
          900: '#614434',
          950: '#352218',
        },
        // Neutralne beżowe tła
        warm: {
          50: '#faf9f7',
          100: '#f5f3f0',
          200: '#ebe7e0',
          300: '#ddd6cc',
          400: '#c9bfb0',
          500: '#b5a594',
          600: '#9f8b78',
          700: '#857264',
          800: '#6e5e54',
          900: '#5c4f47',
          950: '#302925',
        },
        // Akcenty dla statusów
        status: {
          available: '#6b8e6b',
          reserved: '#b8860b',
          adopted: '#2e5a2e',
          quarantine: '#8b4513',
          treatment: '#cd5c5c',
        },
        // Kolory tekstowe z gwarantowanym kontrastem WCAG AA (4.5:1)
        'text-accessible': {
          muted: '#5c5247',      // warm-900 equivalent - 7.5:1 na białym
          secondary: '#6e5e54', // warm-800 equivalent - 5.9:1 na białym
          tertiary: '#857264',  // warm-700 equivalent - 4.6:1 na białym
        },
        // Kolor akcentowy (inspirowany logo WAT)
        accent: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
          950: '#172554',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        heading: ['Poppins', 'system-ui', 'sans-serif'],
      },
      boxShadow: {
        'warm': '0 4px 14px 0 rgba(139, 99, 68, 0.1)',
        'warm-lg': '0 10px 25px 0 rgba(139, 99, 68, 0.15)',
        'accent': '0 4px 14px 0 rgba(37, 99, 235, 0.2)',
        'accent-lg': '0 10px 25px 0 rgba(37, 99, 235, 0.25)',
      },
      animation: {
        'fade-in': 'fadeIn 0.5s ease-out',
        'fade-in-up': 'fadeInUp 0.6s ease-out',
        'fade-in-down': 'fadeInDown 0.6s ease-out',
        'scale-in': 'scaleIn 0.3s ease-out',
        'slide-in-right': 'slideInRight 0.4s ease-out',
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'bounce-slow': 'bounce 2s infinite',
        'float': 'float 3s ease-in-out infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        fadeInUp: {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        fadeInDown: {
          '0%': { opacity: '0', transform: 'translateY(-20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        scaleIn: {
          '0%': { opacity: '0', transform: 'scale(0.95)' },
          '100%': { opacity: '1', transform: 'scale(1)' },
        },
        slideInRight: {
          '0%': { opacity: '0', transform: 'translateX(-20px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        float: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-10px)' },
        },
      },
    },
  },
  plugins: [],
}
