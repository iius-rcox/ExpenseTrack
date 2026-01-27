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
  useCreateCategory,
  useUpdateCategory,
  useDeleteCategory,
} from '@/hooks/queries/use-settings'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { toast } from 'sonner'
import {
  User,
  Settings,
  Palette,
  Building2,
  Tag,
  Plus,
  Pencil,
  Trash2,
  Loader2,
  Check,
  Moon,
  Sun,
  Monitor,
  FileText,
} from 'lucide-react'

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
  const { mutate: createCategory, isPending: creatingCategory } = useCreateCategory()
  const { mutate: updateCategory } = useUpdateCategory()
  const { mutate: deleteCategory, isPending: deletingCategory } = useDeleteCategory()

  const [newCategoryName, setNewCategoryName] = useState('')
  const [newCategoryDesc, setNewCategoryDesc] = useState('')
  const [editingCategory, setEditingCategory] = useState<{ id: string; name: string; description: string } | null>(null)
  const [categoryDialogOpen, setCategoryDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [categoryToDelete, setCategoryToDelete] = useState<string | null>(null)

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

  const handleCreateCategory = () => {
    if (!newCategoryName.trim()) return

    createCategory(
      { name: newCategoryName.trim(), description: newCategoryDesc.trim() || undefined },
      {
        onSuccess: () => {
          toast.success('Category created')
          setNewCategoryName('')
          setNewCategoryDesc('')
          setCategoryDialogOpen(false)
        },
        onError: () => {
          toast.error('Failed to create category')
        },
      }
    )
  }

  const handleUpdateCategory = () => {
    if (!editingCategory || !editingCategory.name.trim()) return

    updateCategory(
      {
        id: editingCategory.id,
        name: editingCategory.name.trim(),
        description: editingCategory.description.trim() || undefined,
      },
      {
        onSuccess: () => {
          toast.success('Category updated')
          setEditingCategory(null)
        },
        onError: () => {
          toast.error('Failed to update category')
        },
      }
    )
  }

  const handleDeleteCategory = () => {
    if (!categoryToDelete) return

    deleteCategory(categoryToDelete, {
      onSuccess: () => {
        toast.success('Category deleted')
        setCategoryToDelete(null)
        setDeleteDialogOpen(false)
      },
      onError: () => {
        toast.error('Failed to delete category')
      },
    })
  }

  const handleToggleCategoryActive = (id: string, currentActive: boolean) => {
    updateCategory(
      { id, isActive: !currentActive },
      {
        onSuccess: () => {
          toast.success(`Category ${currentActive ? 'deactivated' : 'activated'}`)
        },
        onError: () => {
          toast.error('Failed to update category')
        },
      }
    )
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

        {/* Category Management */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Tag className="h-5 w-5" />
              Expense Categories
            </CardTitle>
            <CardDescription>Manage custom expense categories</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Dialog open={categoryDialogOpen} onOpenChange={setCategoryDialogOpen}>
              <DialogTrigger asChild>
                <Button variant="outline" size="sm">
                  <Plus className="mr-2 h-4 w-4" />
                  Add Category
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Create Category</DialogTitle>
                  <DialogDescription>Add a new expense category</DialogDescription>
                </DialogHeader>
                <div className="space-y-4 py-4">
                  <div className="space-y-2">
                    <Label htmlFor="category-name">Name</Label>
                    <Input
                      id="category-name"
                      value={newCategoryName}
                      onChange={(e) => setNewCategoryName(e.target.value)}
                      placeholder="e.g., Office Supplies"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="category-desc">Description (optional)</Label>
                    <Input
                      id="category-desc"
                      value={newCategoryDesc}
                      onChange={(e) => setNewCategoryDesc(e.target.value)}
                      placeholder="Brief description"
                    />
                  </div>
                </div>
                <DialogFooter>
                  <Button
                    onClick={handleCreateCategory}
                    disabled={creatingCategory || !newCategoryName.trim()}
                  >
                    {creatingCategory && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                    Create Category
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>

            <Separator />

            {loadingCategories ? (
              <div className="space-y-2">
                {Array.from({ length: 4 }).map((_, i) => (
                  <Skeleton key={i} className="h-12 w-full" />
                ))}
              </div>
            ) : categories && categories.length > 0 ? (
              <div className="space-y-2">
                {categories.map((category) => (
                  <div
                    key={category.id}
                    className="flex items-center justify-between p-3 rounded-lg border"
                  >
                    <div className="flex items-center gap-3">
                      <Switch
                        checked={category.isActive}
                        onCheckedChange={() => handleToggleCategoryActive(category.id, category.isActive)}
                      />
                      <div>
                        <p className={`font-medium ${!category.isActive ? 'text-muted-foreground' : ''}`}>
                          {category.name}
                        </p>
                        {category.description && (
                          <p className="text-xs text-muted-foreground">{category.description}</p>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      {!category.isActive && (
                        <Badge variant="outline" className="mr-2">Inactive</Badge>
                      )}
                      <Dialog>
                        <DialogTrigger asChild>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => setEditingCategory({
                              id: category.id,
                              name: category.name,
                              description: category.description || '',
                            })}
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                        </DialogTrigger>
                        <DialogContent>
                          <DialogHeader>
                            <DialogTitle>Edit Category</DialogTitle>
                            <DialogDescription>Update category details</DialogDescription>
                          </DialogHeader>
                          {editingCategory && (
                            <div className="space-y-4 py-4">
                              <div className="space-y-2">
                                <Label htmlFor="edit-category-name">Name</Label>
                                <Input
                                  id="edit-category-name"
                                  value={editingCategory.name}
                                  onChange={(e) => setEditingCategory({
                                    ...editingCategory,
                                    name: e.target.value,
                                  })}
                                />
                              </div>
                              <div className="space-y-2">
                                <Label htmlFor="edit-category-desc">Description</Label>
                                <Input
                                  id="edit-category-desc"
                                  value={editingCategory.description}
                                  onChange={(e) => setEditingCategory({
                                    ...editingCategory,
                                    description: e.target.value,
                                  })}
                                />
                              </div>
                            </div>
                          )}
                          <DialogFooter>
                            <Button onClick={handleUpdateCategory}>
                              <Check className="mr-2 h-4 w-4" />
                              Save Changes
                            </Button>
                          </DialogFooter>
                        </DialogContent>
                      </Dialog>

                      <AlertDialog open={deleteDialogOpen && categoryToDelete === category.id} onOpenChange={setDeleteDialogOpen}>
                        <AlertDialogTrigger asChild>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => {
                              setCategoryToDelete(category.id)
                              setDeleteDialogOpen(true)
                            }}
                          >
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                          <AlertDialogHeader>
                            <AlertDialogTitle>Delete Category?</AlertDialogTitle>
                            <AlertDialogDescription>
                              This will permanently delete the "{category.name}" category.
                              Existing expenses using this category will not be affected.
                            </AlertDialogDescription>
                          </AlertDialogHeader>
                          <AlertDialogFooter>
                            <AlertDialogCancel onClick={() => setCategoryToDelete(null)}>
                              Cancel
                            </AlertDialogCancel>
                            <AlertDialogAction
                              onClick={handleDeleteCategory}
                              disabled={deletingCategory}
                            >
                              {deletingCategory && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                              Delete
                            </AlertDialogAction>
                          </AlertDialogFooter>
                        </AlertDialogContent>
                      </AlertDialog>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-4">
                No custom categories yet
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
