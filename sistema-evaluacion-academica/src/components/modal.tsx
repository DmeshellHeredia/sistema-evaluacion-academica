"use client"

import { useEffect, useId } from "react"
import { X } from "lucide-react"
import { cloneElement, isValidElement } from "react"

interface ModalProps {
  title: string
  onClose: () => void
  children: React.ReactNode
  size?: "sm" | "md" | "lg"
}

export function Modal({ title, onClose, children, size = "md" }: ModalProps) {
  const titleId = useId()

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose() }
    document.addEventListener("keydown", handler)
    return () => document.removeEventListener("keydown", handler)
  }, [onClose])

  const widthMap = { sm: "max-w-sm", md: "max-w-md", lg: "max-w-lg" }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
    >
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className={`relative w-full ${widthMap[size]} bg-card border border-border rounded-2xl shadow-2xl`}>
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <h2 id={titleId} className="text-base font-semibold text-foreground">{title}</h2>
          <button
            onClick={onClose}
            aria-label="Cerrar"
            className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="px-6 py-5">{children}</div>
      </div>
    </div>
  )
}

// Primitivos de formulario compartidos por páginas CRUD
export const inputCls =
  "w-full h-9 px-3 rounded-lg border border-input bg-background text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring/50 focus:border-ring transition-colors"

export function Field({
  label,
  error,
  children,
}: {
  label: string
  error?: string
  children: React.ReactNode
}) {
  const id = useId()
  const childWithId = isValidElement(children)
    ? cloneElement(children as React.ReactElement<{ id?: string; "aria-describedby"?: string }>, {
        id,
        ...(error ? { "aria-describedby": `${id}-err` } : {}),
      })
    : children

  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="text-sm font-medium text-foreground">
        {label}
      </label>
      {childWithId}
      {error && (
        <p id={`${id}-err`} role="alert" className="text-xs text-destructive">
          {error}
        </p>
      )}
    </div>
  )
}
