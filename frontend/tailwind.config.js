module.exports = {
  content: [
    "./index.html",
    "./admin/**/*.html",
    "./pages/**/*.html",
    "./js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        gold: {
          50: '#FFF8E7',
          100: '#FFEDCE',
          200: '#FFE0A3',
          300: '#FFD166',
          400: '#FFC300',
          500: '#FFB400',
          600: '#FFA500',
          700: '#FF8800',
          800: '#FF7700',
          900: '#FF6600'
        },
        copper: {
          50: '#FDF6F0',
          100: '#FBE8DD',
          200: '#F5D3B8',
          300: '#EFBB94',
          400: '#E8A06F',
          500: '#E0874A',
          600: '#D87135',
          700: '#C85B2A',
          800: '#B84A23',
          900: '#A63C1D'
        }
      }
    }
  },
  plugins: []
}
