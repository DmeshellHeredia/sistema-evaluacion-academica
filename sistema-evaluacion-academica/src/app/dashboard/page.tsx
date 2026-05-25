"use client"

import Link from "next/link"
import { useQuery } from "@tanstack/react-query"
import {
  Users,
  BookOpen,
  TrendingUp,
  GraduationCap,
  Clock,
  Users2,
  CalendarDays,
  ArrowRight,
  Star,
} from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { studentsApi, professorsApi, subjectsApi, sectionsApi, periodsApi, gradesApi } from "@/lib/api"
import { StatsCard } from "@/components/stats-card"
import { GradeBadge } from "@/components/grade-badge"
import { ErrorState } from "@/components/error-state"
import { gradeCategory } from "@/lib/grades"
import type { SectionDto } from "@/types"
import { DAY_LABELS } from "@/types"

export default function DashboardPage() {
  const { user } = useAuth()

  return (
    <div className="space-y-6">
      <div className="rounded-2xl bg-linear-to-r from-primary to-primary/80 p-6 text-primary-foreground shadow-lg shadow-primary/20">
        <div className="flex items-center gap-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/20">
            <GraduationCap className="h-6 w-6" />
          </div>
          <div>
            <h2 className="text-xl font-bold">
              ¡Bienvenido, {user?.fullName?.split(" ")[0]}!
            </h2>
            <p className="text-primary-foreground/80 text-sm">
              {user?.role === "Admin" && "Panel de administración del sistema académico"}
              {user?.role === "Profesor" && "Panel de gestión de tus secciones y calificaciones"}
              {user?.role === "Estudiante" && "Consulta tu progreso académico"}
            </p>
          </div>
        </div>
      </div>

      {user?.role === "Admin" && <AdminDashboard />}
      {user?.role === "Profesor" && <ProfesorDashboard />}
      {user?.role === "Estudiante" && <EstudianteDashboard />}
    </div>
  )
}

