"use client"

import { AlertCircle, CheckCircle, ClipboardList, BookMarked, MessageSquare, Star, FileText, Loader2, Trash2, Upload, XCircle, X } from "lucide-react"
import type { ActivityType } from "@/types"

export const ACTIVITY_TYPES: ActivityType[] = ["Tarea", "Recurso", "Cuestionario", "Foro", "Evaluacion"]

export const activityTypeIcon = (type: ActivityType) => {
  switch (type) {
    case "Tarea": return ClipboardList
    case "Recurso": return BookMarked
    case "Cuestionario": return MessageSquare
    case "Foro": return MessageSquare
    case "Evaluacion": return Star
    default: return FileText
  }
}

export const statusColor = (status: string) => {
  switch (status) {
    case "Calificada": return "text-emerald-600 dark:text-emerald-400"
    case "Entregada": return "text-blue-600 dark:text-blue-400"
    case "Cerrada": return "text-gray-500"
    default: return "text-amber-600 dark:text-amber-400"
  }
}

export const statusIcon = (status: string) => {
  switch (status) {
    case "Calificada": return CheckCircle
    case "Entregada": return Upload
    case "Cerrada": return XCircle
    default: return AlertCircle
  }
}

export const inputCls = "w-full rounded-xl border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent transition"
export const primaryBtnCls = "flex items-center justify-center gap-2 rounded-xl bg-primary text-primary-foreground text-sm font-medium px-4 py-2.5 hover:bg-primary/90 disabled:opacity-50 transition cursor-pointer"
export const cancelBtnCls = "flex items-center justify-center gap-2 rounded-xl border border-border bg-background text-foreground text-sm font-medium px-4 py-2.5 hover:bg-muted transition cursor-pointer"

export function Spinner() {
  return <div className="flex justify-center py-12"><Loader2 className="h-6 w-6 animate-spin text-primary" /></div>
}

export function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">{label}</label>
      {children}
    </div>
  )
}

export function ModalOverlay({ children, onClose, wide }: { children: React.ReactNode; onClose: () => void; wide?: boolean }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" onClick={onClose}>
      <div
        className={`bg-card border border-border rounded-2xl shadow-2xl p-6 w-full ${wide ? "max-w-2xl" : "max-w-lg"}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex justify-end mb-2 -mt-2 -mr-2">
          <button onClick={onClose} className="flex h-8 w-8 items-center justify-center rounded-lg hover:bg-muted transition-colors cursor-pointer">
            <X className="h-4 w-4 text-muted-foreground" />
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}

export function DeleteConfirmModal({
  title,
  onConfirm,
  onCancel,
  loading,
}: {
  title: string
  onConfirm: () => void
  onCancel: () => void
  loading: boolean
}) {
  return (
    <ModalOverlay onClose={onCancel}>
      <div className="space-y-5">
        <h2 className="text-lg font-bold text-foreground">{title}</h2>
        <div className="flex items-start gap-3 px-4 py-3 rounded-xl bg-destructive/10 border border-destructive/20">
          <AlertCircle className="h-4 w-4 text-destructive shrink-0 mt-0.5" />
          <p className="text-sm text-destructive">Esta acción no se puede deshacer.</p>
        </div>
        <div className="flex gap-3 pt-1">
          <button type="button" onClick={onCancel} className={`flex-1 ${cancelBtnCls}`}>Cancelar</button>
          <button
            onClick={onConfirm}
            disabled={loading}
            className="flex-1 flex items-center justify-center gap-2 rounded-xl bg-destructive text-white text-sm font-medium px-4 py-2.5 hover:bg-destructive/90 disabled:opacity-50 transition cursor-pointer"
          >
            {loading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Trash2 className="h-3.5 w-3.5" />}
            {loading ? "Eliminando..." : "Eliminar"}
          </button>
        </div>
      </div>
    </ModalOverlay>
  )
}
