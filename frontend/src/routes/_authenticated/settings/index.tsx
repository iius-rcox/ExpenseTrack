"use client"

import { useState, useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import {
  useUserInfo,
  useUserPreferences,
  useUpdatePreferences,
  useDepartments,
  useProjects,
  useCategories,
} from '@/hooks/queries/use-settings'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { toast } from 'sonner'
import {
  User,
  Settings,
  Palette,
  Building2,
  Tag,
  Plus,
  Loader2,
  Moon,
  Sun,
  Monitor,
  FileText,
  Repeat,
} from 'lucide-react'
import { AllowanceList } from '@/components/settings/allowance-list'
import { AllowanceFormDialog } from '@/components/settings/allowance-form-dialog'

export const Route = createFileRoute('/_authenticated/settings/')({
  component: SettingsPage,
})

function SettingsPage() {
  const { data: userInfo, isLoading: loadingUser } = useUserInfo()
  const { data: preferences, isLoading: loadingPrefs } = useUserPreferences()
  const { data: departments, isLoading: loadingDepts } = useDepartments()
  const { data: projects } = useProjects(preferences?.defaultDepartment)
  const { data: categories, isLoading: loadingCategories } = useCategories()

  const { mutate: updatePreferences, isPending: updatingPrefs } = useUpdatePreferences()

  // Allowance dialog state
  const [allowanceDialogOpen, setAllowanceDialogOpen] = useState(false)

  // Report preferences form state
  const [employeeId, setEmployeeId] = useState('')
  const [supervisorName, setSupervisorName] = useState('')
  const [departmentName, setDepartmentName] = useState('')
  const [reportPrefsEdited, setReportPrefsEdited] = useState(false)

  // Sync report preferences form state with fetched preferences
  useEffect(() => {
    if (preferences) {
      setEmployeeId(preferences.employeeId || '')
      setSupervisorName(preferences.supervisorName || '')
      setDepartmentName(preferences.departmentName || '')
      setReportPrefsEdited(false)
    }
  }, [preferences])

  const handleThemeChange = (theme: 'light' | 'dark' | 'system') => {
    updatePreferences({ theme }, {
      onSuccess: () => {
        toast.success('Theme preference updated')
        // Apply theme to document
        if (theme === 'system') {
          const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
          document.documentElement.classList.toggle('dark', systemTheme === 'dark')
        } else {
          document.documentElement.classList.toggle('dark', theme === 'dark')
        }
      },
      onError: () => {
        toast.error('Failed to update theme')
      },
    })
  }

  const handleDepartmentChange = (departmentId: string) => {
    updatePreferences({ defaultDepartment: departmentId, defaultProject: undefined }, {
      onSuccess: () => {
        toast.success('Default department updated')
      },
      onError: () => {
        toast.error('Failed to update department')
      },
    })
  }

  const handleProjectChange = (projectId: string) => {
    updatePreferences({ defaultProject: projectId }, {
      onSuccess: () => {
        toast.success('Default project updated')
      },
      onError: () => {
        toast.error('Failed to update project')
      },
    })
  }

  const handleReportPrefsChange = (field: 'employeeId' | 'supervisorName' | 'departmentName', value: string) => {
    setReportPrefsEdited(true)
    if (field === 'employeeId') setEmployeeId(value)
    else if (field === 'supervisorName') setSupervisorName(value)
    else if (field === 'departmentName') setDepartmentName(value)
  }

  const handleSaveReportPrefs = () => {
    updatePreferences(
      {
        employeeId: employeeId || undefined,
        supervisorName: supervisorName || undefined,
        departmentName: departmentName || undefined,
      },
      {
        onSuccess: () => {
          toast.success('Report preferences saved')
          setReportPrefsEdited(false)
        },
        onError: () => {
          toast.error('Failed to save report preferences')
        },
      }
    )
  }

  const isLoading = loadingUser || loadingPrefs

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
        <p className="text-muted-foreground">
          Manage your account and application preferences
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Profile Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <User className="h-5 w-5" />
              Profile
            </CardTitle>
            <CardDescription>Your account information</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {loadingUser ? (
              <div className="space-y-4">
                <Skeleton className="h-4 w-48" />
                <Skeleton className="h-4 w-32" />
              </div>
            ) : userInfo ? (
              <>
                <div className="space-y-1">
                  <Label className="text-sm text-muted-foreground">Display Name</Label>
                  <p className="font-medium">{userInfo.displayName}</p>
                </div>
                <div className="space-y-1">
                  <Label className="text-sm text-muted-foreground">Email</Label>
                  <p className="font-medium">{userInfo.email}</p>
                </div>
                <div className="space-y-1">
                  <Label className="text-sm text-muted-foreground">User ID</Label>
                  <p className="font-mono text-sm text-muted-foreground">{userInfo.id}</p>
                </div>
              </>
            ) : (
              <p className="text-muted-foreground">Unable to load profile</p>
            )}
          </CardContent>
        </Card>

        {/* Theme Settings */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Palette className="h-5 w-5" />
              Appearance
            </CardTitle>
            <CardDescription>Customize how ExpenseFlow looks</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {isLoading ? (
              <Skeleton className="h-10 w-full" />
            ) : (
              <div className="space-y-4">
                <Label>Theme</Label>
                <div className="flex gap-2">
                  <Button
                    variant={preferences?.theme === 'light' ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handleThemeChange('light')}
                    disabled={updatingPrefs}
                  >
                    <Sun className="mr-2 h-4 w-4" />
                    Light
                  </Button>
                  <Button
                    variant={preferences?.theme === 'dark' ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handleThemeChange('dark')}
                    disabled={updatingPrefs}
                  >
                    <Moon className="mr-2 h-4 w-4" />
                    Dark
                  </Button>
                  <Button
                    variant={preferences?.theme === 'system' ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handleThemeChange('system')}
                    disabled={updatingPrefs}
                  >
                    <Monitor className="mr-2 h-4 w-4" />
                    System
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Default Department/Project */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Default Selections
            </CardTitle>
            <CardDescription>Set your default department and project for new expenses</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {loadingDepts ? (
              <div className="space-y-4">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : (
              <>
                <div className="space-y-2">
                  <Label>Default Department</Label>
                  <Select
                    value={preferences?.defaultDepartment || ''}
                    onValueChange={handleDepartmentChange}
                    disabled={updatingPrefs}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select department" />
                    </SelectTrigger>
                    <SelectContent>
                      {departments?.map((dept) => (
                        <SelectItem key={dept.id} value={dept.id}>
                          {dept.name} ({dept.code})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>Default Project</Label>
                  <Select
                    value={preferences?.defaultProject || ''}
                    onValueChange={handleProjectChange}
                    disabled={updatingPrefs || !preferences?.defaultDepartment}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder={preferences?.defaultDepartment ? "Select project" : "Select department first"} />
                    </SelectTrigger>
                    <SelectContent>
                      {projects?.map((proj) => (
                        <SelectItem key={proj.id} value={proj.id}>
                          {proj.name} ({proj.code})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        {/* Recurring Allowances */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Repeat className="h-5 w-5" />
              Recurring Allowances
            </CardTitle>
            <CardDescription>
              Pre-configure recurring expenses for automatic categorization
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setAllowanceDialogOpen(true)}
            >
              <Plus className="mr-2 h-4 w-4" />
              Add Allowance
            </Button>
            <Separator />
            <AllowanceList />
            <AllowanceFormDialog
              open={allowanceDialogOpen}
              onOpenChange={setAllowanceDialogOpen}
            />
          </CardContent>
        </Card>

        {/* Report Preferences - Employee Info for PDF Header */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Report Preferences
            </CardTitle>
            <CardDescription>
              Your information for expense report PDF headers
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {isLoading ? (
              <div className="space-y-4">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : (
              <>
                <div className="space-y-2">
                  <Label htmlFor="employee-id">Employee ID</Label>
                  <Input
                    id="employee-id"
                    placeholder="e.g., EMP-001"
                    value={employeeId}
                    onChange={(e) => handleReportPrefsChange('employeeId', e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="supervisor-name">Supervisor</Label>
                  <Input
                    id="supervisor-name"
                    placeholder="e.g., John Smith"
                    value={supervisorName}
                    onChange={(e) => handleReportPrefsChange('supervisorName', e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="department-name">Department Name</Label>
                  <Input
                    id="department-name"
                    placeholder="e.g., Engineering"
                    value={departmentName}
                    onChange={(e) => handleReportPrefsChange('departmentName', e.target.value)}
                  />
                  <p className="text-xs text-muted-foreground">
                    This appears in the PDF header. Different from your default department selection above.
                  </p>
                </div>
                <Button
                  onClick={handleSaveReportPrefs}
                  disabled={!reportPrefsEdited || updatingPrefs}
                  className="w-full"
                >
                  {updatingPrefs && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Save Report Preferences
                </Button>
              </>
            )}
          </CardContent>
        </Card>

        {/* Expense Categories (Read-only) */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Tag className="h-5 w-5" />
              Expense Categories
            </CardTitle>
            <CardDescription>Categories derived from your transaction history</CardDescription>
          </CardHeader>
          <CardContent>
            {loadingCategories ? (
              <div className="space-y-2">
                {Array.from({ length: 4 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full" />
                ))}
              </div>
            ) : categories && categories.length > 0 ? (
              <div className="flex flex-wrap gap-2">
                {categories.map((category) => (
                  <Badge key={category.id} variant="secondary" className="text-sm py-1.5 px-3">
                    {category.name}
                  </Badge>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-4">
                No categories yet. Categories will appear here as you add transactions.
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Additional Settings Info */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            About
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-1">
              <Label className="text-sm text-muted-foreground">Version</Label>
              <p className="font-mono text-sm">1.0.0</p>
            </div>
            <div className="space-y-1">
              <Label className="text-sm text-muted-foreground">Environment</Label>
              <p className="font-mono text-sm">{import.meta.env.MODE}</p>
            </div>
            <div className="space-y-1">
              <Label className="text-sm text-muted-foreground">API Endpoint</Label>
              <p className="font-mono text-sm truncate">{import.meta.env.VITE_API_BASE_URL || '/api'}</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