function AdminDashboard() {
  const { data: students, isLoading: isLoadingStudents } = useQuery({
    queryKey: ["students", 1, 10],
    queryFn: () => studentsApi.getAll(1, 10),
  })
  const { data: professors, isLoading: isLoadingProfessors } = useQuery({
    queryKey: ["professors", 1, 1],
    queryFn: () => professorsApi.getAll(1, 1),
  })
  const { data: subjects, isLoading: isLoadingSubjects } = useQuery({
    queryKey: ["subjects", 1, 1],
    queryFn: () => subjectsApi.getAll(1, 1),
    staleTime: 60_000,
  })
  const { data: sections, isLoading: isLoadingSections } = useQuery({
    queryKey: ["sections", 1, 1],
    queryFn: () => sectionsApi.getAll(1, 1),
    staleTime: 60_000,
  })
  const { data: periods, isLoading: isLoadingPeriods } = useQuery({
    queryKey: ["periods"],
    queryFn: periodsApi.getAll,
  })

  const isLoading = isLoadingStudents || isLoadingProfessors || isLoadingSubjects || isLoadingSections || isLoadingPeriods

  if (isLoading) {
    return (
      <>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="bg-card border border-border rounded-2xl p-5 shadow-sm animate-pulse">
              <div className="flex items-start justify-between">
                <div className="space-y-2 flex-1">
                  <div className="h-3 w-24 bg-muted rounded" />
                  <div className="h-7 w-12 bg-muted rounded" />
                  <div className="h-3 w-20 bg-muted rounded" />
                </div>
                <div className="h-10 w-10 rounded-xl bg-muted shrink-0" />
              </div>
            </div>
          ))}
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="flex items-center gap-3 p-4 rounded-2xl border border-border bg-card animate-pulse">
              <div className="h-9 w-9 rounded-xl bg-muted shrink-0" />
              <div className="flex-1 space-y-1.5">
                <div className="h-3.5 w-3/4 bg-muted rounded" />
                <div className="h-3 w-1/2 bg-muted rounded" />
              </div>
            </div>
          ))}
        </div>
      </>
    )
  }

  const activePeriod = (periods ?? []).find((p) => p.isEnrollmentOpen)

  const quickLinks = [
    { href: "/dashboard/students", label: "Gestionar Estudiantes", icon: Users, desc: `${students?.totalCount ?? 0} registrados` },
    { href: "/dashboard/professors", label: "Gestionar Profesores", icon: Users, desc: `${professors?.totalCount ?? 0} registrados` },
    { href: "/dashboard/subjects", label: "Gestionar Materias", icon: BookOpen, desc: `${subjects?.totalCount ?? 0} activas` },
    { href: "/dashboard/sections", label: "Gestionar Secciones", icon: CalendarDays, desc: `${sections?.totalCount ?? 0} secciones` },
    { href: "/dashboard/periods", label: "Períodos Académicos", icon: CalendarDays, desc: activePeriod ? `Activo: ${activePeriod.name}` : "Sin período activo" },
    { href: "/dashboard/grades/averages", label: "Ver Promedios", icon: TrendingUp, desc: "Rendimiento general" },
  ]

  return (
    <>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatsCard title="Estudiantes" value={students?.totalCount ?? 0} subtitle="Alumnos registrados" icon={Users} color="indigo" />
        <StatsCard title="Profesores" value={professors?.totalCount ?? 0} subtitle="Profesores activos" icon={Users} color="blue" />
        <StatsCard title="Materias" value={subjects?.totalCount ?? 0} subtitle="Materias activas" icon={BookOpen} color="violet" />
        <StatsCard title="Secciones" value={sections?.totalCount ?? 0} subtitle="Secciones abiertas" icon={CalendarDays} color="emerald" />
      </div>

      {activePeriod && (
        <div className="bg-emerald-500/10 border border-emerald-500/30 rounded-2xl px-5 py-4 flex items-center justify-between">
          <div>
            <p className="text-sm font-semibold text-emerald-700 dark:text-emerald-400">Período activo: {activePeriod.name}</p>
            <p className="text-xs text-emerald-600/70 dark:text-emerald-500/70 mt-0.5">
              {activePeriod.isEnrollmentOpen ? "Inscripciones abiertas" : "Inscripciones cerradas"}
            </p>
          </div>
          <Link
            href="/dashboard/periods"
            className="flex items-center gap-1 text-xs font-medium text-emerald-700 dark:text-emerald-400 hover:underline"
          >
            Administrar <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>
      )}

      <div>
        <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">Acceso rápido</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {quickLinks.map((item) => {
            const Icon = item.icon
            return (
              <Link
                key={item.href}
                href={item.href}
                className="flex items-center gap-3 p-4 rounded-2xl border border-border bg-card hover:border-primary/40 hover:bg-primary/5 transition-all cursor-pointer group"
              >
                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/10 shrink-0 group-hover:bg-primary/20 transition-colors">
                  <Icon className="h-4 w-4 text-primary" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-foreground">{item.label}</p>
                  <p className="text-xs text-muted-foreground truncate">{item.desc}</p>
                </div>
                <ArrowRight className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors shrink-0" />
              </Link>
            )
          })}
        </div>
      </div>
    </>
  )
}

