const placeholderValues = new Set([
  'string',
  'test',
  'demo',
  'null',
  'undefined',
])

export const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.value)) return payload.value
  return []
}

export const isPlaceholderText = (value) => {
  const text = String(value || '').trim().toLowerCase()
  return !text || placeholderValues.has(text)
}

export const isValidStudentRecord = (student) => {
  return Number(student?.id || 0) > 0 &&
    !isPlaceholderText(student?.fullName)
}

export const toStudentOption = (student) => {
  const studentCode = String(student.studentCode || '').trim()

  return {
    ...student,
    displayName: studentCode
      ? `${student.fullName} - ${studentCode}`
      : student.fullName,
  }
}

export const cleanStudents = (payload) => {
  return normalizeList(payload)
    .filter(isValidStudentRecord)
    .map(toStudentOption)
    .sort((first, second) =>
      first.displayName.localeCompare(second.displayName, 'vi'))
}

export const buildStudentNameMap = (students) => {
  return new Map(students.map((student) => [
    student.id,
    student.displayName || student.fullName,
  ]))
}
