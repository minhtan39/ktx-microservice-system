import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import './style.css'

import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

import '@mdi/font/css/materialdesignicons.css'

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'light',
    themes: {
      light: {
        dark: false,
        colors: {
          primary: '#f36f21',
          secondary: '#243447',
          accent: '#ffb347',
          success: '#16a34a',
          warning: '#f59e0b',
          error: '#dc2626',
          info: '#2563eb',
          surface: '#ffffff',
          background: '#fff8f1',
        },
      },
    },
  },
})

const app = createApp(App)

app.use(router)
app.use(vuetify)

app.mount('#app')