function ProfesorDashboard() {
  const {
    data: sections = [],
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ["sections", "my"],
    queryFn: sectionsApi.getMy,
  })

  const totalStudents = sections.reduce((sum, s) => sum + s.enrolledCount, 0)
  const totalCapacity = sections.reduce((sum, s) => sum + s.capacity, 0)

  if (isLoading) {
    return (
      <>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="bg-card border border-border rounded-2xl p-5 shadow-sm animate-pulse">
              <div className="flex items-start justify-between">
                <div className="space-y-2 flex-1">
                  <div className="h-3 w-24 bg-muted rounded" />
                  <div className="h-7 w-12 bg-muted rounded" />
                  <div className="h-3 w-20 bg-muted rounded" />
                </div>
                <div className="h-10 w-10 rounded-xl bg-muted shrink-0" />
              </div>
            </div>
          ))}
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="bg-card border border-border rounded-2xl p-4 flex flex-col gap-3 animate-pulse">
              <div className="flex items-start justify-between gap-2">
                <div className="space-y-1.5 flex-1">
                  <div className="h-3.5 w-3/4 bg-muted rounded" />
                  <div className="h-3 w-1/2 bg-muted rounded" />
                </div>
                <div className="h-5 w-16 rounded-full bg-muted shrink-0" />
              </div>
              <div className="h-3 w-2/3 bg-muted rounded" />
              <div className="h-1.5 rounded-full bg-muted" />
            </div>
          ))}
        </div>
      </>
    )
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />
  }

  if (sections.length === 0) {
    return (
      <div className="bg-card border border-border rounded-2xl p-16 text-center">
        <BookOpen className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
        <p className="text-sm font-semibold text-foreground">Sin secciones asignadas</p>
        <p className="text-xs text-muted-foreground mt-1">Contacta al administrador para que te asigne secciones.</p>
      </div>
    )
  }

  return (
    <>
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <StatsCard title="Mis Secciones" value={sections.length} subtitle="Secciones activas" icon={BookOpen} color="blue" />
        <StatsCard title="Estudiantes" value={totalStudents} subtitle="Inscritos en mis secciones" icon={Users} color="indigo" />
        <StatsCard title="Ocupación" value={totalCapacity > 0 ? `${Math.round((totalStudents / totalCapacity) * 100)}%` : "—"} subtitle="Capacidad promedio" icon={TrendingUp} color="emerald" />
      </div>

      <div>
        <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">Mis secciones</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {sections.map((section) => (
            <SectionCard key={section.id} section={section} />
          ))}
        </div>
      </div>
    </>
  )
}

function EstudianteDashboard() {
  const {
    data: studentData,
    isLoading: loadingStudent,
    isError: studentError,
    error: studentErr,
    refetch: refetchStudent,
  } = useQuery({
    queryKey: ["student", "me"],
    queryFn: studentsApi.me,
  })

  const {
    data: gradesData,
    isLoading: loadingGrades,
  } = useQuery({
    queryKey: ["grades", "student", studentData?.id, "dashboard"],
    queryFn: () => gradesApi.getByStudent(studentData!.id, 1, 100),
    enabled: !!studentData?.id,
  })

  const grades = gradesData?.items ?? []

  if (loadingStudent) {
    return (
      <>
        <div className="bg-card border border-border rounded-2xl p-5 shadow-sm animate-pulse">
          <div className="flex items-center gap-4">
            <div className="h-14 w-14 rounded-2xl bg-muted shrink-0" />
            <div className="flex-1 space-y-2">
              <div className="h-4 w-40 bg-muted rounded" />
              <div className="h-3 w-56 bg-muted rounded" />
              <div className="h-3 w-24 bg-muted rounded" />
            </div>
          </div>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="bg-card border border-border rounded-2xl p-5 shadow-sm animate-pulse">
              <div className="flex items-start justify-between">
                <div className="space-y-2 flex-1">
                  <div className="h-3 w-24 bg-muted rounded" />
                  <div className="h-7 w-12 bg-muted rounded" />
                  <div className="h-3 w-20 bg-muted rounded" />
                </div>
                <div className="h-10 w-10 rounded-xl bg-muted shrink-0" />
              </div>
            </div>
          ))}
        </div>
      </>
    )
  }

  if (studentError) {
    return <ErrorState error={studentErr} onRetry={() => void refetchStudent()} />
  }

  const approved = grades.filter((g) => g.value >= 7)
  const failed = grades.filter((g) => g.value < 7)
  const average =
    grades.length > 0
      ? grades.reduce((sum, g) => sum + g.value, 0) / grades.length
      : null

  const quickLinks = [
    { href: "/dashboard/catalog", label: "Mis Materias", icon: BookOpen, desc: "Ver materias y estado del pensum" },
    { href: "/dashboard/grades", label: "Mis Calificaciones", icon: Star, desc: "Historial de calificaciones" },
  ]

  return (
    <>
      <div className="bg-card border border-border rounded-2xl p-5 shadow-sm">
        <div className="flex items-center gap-4">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/10 shrink-0">
            <GraduationCap className="h-7 w-7 text-primary" />
          </div>
          <div>
            <p className="font-semibold text-foreground">{studentData?.fullName}</p>
            <p className="text-sm text-muted-foreground">{studentData?.career} · Semestre {studentData?.semester}</p>
            <p className="text-xs text-muted-foreground mt-0.5">{studentData?.studentCode}</p>
          </div>
          {average !== null && (
            <div className="ml-auto">
              <GradeBadge category={gradeCategory(average)} value={average} />
            </div>
          )}
        </div>
      </div>

      {!loadingGrades && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatsCard
            title="Promedio"
            value={average !== null ? average.toFixed(1) : "—"}
            subtitle={average !== null ? "Promedio general" : "Sin calificaciones"}
            icon={TrendingUp}
            color="indigo"
          />
          <StatsCard
            title="Aprobadas"
            value={approved.length}
            subtitle="Materias aprobadas"
            icon={GraduationCap}
            color="emerald"
          />
          <StatsCard
            title="Reprobadas"
            value={failed.length}
            subtitle="Materias reprobadas"
            icon={BookOpen}
            color={failed.length > 0 ? "red" : "blue"}
          />
        </div>
      )}

      <div>
        <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">Mi espacio académico</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {quickLinks.map((item) => {
            const Icon = item.icon
            return (
              <Link
                key={item.href}
                href={item.href}
                className="flex items-center gap-3 p-4 rounded-2xl border border-border bg-card hover:border-primary/40 hover:bg-primary/5 transition-all cursor-pointer group"
              >
                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/10 shrink-0 group-hover:bg-primary/20 transition-colors">
                  <Icon className="h-4 w-4 text-primary" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-foreground">{item.label}</p>
                  <p className="text-xs text-muted-foreground">{item.desc}</p>
                </div>
                <ArrowRight className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors shrink-0" />
              </Link>
            )
          })}
        </div>
      </div>
    </>
  )
}

