import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from './context/AuthContext.tsx'
import dayjs from 'dayjs'
import utc from 'dayjs/plugin/utc'
import timezone from 'dayjs/plugin/timezone'
import 'dayjs/locale/es'
import { getStoredBranchTimeZone } from './utils/branchTimeZone.ts'

dayjs.extend(utc)
dayjs.extend(timezone)
dayjs.tz.setDefault(getStoredBranchTimeZone())
dayjs.locale('es')

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <App />
    </AuthProvider>
  </StrictMode>,
)
