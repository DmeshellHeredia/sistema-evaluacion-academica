"use client"

import { useState, useRef, useEffect } from "react"
import Link from "next/link"
import { Menu, Bell, User, Star, Settings, LogOut, ChevronDown } from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { usePathname } from "next/navigation"

const pageTitles: Record<string, string> = {
  "/dashboard": "Tablero",
  "/dashboard/students": "Estudiantes",
  "/dashboard/professors": "Profesores",
  "/dashboard/subjects": "Materias",
  "/dashboard/sections": "Secciones",
  "/dashboard/periods": "Períodos",
  "/dashboard/grades": "Calificaciones",
  "/dashboard/grades/averages": "Promedios",
  "/dashboard/courses": "Mis Cursos",
  "/dashboard/catalog": "Mis Materias",
  "/dashboard/schedule": "Mi Horario",
  "/dashboard/profile": "Mi Perfil",
  "/dashboard/preferences": "Preferencias",
}

interface HeaderProps {
  onMenuClick: () => void
}

export function Header({ onMenuClick }: HeaderProps) {
  const { user, logout } = useAuth()
  const pathname = usePathname()

  const getTitle = () => {
    if (pageTitles[pathname]) return pageTitles[pathname]
    if (pathname.startsWith("/dashboard/courses/")) return "Curso"
    if (pathname.startsWith("/dashboard/students/")) return "Estudiante"
    return "Academia"
  }

  const [bellOpen, setBellOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const bellRef = useRef<HTMLDivElement>(null)
  const profileRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (bellRef.current && !bellRef.current.contains(e.target as Node)) setBellOpen(false)
      if (profileRef.current && !profileRef.current.contains(e.target as Node)) setProfileOpen(false)
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [])

  return (
    <header className="sticky top-0 z-20 flex h-16 items-center gap-4 border-b border-border bg-card/80 backdrop-blur-sm px-4 md:px-6">
      <button
        onClick={onMenuClick}
        aria-label="Abrir menú"
        className="flex h-9 w-9 items-center justify-center rounded-lg border border-border hover:bg-muted transition-colors lg:hidden cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <Menu className="h-5 w-5 text-muted-foreground" />
      </button>

      <div className="flex-1">
        <h1 className="text-lg font-semibold text-foreground">{getTitle()}</h1>
      </div>

      <div className="flex items-center gap-2">
        {/* Bell */}
        <div ref={bellRef} className="relative">
          <button
            onClick={() => { setBellOpen((o) => !o); setProfileOpen(false) }}
            aria-label="Notificaciones"
            aria-expanded={bellOpen}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-border hover:bg-muted transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <Bell className="h-4 w-4 text-muted-foreground" />
          </button>
          {bellOpen && (
            <div className="absolute right-0 top-11 z-30 w-64 rounded-xl border border-border bg-card shadow-lg shadow-black/10 overflow-hidden">
              <div className="px-4 py-2.5 border-b border-border">
                <p className="text-xs font-semibold text-foreground">Notificaciones</p>
              </div>
              <div className="px-4 py-6 text-center">
                <p className="text-xs text-muted-foreground">Sin notificaciones</p>
              </div>
            </div>
          )}
        </div>

        {/* Profile dropdown */}
        <div ref={profileRef} className="relative">
          <button
            onClick={() => { setProfileOpen((o) => !o); setBellOpen(false) }}
            aria-label="Perfil de usuario"
            aria-expanded={profileOpen}
            className="flex h-9 items-center gap-2.5 px-3 rounded-lg border border-border bg-background hover:bg-muted transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <div className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-primary-foreground text-xs font-bold">
              {user?.fullName?.charAt(0) ?? "?"}
            </div>
            <span className="text-sm font-medium text-foreground hidden sm:block">
              {user?.fullName?.split(" ")[0]}
            </span>
            <ChevronDown className={`h-3.5 w-3.5 text-muted-foreground transition-transform ${profileOpen ? "rotate-180" : ""}`} />
          </button>

          {profileOpen && (
            <div className="absolute right-0 top-11 z-30 w-52 rounded-xl border border-border bg-card shadow-lg shadow-black/10 overflow-hidden">
              <div className="px-4 py-3 border-b border-border">
                <p className="text-sm font-semibold text-foreground truncate">{user?.fullName}</p>
                <p className="text-xs text-muted-foreground truncate">{user?.email}</p>
              </div>
              <div className="py-1">
                <Link
                  href="/dashboard/profile"
                  onClick={() => setProfileOpen(false)}
                  className="flex items-center gap-2.5 px-4 py-2 text-sm text-foreground hover:bg-muted transition-colors cursor-pointer"
                >
                  <User className="h-4 w-4 text-muted-foreground" />
                  Mi Perfil
                </Link>
                {(user?.role === "Profesor" || user?.role === "Estudiante") && (
                  <Link
                    href="/dashboard/grades"
                    onClick={() => setProfileOpen(false)}
                    className="flex items-center gap-2.5 px-4 py-2 text-sm text-foreground hover:bg-muted transition-colors cursor-pointer"
                  >
                    <Star className="h-4 w-4 text-muted-foreground" />
                    {user.role === "Estudiante" ? "Mis Calificaciones" : "Calificaciones"}
                  </Link>
                )}
                <Link
                  href="/dashboard/preferences"
                  onClick={() => setProfileOpen(false)}
                  className="flex items-center gap-2.5 px-4 py-2 text-sm text-foreground hover:bg-muted transition-colors cursor-pointer"
                >
                  <Settings className="h-4 w-4 text-muted-foreground" />
                  Preferencias
                </Link>
              </div>
              <div className="border-t border-border py-1">
                <button
                  onClick={() => { setProfileOpen(false); logout() }}
                  className="flex w-full items-center gap-2.5 px-4 py-2 text-sm text-red-500 hover:bg-red-500/10 transition-colors cursor-pointer"
                >
                  <LogOut className="h-4 w-4" />
                  Cerrar sesión
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