function SectionCard({ section }: { section: SectionDto }) {
  const occupancy = section.capacity > 0 ? Math.round((section.enrolledCount / section.capacity) * 100) : 0
  const barColor = occupancy >= 90 ? "bg-red-500" : occupancy >= 70 ? "bg-amber-500" : "bg-emerald-500"

  return (
    <Link
      href={`/dashboard/courses/${section.id}`}
      className="bg-card border border-border rounded-2xl p-4 flex flex-col gap-3 hover:border-primary/40 hover:bg-primary/5 hover:shadow-md transition-all cursor-pointer"
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-foreground text-sm leading-snug truncate">{section.subjectName}</p>
          <p className="text-xs text-muted-foreground mt-0.5">{section.subjectCode} · Sec. {section.sectionCode}</p>
        </div>
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-primary/10 text-primary shrink-0">
          {section.modality}
        </span>
      </div>

      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <Clock className="h-3.5 w-3.5 shrink-0" />
        <span>{DAY_LABELS[section.dayOfWeek]} · {section.startTime}–{section.endTime}</span>
      </div>

      <div className="space-y-1">
        <div className="flex items-center justify-between text-xs">
          <div className="flex items-center gap-1 text-muted-foreground">
            <Users2 className="h-3.5 w-3.5" />
            <span>{section.enrolledCount}/{section.capacity} estudiantes</span>
          </div>
          <span className={`font-semibold ${occupancy >= 90 ? "text-red-500" : occupancy >= 70 ? "text-amber-500" : "text-emerald-500"}`}>
            {occupancy}%
          </span>
        </div>
        <div className="h-1.5 rounded-full bg-muted overflow-hidden">
          <div className={`h-full rounded-full ${barColor} transition-all duration-300`} style={{ width: `${occupancy}%` }} />
        </div>
      </div>
    </Link>
  )
}

