const API_BASE = '/api'

let isRefreshing = false
let refreshPromise = null

async function tryRefresh() {
  if (isRefreshing) return refreshPromise

  isRefreshing = true
  refreshPromise = fetch(`${API_BASE}/Auth/refresh`, {
    method: 'POST',
    credentials: 'include'
  }).finally(() => { isRefreshing = false })

  return refreshPromise
}

function redirectToLogin() {
  localStorage.removeItem('matricule')
  localStorage.removeItem('nom')
  localStorage.removeItem('prenom')
  localStorage.removeItem('role')
  window.location.href = '/login'
}

async function handleResponse(response, options, isRetry) {
  if (response.status === 401 && !isRetry && !options._isRefreshCall) {
    const refreshRes = await tryRefresh()
    if (refreshRes && refreshRes.ok) {
      const retryResponse = await fetch(`${API_BASE}${options._endpoint}`, {
        ...options,
        _endpoint: undefined,
        _isRefreshCall: undefined
      })
      return handleResponse(retryResponse, options, true)
    }
    redirectToLogin()
    throw new Error('Session expirée.')
  }
  if (response.status === 401) {
    redirectToLogin()
    throw new Error('Session expirée.')
  }
  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Erreur HTTP ${response.status}`)
  }
  const contentType = response.headers.get('content-type')
  if (contentType && contentType.includes('application/json')) {
    return response.json()
  }
  return response.text()
}

export async function apiGet(endpoint) {
  const options = {
    credentials: 'include',
    _endpoint: endpoint
  }
  const response = await fetch(`${API_BASE}${endpoint}`, options)
  return handleResponse(response, options, false)
}

export async function apiPost(endpoint, body) {
  const options = {
    method: 'POST',
    credentials: 'include',
    headers: {},
    _endpoint: endpoint
  }
  if (body !== undefined && body !== null) {
    options.headers['Content-Type'] = 'application/json'
    options.body = JSON.stringify(body)
  }
  const response = await fetch(`${API_BASE}${endpoint}`, options)
  return handleResponse(response, options, false)
}

export async function apiPut(endpoint, body) {
  const options = {
    method: 'PUT',
    credentials: 'include',
    headers: {},
    _endpoint: endpoint
  }
  if (body !== undefined && body !== null) {
    options.headers['Content-Type'] = 'application/json'
    options.body = JSON.stringify(body)
  }
  const response = await fetch(`${API_BASE}${endpoint}`, options)
  return handleResponse(response, options, false)
}

export async function apiDelete(endpoint) {
  const options = {
    method: 'DELETE',
    credentials: 'include',
    _endpoint: endpoint
  }
  const response = await fetch(`${API_BASE}${endpoint}`, options)
  return handleResponse(response, options, false)
}

export async function apiDownload(endpoint, fallbackName) {
  const options = {
    credentials: 'include',
    _endpoint: endpoint
  }
  let response = await fetch(`${API_BASE}${endpoint}`, options)

  if (response.status === 401) {
    const refreshRes = await tryRefresh()
    if (refreshRes && refreshRes.ok) {
      response = await fetch(`${API_BASE}${endpoint}`, options)
    } else {
      redirectToLogin()
      throw new Error('Session expirée.')
    }
  }

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Erreur HTTP ${response.status}`)
  }

  const blob = await response.blob()
  const disposition = response.headers.get('content-disposition')
  const match = disposition && disposition.match(/filename="?([^";]+)"?/)
  const filename = (match && match[1]) || fallbackName

  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
