import { Badge } from '@/components/ui/badge'
import { Split } from 'lucide-react'
import { cn } from '@/lib/utils'

interface SplitIndicatorBadgeProps {
  count: number
  isExpanded?: boolean
  onClick?: () => void
  className?: string
}

export function SplitIndicatorBadge({
  count,
  isExpanded,
  onClick,
  className,
}: SplitIndicatorBadgeProps) {
  return (
    <Badge
      variant="outline"
      className={cn(
        'gap-1 cursor-pointer hover:bg-accent transition-colors',
        isExpanded && 'bg-accent',
        className
      )}
      onClick={onClick}
    >
      <Split className="h-3 w-3" />
      Split ({count})
    </Badge>
  )
}
