/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.cshtml",
    "./Pages/*.cshtml",
    "./Views/**/*.cshtml",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      fontFamily: {
        alegreya: ['Alegreya', 'serif'],
      },
      scale: {
        '250': '2.5',
        '300': '3',
      }
    },
  },
}
