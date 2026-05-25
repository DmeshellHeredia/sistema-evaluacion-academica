"use client"

import { AlertCircle, WifiOff, ShieldX, ServerCrash, RefreshCw } from "lucide-react"
import axios from "axios"
import { getErrorMessage } from "@/lib/api"
import type { LucideIcon } from "lucide-react"

function classify(error: unknown): { icon: LucideIcon; title: string; message: string } {
  if (!axios.isAxiosError(error)) {
    return { icon: AlertCircle, title: "Algo salió mal", message: "Ocurrió un error inesperado." }
  }
  if (!error.response) {
    if (error.code === "ECONNABORTED") {
      return {
        icon: ServerCrash,
        title: "El servidor no respondió",
        message: "La petición tardó demasiado. Verifica que el API esté corriendo en localhost:5000.",
      }
    }
    return {
      icon: WifiOff,
      title: "Sin conexión con el servidor",
      message: "No se pudo contactar el backend. Asegúrate de que esté corriendo en localhost:5000.",
    }
  }
  const status = error.response.status
  if (status === 403) {
    return { icon: ShieldX, title: "Sin permiso", message: "No tienes acceso a esta información." }
  }
  if (status >= 500) {
    return {
      icon: ServerCrash,
      title: "Error del servidor",
      message: "El API devolvió un error interno. Intenta de nuevo en un momento.",
    }
  }
  return { icon: AlertCircle, title: "Error al cargar", message: getErrorMessage(error) }
}

interface ErrorStateProps {
  error: unknown
  onRetry?: () => void
}

export function ErrorState({ error, onRetry }: ErrorStateProps) {
  const { icon: Icon, title, message } = classify(error)
  return (
    <div role="alert" className="rounded-2xl border border-destructive/20 bg-destructive/5 p-8 text-center space-y-4">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10 mx-auto">
        <Icon className="h-6 w-6 text-destructive" />
      </div>
      <div>
        <p className="font-semibold text-foreground">{title}</p>
        <p className="text-sm text-muted-foreground mt-1">{message}</p>
      </div>
      {onRetry && (
        <button
          onClick={onRetry}
          className="inline-flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80 transition-colors cursor-pointer"
        >
          <RefreshCw className="h-3.5 w-3.5" />
          Reintentar
        </button>
      )}
    </div>
  )
}
