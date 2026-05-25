"use client"

import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { Toaster } from "sonner"
import { AuthProvider } from "@/context/auth-context"
import { useState, type ReactNode } from "react"

export function Providers({ children }: { children: ReactNode }) {
  // QueryClient se crea dentro del componente para que cada request
  // en SSR tenga su propia instancia (evita compartir estado entre usuarios)
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            retry: 1,
            retryDelay: 800,         // falla rápido en vez de esperar backoff exponencial
            refetchOnWindowFocus: false,
          },
        },
      })
  )

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        {children}
        <Toaster
          position="top-right"
          richColors
          closeButton
          toastOptions={{ duration: 4000 }}
        />
      </AuthProvider>
    </QueryClientProvider>
  )
}
