"use client"

import { formatCurrency, formatDate, getStatusVariant } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ImageOff } from 'lucide-react'
import type { ReceiptSummary } from '@/types/api'

interface ReceiptCardProps {
  receipt: ReceiptSummary
}

export function ReceiptCard({ receipt }: ReceiptCardProps) {
  const statusVariant = getStatusVariant(receipt.status)

  return (
    <a href={`/receipts/${receipt.id}`}>
      <Card className="overflow-hidden transition-colors hover:bg-accent/50 cursor-pointer">
        <div className="aspect-[4/3] relative bg-muted">
          {receipt.thumbnailUrl ? (
            <img
              src={receipt.thumbnailUrl}
              alt={receipt.originalFilename}
              className="object-cover w-full h-full"
            />
          ) : (
            <div className="flex items-center justify-center w-full h-full">
              <ImageOff className="h-12 w-12 text-muted-foreground/50" />
            </div>
          )}
          <Badge
            variant={statusVariant}
            className="absolute top-2 right-2"
          >
            {receipt.status}
          </Badge>
        </div>
        <CardContent className="p-3">
          <div className="space-y-1">
            <p className="text-sm font-medium truncate" title={receipt.originalFilename}>
              {receipt.vendor || receipt.originalFilename}
            </p>
            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <span>{receipt.date ? formatDate(receipt.date) : 'No date'}</span>
              <span className="font-medium text-foreground">
                {receipt.amount != null ? formatCurrency(receipt.amount, receipt.currency) : '--'}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>
    </a>
  )
}

export function ReceiptCardSkeleton() {
  return (
    <Card className="overflow-hidden">
      <Skeleton className="aspect-[4/3]" />
      <CardContent className="p-3">
        <div className="space-y-2">
          <Skeleton className="h-4 w-3/4" />
          <div className="flex items-center justify-between">
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-3 w-12" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
