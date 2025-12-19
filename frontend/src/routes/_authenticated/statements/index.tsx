import { createFileRoute } from '@tanstack/react-router'
import { StatementImportPage } from '@/pages/StatementImportPage'

export const Route = createFileRoute('/_authenticated/statements/')({
  component: StatementsPage,
})

function StatementsPage() {
  return <StatementImportPage />
}
