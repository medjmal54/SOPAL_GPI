import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { ref } from 'vue'

let connection = null
const connected = ref(false)
const requests = ref([])
const connecting = ref(false)

async function getToken() {
  const res = await fetch('/api/Auth/token', { credentials: 'include' })
  if (!res.ok) throw new Error('Failed to fetch auth token')
  const { token } = await res.json()
  return token
}

export function useSignalR() {
  async function startConnection(retries = 3) {
    if (connection && connection.state === 'Connected') return
    if (connecting.value) return

    connecting.value = true
    try {
      const token = await getToken()

      if (!connection) {
        connection = new HubConnectionBuilder()
          .withUrl('/hubs/notifications', {
            accessTokenFactory: async () => {
              try {
                return await getToken()
              } catch {
                connection?.stop()
                return null
              }
            },
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build()

        connection.on('ReceiveNewRequest', (request) => {
          requests.value.unshift(request)
        })

        connection.on('ReceiveRequestUpdate', (update) => {
          const idx = requests.value.findIndex((r) => r.id === update.id)
          if (idx !== -1) requests.value[idx] = { ...requests.value[idx], ...update }
        })

        connection.onreconnected(() => {
          connected.value = true
        })
        connection.onclose(() => {
          connected.value = false
        })
      }

      await connection.start()
      connected.value = true
    } catch (err) {
      console.error('SignalR connection error:', err)
      if (retries > 0) {
        await new Promise((r) => setTimeout(r, 2000))
        return startConnection(retries - 1)
      }
    } finally {
      connecting.value = false
    }
  }

  function stopConnection() {
    if (connection) {
      connection.stop()
      connection = null
      connected.value = false
    }
  }

  return { connection, connected, requests, connecting, startConnection, stopConnection }
}
