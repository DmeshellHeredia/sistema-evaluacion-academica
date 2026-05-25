"use client"

import { useState } from "react"
import { useMutation } from "@tanstack/react-query"
import { Lock, Eye, EyeOff, CheckCircle } from "lucide-react"
import { authApi, getErrorMessage } from "@/lib/api"

export default function PreferencesPage() {
  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h2 className="text-xl font-bold text-foreground">Preferencias</h2>
      <PasswordSection />
    </div>
  )
}

function PasswordSection() {
  const [current, setCurrent] = useState("")
  const [newPwd, setNewPwd] = useState("")
  const [confirm, setConfirm] = useState("")
  const [showCurrent, setShowCurrent] = useState(false)
  const [showNew, setShowNew] = useState(false)
  const [error, setError] = useState("")
  const [success, setSuccess] = useState(false)

  const mutation = useMutation({
    mutationFn: () => authApi.changePassword(current, newPwd),
    onSuccess: () => {
      setSuccess(true)
      setCurrent("")
      setNewPwd("")
      setConfirm("")
      setError("")
    },
    onError: (e) => {
      setError(getErrorMessage(e))
      setSuccess(false)
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (newPwd !== confirm) { setError("Las contraseñas no coinciden."); return }
    if (newPwd.length < 6) { setError("La contraseña debe tener al menos 6 caracteres."); return }
    setError("")
    mutation.mutate()
  }

  return (
    <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-sm">
      <div className="flex items-center gap-3 px-6 py-4 border-b border-border">
        <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/10">
          <Lock className="h-4.5 w-4.5 text-primary" />
        </div>
        <div>
          <h3 className="text-sm font-semibold text-foreground">Cambiar contraseña</h3>
          <p className="text-xs text-muted-foreground">Actualiza tu contraseña de acceso</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="p-6 space-y-4">
        {success && (
          <div className="flex items-center gap-2 px-4 py-3 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-700 dark:text-emerald-400">
            <CheckCircle className="h-4 w-4 shrink-0" />
            <p className="text-sm font-medium">Contraseña actualizada correctamente.</p>
          </div>
        )}

        {error && (
          <div className="px-4 py-3 rounded-xl bg-red-500/10 border border-red-500/20">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        <div className="space-y-1.5">
          <label htmlFor="current-password" className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">
            Contraseña actual
          </label>
          <div className="relative">
            <input
              id="current-password"
              type={showCurrent ? "text" : "password"}
              value={current}
              onChange={(e) => setCurrent(e.target.value)}
              required
              autoComplete="current-password"
              className="w-full rounded-xl border border-border bg-background px-3 py-2.5 pr-10 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent transition"
            />
            <button
              type="button"
              onClick={() => setShowCurrent((v) => !v)}
              aria-label={showCurrent ? "Ocultar contraseña actual" : "Mostrar contraseña actual"}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring rounded"
            >
              {showCurrent ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="new-password" className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">
            Nueva contraseña
          </label>
          <div className="relative">
            <input
              id="new-password"
              type={showNew ? "text" : "password"}
              value={newPwd}
              onChange={(e) => setNewPwd(e.target.value)}
              required
              minLength={6}
              autoComplete="new-password"
              className="w-full rounded-xl border border-border bg-background px-3 py-2.5 pr-10 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent transition"
            />
            <button
              type="button"
              onClick={() => setShowNew((v) => !v)}
              aria-label={showNew ? "Ocultar nueva contraseña" : "Mostrar nueva contraseña"}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring rounded"
            >
              {showNew ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
          <p className="text-xs text-muted-foreground">Mínimo 6 caracteres</p>
        </div>

        <div className="space-y-1.5">
          <label htmlFor="confirm-password" className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">
            Confirmar nueva contraseña
          </label>
          <input
            id="confirm-password"
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
            autoComplete="new-password"
            className={`w-full rounded-xl border px-3 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent transition bg-background ${
              confirm && confirm !== newPwd ? "border-red-500/50" : "border-border"
            }`}
          />
          {confirm && confirm !== newPwd && (
            <p className="text-xs text-red-500">Las contraseñas no coinciden</p>
          )}
        </div>

        <div className="pt-2">
          <button
            type="submit"
            disabled={mutation.isPending || (!!confirm && confirm !== newPwd)}
            className="w-full flex items-center justify-center gap-2 rounded-xl bg-primary text-primary-foreground text-sm font-medium px-4 py-2.5 hover:bg-primary/90 disabled:opacity-50 transition cursor-pointer"
          >
            {mutation.isPending ? "Actualizando..." : "Actualizar contraseña"}
          </button>
        </div>
      </form>
    </div>
  )
}
