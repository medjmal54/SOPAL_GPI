import { apiDownload } from '../services/api'

export function useExport() {
  async function exportData(entity, params, format) {
    const search = new URLSearchParams(params || {})
    search.set('format', format)
    const name = `sopal-gpi-${entity}-${new Date().toISOString().slice(0, 10)}.${format}`
    await apiDownload(`/export/${entity}?${search.toString()}`, name)
  }

  return { exportData }
}
