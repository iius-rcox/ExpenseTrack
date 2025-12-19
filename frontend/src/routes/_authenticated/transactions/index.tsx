"use client"

import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import { useTransactionList, useImportStatement } from '@/hooks/queries/use-transactions'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { formatCurrency, formatDate } from '@/lib/utils'
import { toast } from 'sonner'
import {
  CreditCard,
  Upload,
  Search,
  ChevronLeft,
  ChevronRight,
  Check,
  X,
  FileSpreadsheet,
  Loader2,
} from 'lucide-react'

const transactionSearchSchema = z.object({
  matched: z.coerce.boolean().optional(),
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(20),
  search: z.string().optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
})

export const Route = createFileRoute('/_authenticated/transactions/')({
  validateSearch: transactionSearchSchema,
  component: TransactionsPage,
})

function TransactionsPage() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [searchInput, setSearchInput] = useState(search.search || '')

  const { data: transactions, isLoading, error } = useTransactionList({
    page: search.page,
    pageSize: search.pageSize,
    matched: search.matched,
    search: search.search,
    startDate: search.startDate,
    endDate: search.endDate,
  })

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    navigate({
      search: {
        ...search,
        search: searchInput || undefined,
        page: 1,
      },
    })
  }

  const handleFilterChange = (matched: boolean | undefined) => {
    navigate({
      search: {
        ...search,
        matched,
        page: 1,
      },
    })
  }

  const handlePageChange = (newPage: number) => {
    navigate({
      search: {
        ...search,
        page: newPage,
      },
    })
  }

  const totalPages = transactions
    ? Math.ceil(transactions.totalCount / search.pageSize)
    : 0

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Transactions</h1>
          <p className="text-muted-foreground">
            View and search your imported transactions
          </p>
        </div>
        <Dialog open={importDialogOpen} onOpenChange={setImportDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Upload className="mr-2 h-4 w-4" />
              Import Statement
            </Button>
          </DialogTrigger>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Import Bank Statement</DialogTitle>
              <DialogDescription>
                Upload a CSV or Excel file from your bank
              </DialogDescription>
            </DialogHeader>
            <StatementImportForm onSuccess={() => setImportDialogOpen(false)} />
          </DialogContent>
        </Dialog>
      </div>

      {/* Search and Filter Bar */}
      <div className="flex flex-col sm:flex-row gap-4">
        <form onSubmit={handleSearch} className="flex-1 flex gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search transactions..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="pl-9"
            />
          </div>
          <Button type="submit" variant="secondary">
            Search
          </Button>
        </form>
        <div className="flex gap-2">
          <Button
            variant={search.matched === undefined ? 'default' : 'outline'}
            size="sm"
            onClick={() => handleFilterChange(undefined)}
          >
            All
            {transactions && (
              <Badge variant="secondary" className="ml-2">
                {transactions.totalCount}
              </Badge>
            )}
          </Button>
          <Button
            variant={search.matched === false ? 'default' : 'outline'}
            size="sm"
            onClick={() => handleFilterChange(false)}
          >
            Unmatched
            {transactions && (
              <Badge variant="secondary" className="ml-2">
                {transactions.unmatchedCount}
              </Badge>
            )}
          </Button>
          <Button
            variant={search.matched === true ? 'default' : 'outline'}
            size="sm"
            onClick={() => handleFilterChange(true)}
          >
            Matched
          </Button>
        </div>
      </div>

      {/* Error State */}
      {error && (
        <Card className="border-destructive">
          <CardContent className="pt-6">
            <p className="text-destructive">Failed to load transactions. Please try again.</p>
          </CardContent>
        </Card>
      )}

      {/* Transaction Table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[100px]">Date</TableHead>
                <TableHead>Description</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead className="w-[100px] text-center">Receipt</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 10 }).map((_, i) => (
                  <TableRow key={i}>
                    <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                    <TableCell><Skeleton className="h-4 w-64" /></TableCell>
                    <TableCell><Skeleton className="h-4 w-16 ml-auto" /></TableCell>
                    <TableCell className="text-center"><Skeleton className="h-5 w-5 mx-auto" /></TableCell>
                  </TableRow>
                ))
              ) : transactions?.transactions.map((txn) => (
                <TableRow
                  key={txn.id}
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() => navigate({
                    to: '/transactions/$transactionId',
                    params: { transactionId: txn.id }
                  })}
                >
                  <TableCell className="font-medium">
                    {formatDate(txn.transactionDate)}
                  </TableCell>
                  <TableCell>{txn.description}</TableCell>
                  <TableCell className="text-right font-medium">
                    {formatCurrency(txn.amount)}
                  </TableCell>
                  <TableCell className="text-center">
                    {txn.hasMatchedReceipt ? (
                      <Check className="h-5 w-5 text-green-500 mx-auto" />
                    ) : (
                      <X className="h-5 w-5 text-muted-foreground/50 mx-auto" />
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Empty State */}
      {!isLoading && transactions?.transactions.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <CreditCard className="h-12 w-12 text-muted-foreground" />
            <h3 className="mt-4 text-lg font-semibold">No transactions found</h3>
            <p className="text-sm text-muted-foreground mt-1">
              {search.search
                ? `No transactions matching "${search.search}"`
                : 'Import a bank statement to get started'}
            </p>
            <Button
              className="mt-4"
              onClick={() => setImportDialogOpen(true)}
            >
              <Upload className="mr-2 h-4 w-4" />
              Import Statement
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Pagination */}
      {transactions && transactions.totalCount > search.pageSize && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Showing {((search.page - 1) * search.pageSize) + 1} to{' '}
            {Math.min(search.page * search.pageSize, transactions.totalCount)} of{' '}
            {transactions.totalCount} transactions
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(search.page - 1)}
              disabled={search.page <= 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="text-sm">
              Page {search.page} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(search.page + 1)}
              disabled={search.page >= totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

interface StatementImportFormProps {
  onSuccess: () => void
}

function StatementImportForm({ onSuccess }: StatementImportFormProps) {
  const [file, setFile] = useState<File | null>(null)
  const [progress, setProgress] = useState(0)
  const { mutate: importStatement, isPending } = useImportStatement()

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      setFile(selectedFile)
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!file) return

    setProgress(0)
    importStatement(
      {
        file,
        onProgress: (p) => setProgress(Math.round(p)),
      },
      {
        onSuccess: (result) => {
          toast.success(
            `Imported ${result.transactionCount} transactions (${result.duplicateCount} duplicates skipped)`
          )
          setFile(null)
          onSuccess()
        },
        onError: (error) => {
          toast.error(`Import failed: ${error.message}`)
        },
      }
    )
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="border-2 border-dashed rounded-lg p-6 text-center">
        <FileSpreadsheet className="mx-auto h-10 w-10 text-muted-foreground" />
        <p className="mt-2 text-sm text-muted-foreground">
          {file ? file.name : 'Select a CSV or Excel file'}
        </p>
        <Input
          type="file"
          accept=".csv,.xlsx,.xls"
          onChange={handleFileChange}
          className="mt-4"
          disabled={isPending}
        />
      </div>
      {isPending && (
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span>Uploading...</span>
            <span>{progress}%</span>
          </div>
          <div className="h-2 bg-muted rounded-full overflow-hidden">
            <div
              className="h-full bg-primary transition-all"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      )}
      <Button
        type="submit"
        className="w-full"
        disabled={!file || isPending}
      >
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Importing...
          </>
        ) : (
          <>
            <Upload className="mr-2 h-4 w-4" />
            Import Statement
          </>
        )}
      </Button>
    </form>
  )
}
