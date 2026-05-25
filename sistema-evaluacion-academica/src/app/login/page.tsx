"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useState, useEffect } from "react"
import { toast } from "sonner"
import { Eye, EyeOff, GraduationCap, Loader2 } from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { getErrorMessage } from "@/lib/api"
import { consumeSessionExpiredFlag } from "@/lib/auth"

const schema = z.object({
  email: z.string().email("Correo electrónico inválido"),
  password: z.string().min(1, "La contraseña es requerida"),
})
type FormData = z.infer<typeof schema>

export default function LoginPage() {
  const { login } = useAuth()
  const [showPass, setShowPass] = useState(false)

  useEffect(() => {
    if (consumeSessionExpiredFlag()) {
      toast.warning("Tu sesión expiró. Inicia sesión nuevamente.")
    }
  }, [])

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) })

  async function onSubmit(data: FormData) {
    try {
      await login(data)
    } catch (err) {
      toast.error(getErrorMessage(err))
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 rounded-full bg-primary/10 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 rounded-full bg-primary/5 blur-3xl" />
      </div>

      <div className="w-full max-w-md relative">
        <div className="bg-card border border-border rounded-2xl shadow-xl p-8">
          <div className="flex flex-col items-center mb-8">
            <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-primary shadow-lg shadow-primary/30 mb-4">
              <GraduationCap className="h-7 w-7 text-primary-foreground" />
            </div>
            <h1 className="text-2xl font-bold text-foreground">Academia</h1>
            <p className="text-sm text-muted-foreground mt-1">
              Sistema de Evaluación Académica
            </p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div className="space-y-1.5">
              <label className="text-sm font-medium text-foreground" htmlFor="email">
                Correo electrónico
              </label>
              <input
                {...register("email")}
                id="email"
                type="email"
                placeholder="admin@academia.com"
                autoComplete="email"
                className="w-full h-10 px-3 rounded-lg border border-input bg-background text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring/50 focus:border-ring transition-colors"
              />
              {errors.email && (
                <p className="text-xs text-destructive">{errors.email.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="text-sm font-medium text-foreground" htmlFor="password">
                Contraseña
              </label>
              <div className="relative">
                <input
                  {...register("password")}
                  id="password"
                  type={showPass ? "text" : "password"}
                  placeholder="••••••••"
                  autoComplete="current-password"
                  className="w-full h-10 px-3 pr-10 rounded-lg border border-input bg-background text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring/50 focus:border-ring transition-colors"
                />
                <button
                  type="button"
                  onClick={() => setShowPass((v) => !v)}
                  aria-label={showPass ? "Ocultar contraseña" : "Mostrar contraseña"}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring rounded"
                >
                  {showPass ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.password && (
                <p className="text-xs text-destructive">{errors.password.message}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full h-10 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 cursor-pointer shadow-lg shadow-primary/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
              {isSubmitting ? "Ingresando..." : "Ingresar"}
            </button>
          </form>

          <div className="mt-6 p-4 rounded-xl bg-muted/60 border border-border">
            <p className="text-xs font-semibold text-muted-foreground mb-2">Credenciales de prueba</p>
            <div className="space-y-1 text-xs text-muted-foreground font-mono">
              <div className="flex justify-between">
                <span className="text-foreground/70">Admin:</span>
                <span>admin@academia.com / Admin123!</span>
              </div>
              <div className="flex justify-between">
                <span className="text-foreground/70">Profesor:</span>
                <span>prof.garcia@academia.com / Profesor123!</span>
              </div>
              <div className="flex justify-between">
                <span className="text-foreground/70">Estudiante:</span>
                <span>juan.perez@academia.com / Estudiante123!</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
