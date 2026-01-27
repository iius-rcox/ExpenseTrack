/**
 * AllowanceFormDialog Component
 *
 * A dialog for creating or editing recurring expense allowances.
 * Uses controlled form state with validation.
 */

import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { toast } from 'sonner'
import { Loader2 } from 'lucide-react'
import { useCreateAllowance, useUpdateAllowance } from '@/hooks/queries/use-allowances'
import { useGLAccounts, useDepartments } from '@/hooks/queries/use-reference-data'
import type { Allowance, CreateAllowanceRequest, UpdateAllowanceRequest } from '@/types/allowance'
import { AllowanceFrequency, getFrequencyDisplayText, NONE_SELECTED, MAX_ALLOWANCE_AMOUNT, MAX_DESCRIPTION_LENGTH } from '@/types/allowance'

interface AllowanceFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Existing allowance to edit (undefined for create mode) */
  allowance?: Allowance
}

export function AllowanceFormDialog({
  open,
  onOpenChange,
  allowance,
}: AllowanceFormDialogProps) {
  const isEditMode = Boolean(allowance)

  // Form state - use NONE_SELECTED sentinel for optional selects
  const [vendorName, setVendorName] = useState('')
  const [amount, setAmount] = useState('')
  const [frequency, setFrequency] = useState<AllowanceFrequency>(AllowanceFrequency.Monthly)
  const [glCode, setGlCode] = useState<string>(NONE_SELECTED)
  const [departmentCode, setDepartmentCode] = useState<string>(NONE_SELECTED)
  const [description, setDescription] = useState('')

  // Validation errors
  const [errors, setErrors] = useState<{
    vendorName?: string
    amount?: string
    description?: string
  }>({})

  // Queries for reference data
  const { data: glAccounts, isLoading: loadingGL } = useGLAccounts()
  const { data: departments, isLoading: loadingDepts } = useDepartments()

  // Mutations
  const { mutate: createAllowance, isPending: isCreating } = useCreateAllowance()
  const { mutate: updateAllowance, isPending: isUpdating } = useUpdateAllowance()

  const isPending = isCreating || isUpdating

  // Reset form to default state
  const resetForm = () => {
    setVendorName('')
    setAmount('')
    setFrequency(AllowanceFrequency.Monthly)
    setGlCode(NONE_SELECTED)
    setDepartmentCode(NONE_SELECTED)
    setDescription('')
    setErrors({})
  }

  // Populate form with allowance data
  const populateForm = (a: Allowance) => {
    setVendorName(a.vendorName)
    setAmount(String(a.amount))
    setFrequency(a.frequency)
    setGlCode(a.glCode || NONE_SELECTED)
    setDepartmentCode(a.departmentCode || NONE_SELECTED)
    setDescription(a.description || '')
    setErrors({})
  }

  // Reset form when dialog opens/closes or allowance changes
  useEffect(() => {
    if (open) {
      if (allowance) {
        populateForm(allowance)
      } else {
        resetForm()
      }
    } else {
      // Clear form state on close to prevent stale data flash on reopen
      resetForm()
    }
  }, [open, allowance])

  const validate = (): boolean => {
    const newErrors: typeof errors = {}

    if (!vendorName.trim()) {
      newErrors.vendorName = 'Vendor name is required'
    } else if (vendorName.length > 100) {
      newErrors.vendorName = 'Vendor name too long (max 100 characters)'
    }

    const parsedAmount = parseFloat(amount)
    if (!amount || isNaN(parsedAmount)) {
      newErrors.amount = 'Amount is required'
    } else if (parsedAmount <= 0) {
      newErrors.amount = 'Amount must be positive'
    } else if (parsedAmount > MAX_ALLOWANCE_AMOUNT) {
      newErrors.amount = `Amount cannot exceed ${MAX_ALLOWANCE_AMOUNT.toLocaleString()}`
    }

    if (description.length > MAX_DESCRIPTION_LENGTH) {
      newErrors.description = `Description too long (max ${MAX_DESCRIPTION_LENGTH} characters)`
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) {
      return
    }

    const parsedAmount = parseFloat(amount)

    if (isEditMode && allowance) {
      // Update existing allowance
      const updateData: UpdateAllowanceRequest = {
        vendorName: vendorName.trim(),
        amount: parsedAmount,
        frequency,
        glCode: glCode === NONE_SELECTED ? null : glCode,
        departmentCode: departmentCode === NONE_SELECTED ? null : departmentCode,
        description: description.trim() || null,
      }

      updateAllowance(
        { id: allowance.id, data: updateData },
        {
          onSuccess: () => {
            toast.success('Allowance updated successfully')
            onOpenChange(false)
          },
          onError: (error) => {
            toast.error(getUserFriendlyErrorMessage(error))
          },
        }
      )
    } else {
      // Create new allowance
      const createData: CreateAllowanceRequest = {
        vendorName: vendorName.trim(),
        amount: parsedAmount,
        frequency,
        glCode: glCode === NONE_SELECTED ? undefined : glCode,
        departmentCode: departmentCode === NONE_SELECTED ? undefined : departmentCode,
        description: description.trim() || undefined,
      }

      createAllowance(createData, {
        onSuccess: () => {
          toast.success('Allowance created successfully')
          onOpenChange(false)
        },
        onError: (error) => {
          toast.error(getUserFriendlyErrorMessage(error))
        },
      })
    }
  }

  const frequencyOptions = [
    { value: AllowanceFrequency.Weekly, label: getFrequencyDisplayText(AllowanceFrequency.Weekly) },
    { value: AllowanceFrequency.Monthly, label: getFrequencyDisplayText(AllowanceFrequency.Monthly) },
    { value: AllowanceFrequency.Quarterly, label: getFrequencyDisplayText(AllowanceFrequency.Quarterly) },
  ]

  // Map API errors to user-friendly messages (avoid exposing internal details)
  const getUserFriendlyErrorMessage = (error: Error & { status?: number }): string => {
    if (error.status === 400) {
      return 'Please check your input and try again.'
    }
    if (error.status === 409) {
      return 'This allowance conflicts with an existing one.'
    }
    if (error.status === 422) {
      return 'Invalid data provided. Please check your entries.'
    }
    // Log full error for debugging, show generic message to user
    console.error('Allowance operation failed:', error)
    return 'Something went wrong. Please try again later.'
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>
            {isEditMode ? 'Edit Allowance' : 'Add Recurring Allowance'}
          </DialogTitle>
          <DialogDescription>
            {isEditMode
              ? 'Update the details of this recurring expense allowance.'
              : 'Create a new recurring expense allowance for automatic categorization.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Vendor Name */}
          <div className="space-y-2">
            <Label htmlFor="vendorName">Vendor Name *</Label>
            <Input
              id="vendorName"
              placeholder="e.g., Netflix, Adobe, etc."
              value={vendorName}
              onChange={(e) => setVendorName(e.target.value)}
              disabled={isPending}
            />
            {errors.vendorName && (
              <p className="text-sm text-destructive">{errors.vendorName}</p>
            )}
          </div>

          {/* Amount and Frequency */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="amount">Amount *</Label>
              <Input
                id="amount"
                type="number"
                step="0.01"
                min="0"
                placeholder="0.00"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                disabled={isPending}
              />
              {errors.amount && (
                <p className="text-sm text-destructive">{errors.amount}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="frequency">Frequency *</Label>
              <Select
                value={String(frequency)}
                onValueChange={(value) => setFrequency(Number(value) as AllowanceFrequency)}
                disabled={isPending}
              >
                <SelectTrigger id="frequency">
                  <SelectValue placeholder="Select frequency" />
                </SelectTrigger>
                <SelectContent>
                  {frequencyOptions.map((option) => (
                    <SelectItem key={option.value} value={String(option.value)}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* GL Code */}
          <div className="space-y-2">
            <Label htmlFor="glCode">GL Account</Label>
            <Select
              value={glCode}
              onValueChange={setGlCode}
              disabled={isPending || loadingGL}
            >
              <SelectTrigger id="glCode">
                <SelectValue placeholder={loadingGL ? 'Loading...' : 'Select GL account (optional)'} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={NONE_SELECTED}>None</SelectItem>
                {glAccounts?.map((gl) => (
                  <SelectItem key={gl.code} value={gl.code}>
                    {gl.code} - {gl.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Department */}
          <div className="space-y-2">
            <Label htmlFor="departmentCode">Department</Label>
            <Select
              value={departmentCode}
              onValueChange={setDepartmentCode}
              disabled={isPending || loadingDepts}
            >
              <SelectTrigger id="departmentCode">
                <SelectValue placeholder={loadingDepts ? 'Loading...' : 'Select department (optional)'} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={NONE_SELECTED}>None</SelectItem>
                {departments?.map((dept) => (
                  <SelectItem key={dept.code} value={dept.code}>
                    {dept.name} ({dept.code})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              placeholder="Optional notes about this recurring expense..."
              rows={3}
              maxLength={MAX_DESCRIPTION_LENGTH}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              disabled={isPending}
            />
            {errors.description && (
              <p className="text-sm text-destructive">{errors.description}</p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isPending}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEditMode ? 'Save Changes' : 'Add Allowance'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
