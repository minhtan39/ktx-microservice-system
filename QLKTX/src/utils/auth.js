export const normalizeRole = (role) => {
  const normalized = String(role || '').trim().toLowerCase()

  if (normalized === 'admin' || normalized === 'quantri') return 'Admin'
  if (normalized === 'student' || normalized === 'sinhvien') return 'Student'
  if (normalized === 'staff' || normalized === 'nhanvien') return 'Staff'
  return ''
}

export const getPermissions = () => {
  try {
    const permissions = JSON.parse(localStorage.getItem('user_permissions') || '[]')
    return Array.isArray(permissions) ? permissions : []
  } catch {
    return []
  }
}

export const hasPermission = (permission, role = localStorage.getItem('user_role')) => {
  const normalizedRole = normalizeRole(role)

  if (!normalizedRole) return false
  return normalizedRole === 'Admin' || getPermissions().includes(permission)
}

export const defaultHomeForRole = (role = localStorage.getItem('user_role')) => {
  const normalizedRole = normalizeRole(role)
  if (normalizedRole === 'Student') return '/student/portal'
  if (normalizedRole === 'Staff') return '/employee/dashboard'
  if (normalizedRole === 'Admin') return '/student-service/dashboard'
  return '/login'
}
