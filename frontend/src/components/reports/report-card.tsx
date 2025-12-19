"use client"

import { Link } from '@tanstack/react-router'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { formatCurrency, formatDate, getReportStatusVariant } from '@/lib/utils'
import type { ReportSummary } from '@/types/api'
import {
  FileText,
  Calendar,
  DollarSign,
  Eye,
  Download,
} from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

interface ReportCardProps {
  report: ReportSummary
  onExport?: (format: 'pdf' | 'excel') => void
}

export function ReportCard({ report, onExport }: ReportCardProps) {
  const statusVariant = getReportStatusVariant(report.status)

  return (
    <Card className="transition-all hover:shadow-md">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-muted-foreground" />
            <CardTitle className="text-lg">{report.title}</CardTitle>
          </div>
          <Badge variant={statusVariant}>{report.status}</Badge>
        </div>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-3 gap-4 mb-4">
          <div className="space-y-1">
            <p className="text-xs text-muted-foreground flex items-center gap-1">
              <Calendar className="h-3 w-3" /> Period
            </p>
            <p className="text-sm font-medium">{report.period}</p>
          </div>
          <div className="space-y-1">
            <p className="text-xs text-muted-foreground flex items-center gap-1">
              <DollarSign className="h-3 w-3" /> Total
            </p>
            <p className="text-sm font-medium">{formatCurrency(report.totalAmount)}</p>
          </div>
          <div className="space-y-1">
            <p className="text-xs text-muted-foreground">Items</p>
            <p className="text-sm font-medium">{report.lineCount} expenses</p>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <p className="text-xs text-muted-foreground">
            Created {formatDate(report.createdAt)}
          </p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to="/reports/$reportId" params={{ reportId: report.id }}>
                <Eye className="mr-2 h-4 w-4" />
                View
              </Link>
            </Button>
            {report.status !== 'Draft' && onExport && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm">
                    <Download className="mr-2 h-4 w-4" />
                    Export
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent>
                  <DropdownMenuItem onClick={() => onExport('pdf')}>
                    Export as PDF
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => onExport('excel')}>
                    Export as Excel
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
