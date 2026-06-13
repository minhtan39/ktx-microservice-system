const escapeHtml = (value) => String(value ?? '')
  .replaceAll('&', '&amp;')
  .replaceAll('<', '&lt;')
  .replaceAll('>', '&gt;')
  .replaceAll('"', '&quot;')
  .replaceAll("'", '&#39;')

export const exportRowsToExcel = ({ filename, sheetName = 'Danh sach', columns, rows }) => {
  const safeFilename = filename.endsWith('.xls') ? filename : `${filename}.xls`
  const tableHead = columns
    .map((column) => `<th>${escapeHtml(column.header)}</th>`)
    .join('')
  const tableRows = rows
    .map((row) => `<tr>${columns
      .map((column) => `<td>${escapeHtml(column.value(row))}</td>`)
      .join('')}</tr>`)
    .join('')

  const html = `
    <html xmlns:o="urn:schemas-microsoft-com:office:office"
      xmlns:x="urn:schemas-microsoft-com:office:excel"
      xmlns="http://www.w3.org/TR/REC-html40">
      <head>
        <meta charset="UTF-8" />
        <style>
          table { border-collapse: collapse; font-family: Arial, sans-serif; }
          th, td { border: 1px solid #d9d9d9; padding: 6px 8px; mso-number-format:"\\@"; }
          th { background: #f5f5f5; font-weight: 700; }
        </style>
      </head>
      <body>
        <table>
          <caption>${escapeHtml(sheetName)}</caption>
          <thead><tr>${tableHead}</tr></thead>
          <tbody>${tableRows}</tbody>
        </table>
      </body>
    </html>
  `

  const blob = new Blob([html], {
    type: 'application/vnd.ms-excel;charset=utf-8',
  })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = safeFilename
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
