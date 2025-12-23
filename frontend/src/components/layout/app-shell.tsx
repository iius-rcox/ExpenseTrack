"use client"

import { Outlet } from '@tanstack/react-router'
import { SidebarProvider, SidebarInset, SidebarTrigger } from '@/components/ui/sidebar'
import { AppSidebar } from './app-sidebar'
import { Separator } from '@/components/ui/separator'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import { ThemeToggle } from '@/components/theme-toggle'

interface AppShellProps {
  breadcrumbs?: Array<{
    label: string
    href?: string
  }>
  children?: React.ReactNode
}

export function AppShell({ breadcrumbs = [], children }: AppShellProps) {
  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1" />
          <Separator orientation="vertical" className="mr-2 h-4" />
          <Breadcrumb>
            <BreadcrumbList>
              {breadcrumbs.map((crumb, index) => (
                <BreadcrumbItem key={crumb.label}>
                  {index > 0 && <BreadcrumbSeparator />}
                  {index === breadcrumbs.length - 1 || !crumb.href ? (
                    <BreadcrumbPage>{crumb.label}</BreadcrumbPage>
                  ) : (
                    <BreadcrumbLink href={crumb.href}>{crumb.label}</BreadcrumbLink>
                  )}
                </BreadcrumbItem>
              ))}
            </BreadcrumbList>
          </Breadcrumb>
          {/* Spacer to push ThemeToggle to the right */}
          <div className="flex-1" />
          <ThemeToggle />
        </header>
        <main className="flex flex-1 flex-col gap-4 p-4">
          {children || <Outlet />}
        </main>
      </SidebarInset>
    </SidebarProvider>
  )
}
