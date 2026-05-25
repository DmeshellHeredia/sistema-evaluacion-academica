"use client"

import React from "react"
import { AlertCircle, RefreshCw } from "lucide-react"

interface Props {
  children: React.ReactNode
  fallback?: React.ReactNode
}

interface State {
  hasError: boolean
}

export class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    if (process.env.NODE_ENV === "development") {
      console.error("[ErrorBoundary]", error, info.componentStack)
    }
  }

  handleReset = () => {
    this.setState({ hasError: false })
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback
      return (
        <div
          role="alert"
          className="flex flex-col items-center justify-center py-16 space-y-5 text-center"
        >
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-destructive/10">
            <AlertCircle className="h-7 w-7 text-destructive" />
          </div>
          <div className="space-y-2 max-w-sm">
            <p className="font-semibold text-foreground text-lg">Algo salió mal</p>
            <p className="text-sm text-muted-foreground">
              Esta sección encontró un error inesperado. Puedes intentar de nuevo o recargar la página.
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button
              onClick={this.handleReset}
              className="flex items-center gap-2 h-9 px-4 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <RefreshCw className="h-4 w-4" />
              Intentar de nuevo
            </button>
            <button
              onClick={() => window.location.reload()}
              className="flex items-center gap-2 h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              Recargar página
            </button>
          </div>
        </div>
      )
    }
    return this.props.children
  }
}
