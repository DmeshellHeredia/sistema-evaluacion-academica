"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { Sidebar } from "@/components/sidebar"
import { Header } from "@/components/header"
import { ErrorBoundary } from "@/components/error-boundary"
import { useAuth } from "@/context/auth-context"

function DashboardSkeleton() {
  return (
    <div className="flex h-screen overflow-hidden bg-background" aria-hidden="true">
      <aside className="hidden lg:flex lg:w-64 lg:shrink-0 border-r border-border bg-card" />
      <div className="flex flex-1 flex-col overflow-hidden">
        <div className="h-16 border-b border-border bg-card/80 shrink-0" />
        <div className="flex-1 overflow-hidden p-4 md:p-6 space-y-4 animate-pulse">
          <div className="h-28 rounded-2xl bg-muted" />
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[0, 1, 2, 3].map((i) => (
              <div key={i} className="h-24 rounded-2xl bg-muted" />
            ))}
          </div>
          <div className="h-48 rounded-2xl bg-muted" />
        </div>
      </div>
    </div>
  )
}

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const { user, hydrated } = useAuth()
  const router = useRouter()
  const [sidebarOpen, setSidebarOpen] = useState(false)

  useEffect(() => {
    if (hydrated && !user) {
      router.replace("/login")
    }
  }, [user, hydrated, router])

  // Show skeleton while auth hydrates — prevents white flash without hydration mismatch
  if (!hydrated) return <DashboardSkeleton />
  if (!user) return null

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {/* Desktop sidebar — siempre visible en lg+ */}
      <aside className="hidden lg:flex lg:w-64 lg:shrink-0 border-r border-border">
        <Sidebar />
      </aside>

      {/* Mobile sidebar — overlay */}
      {sidebarOpen && (
        <>
          {/* Backdrop — keyboard-accessible dismiss */}
          <div
            role="button"
            tabIndex={0}
            aria-label="Cerrar menú"
            className="fixed inset-0 z-30 bg-black/50 lg:hidden cursor-pointer"
            onClick={() => setSidebarOpen(false)}
            onKeyDown={(e) => {
              if (e.key === "Enter" || e.key === " ") setSidebarOpen(false)
            }}
          />
          {/* Drawer */}
          <aside className="fixed inset-y-0 left-0 z-40 w-64 lg:hidden">
            <Sidebar onClose={() => setSidebarOpen(false)} />
          </aside>
        </>
      )}

      {/* Main content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        <Header onMenuClick={() => setSidebarOpen(true)} />
        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          <ErrorBoundary>
            {children}
          </ErrorBoundary>
        </main>
      </div>
    </div>
  )
}
