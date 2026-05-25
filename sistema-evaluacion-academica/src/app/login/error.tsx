"use client"

import { AlertCircle, RefreshCw } from "lucide-react"
import { useEffect } from "react"

export default function LoginError({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  useEffect(() => {
    if (process.env.NODE_ENV === "development") console.error(error)
  }, [error])

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <div className="flex flex-col items-center space-y-5 text-center max-w-sm">
        <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-destructive/10">
          <AlertCircle className="h-7 w-7 text-destructive" />
        </div>
        <div className="space-y-2">
          <p className="font-semibold text-foreground text-lg">No se pudo cargar el login</p>
          <p className="text-sm text-muted-foreground">
            Ocurrió un error al inicializar la página. Intenta de nuevo.
          </p>
        </div>
        <button
          onClick={reset}
          className="flex items-center gap-2 h-9 px-4 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer"
        >
          <RefreshCw className="h-4 w-4" />
          Intentar de nuevo
        </button>
      </div>
    </div>
  )
}
