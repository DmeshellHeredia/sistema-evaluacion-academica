"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import {
  GraduationCap,
  Users,
  BookOpen,
  Star,
  BarChart3,
  X,
  CalendarDays,
} from "lucide-react"
import { cn } from "@/lib/utils"
import { useAuth } from "@/context/auth-context"

interface NavItem {
  href: string
  label: string
  icon: React.ElementType
  roles: string[]
  exact?: boolean
}

const navItems: NavItem[] = [
  // All roles
  {
    href: "/dashboard",
    label: "Tablero",
    icon: BarChart3,
    roles: ["Admin", "Profesor", "Estudiante"],
    exact: true,
  },
  // Admin only
  {
    href: "/dashboard/students",
    label: "Estudiantes",
    icon: Users,
    roles: ["Admin"],
  },
  {
    href: "/dashboard/professors",
    label: "Profesores",
    icon: Users,
    roles: ["Admin"],
  },
  {
    href: "/dashboard/subjects",
    label: "Materias",
    icon: BookOpen,
    roles: ["Admin"],
  },
  {
    href: "/dashboard/sections",
    label: "Secciones",
    icon: CalendarDays,
    roles: ["Admin"],
  },
  {
    href: "/dashboard/periods",
    label: "Períodos",
    icon: CalendarDays,
    roles: ["Admin"],
  },
  // Profesor only
  {
    href: "/dashboard/courses",
    label: "Mis Cursos",
    icon: BookOpen,
    roles: ["Profesor"],
  },
  {
    href: "/dashboard/grades",
    label: "Calificaciones",
    icon: Star,
    roles: ["Profesor"],
    exact: true,
  },
  // Estudiante only
  {
    href: "/dashboard/catalog",
    label: "Mis Materias",
    icon: GraduationCap,
    roles: ["Estudiante"],
  },
  {
    href: "/dashboard/schedule",
    label: "Mi Horario",
    icon: CalendarDays,
    roles: ["Estudiante"],
  },
  {
    href: "/dashboard/courses",
    label: "Mis Cursos",
    icon: BookOpen,
    roles: ["Estudiante"],
  },
  {
    href: "/dashboard/grades",
    label: "Mis Calificaciones",
    icon: Star,
    roles: ["Estudiante"],
    exact: true,
  },
]

interface SidebarProps {
  onClose?: () => void
}

export function Sidebar({ onClose }: SidebarProps) {
  const pathname = usePathname()
  const { user } = useAuth()

  const filtered = navItems.filter(
    (item) => !user?.role || item.roles.includes(user.role)
  )

  const roleColors: Record<string, string> = {
    Admin: "bg-violet-500/20 text-violet-300",
    Profesor: "bg-blue-500/20 text-blue-300",
    Estudiante: "bg-emerald-500/20 text-emerald-300",
  }

  return (
    <div className="flex h-full flex-col bg-sidebar text-sidebar-foreground">
      {/* Header */}
      <div className="flex items-center justify-between px-5 py-5 border-b border-sidebar-border">
        <Link href="/dashboard" className="flex items-center gap-2.5 cursor-pointer">
          <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-sidebar-primary shadow-lg shadow-primary/30">
            <GraduationCap className="h-4 w-4 text-sidebar-primary-foreground" />
          </div>
          <span className="text-base font-bold text-sidebar-foreground">Academia</span>
        </Link>
        {/* Mobile close */}
        {onClose && (
          <button
            onClick={onClose}
            aria-label="Cerrar menú"
            className="p-1 rounded-lg hover:bg-sidebar-accent text-sidebar-foreground/60 hover:text-sidebar-foreground transition-colors cursor-pointer lg:hidden focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-primary"
          >
            <X className="h-5 w-5" />
          </button>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-1">
        {filtered.map((item) => {
          const Icon = item.icon
          const isActive = item.exact
            ? pathname === item.href
            : pathname === item.href || pathname.startsWith(item.href + "/")
          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={onClose}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-primary",
                isActive
                  ? "bg-sidebar-primary text-sidebar-primary-foreground shadow-lg shadow-primary/20"
                  : "text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              {item.label}
            </Link>
          )
        })}
      </nav>

      {/* User footer */}
      <div className="px-3 py-4 border-t border-sidebar-border">
        {/* Role badge */}
        <div className="flex items-center gap-3 px-3 py-2.5 rounded-xl bg-sidebar-accent/50">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-sidebar-primary/20 text-sidebar-primary text-xs font-bold shrink-0">
            {user?.fullName?.charAt(0) ?? "?"}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-sidebar-foreground truncate">
              {user?.fullName ?? "Usuario"}
            </p>
            <span
              className={cn(
                "inline-block text-xs font-medium px-2 py-0.5 rounded-full",
                roleColors[user?.role ?? ""] ?? "bg-muted text-muted-foreground"
              )}
            >
              {user?.role}
            </span>
          </div>
        </div>
      </div>
    </div>
  )
}
