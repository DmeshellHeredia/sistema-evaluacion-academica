"use client"

import Link from "next/link"
import { useQuery } from "@tanstack/react-query"
import { BookOpen, Clock, GraduationCap, ArrowRight } from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { sectionsApi, enrollmentsApi } from "@/lib/api"
import { ErrorState } from "@/components/error-state"
import type { DayOfWeekType } from "@/types"
import { DAY_LABELS } from "@/types"

export default function CoursesPage() {
  const { user } = useAuth()

  if (user?.role === "Profesor") return <ProfesorCourses />
  if (user?.role === "Estudiante") return <EstudianteCourses />
  return (
    <div className="p-8 text-center text-muted-foreground">
      Como administrador, gestiona las secciones desde{" "}
      <Link href="/dashboard/sections" className="underline">Secciones</Link>.
    </div>
  )
}

function ProfesorCourses() {
  const { data: sections = [], isLoading, isError, error, refetch } = useQuery({
    queryKey: ["sections", "my"],
    queryFn: sectionsApi.getMy,
  })

  if (isLoading)
    return <CourseGridSkeleton />
  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-foreground">Mis Cursos</h2>
        <p className="text-sm text-muted-foreground mt-1">Secciones que tienes asignadas</p>
      </div>
      {sections.length === 0 ? (
        <EmptyState message="Sin secciones asignadas" sub="Contacta al administrador." />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {sections.map((s) => (
            <CourseCard
              key={s.id}
              sectionId={s.id}
              subjectCode={s.subjectCode}
              subjectName={s.subjectName}
              sectionCode={s.sectionCode}
              day={s.dayOfWeek}
              start={s.startTime}
              end={s.endTime}
              modality={s.modality}
              enrolled={s.enrolledCount}
              capacity={s.capacity}
              credits={undefined}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function EstudianteCourses() {
  const { data: schedule, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["my-schedule"],
    queryFn: enrollmentsApi.getMySchedule,
  })

  if (isLoading)
    return <CourseGridSkeleton />
  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />

  const sections = schedule?.sections ?? []

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-foreground">Mis Cursos</h2>
        <p className="text-sm text-muted-foreground mt-1">Materias en las que estás inscrito</p>
      </div>
      {sections.length === 0 ? (
        <EmptyState message="Sin materias inscritas" sub="Inscríbete en Mis Materias para ver tus cursos aquí." />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {sections.map((s) => (
            <CourseCard
              key={s.sectionId}
              sectionId={s.sectionId}
              subjectCode={s.subjectCode}
              subjectName={s.subjectName}
              sectionCode={s.sectionCode}
              day={s.dayOfWeek}
              start={s.startTime}
              end={s.endTime}
              modality={s.modality}
              credits={s.credits}
            />
          ))}
        </div>
      )}
    </div>
  )
}

interface CourseCardProps {
  sectionId: string
  subjectCode: string
  subjectName: string
  sectionCode: string
  day: DayOfWeekType
  start: string
  end: string
  modality: string
  credits?: number
  enrolled?: number
  capacity?: number
}

function CourseCard({ sectionId, subjectCode, subjectName, sectionCode, day, start, end, modality, credits, enrolled, capacity }: CourseCardProps) {
  const occupancy = (capacity != null && capacity > 0 && enrolled != null) ? Math.round((enrolled / capacity) * 100) : null
  const barColor = occupancy != null ? (occupancy >= 90 ? "bg-red-500" : occupancy >= 70 ? "bg-amber-500" : "bg-emerald-500") : "bg-primary"

  return (
    <Link
      href={`/dashboard/courses/${sectionId}`}
      className="group flex flex-col gap-3 p-5 rounded-2xl border border-border bg-card hover:border-primary/40 hover:bg-primary/5 transition-all shadow-sm hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 shrink-0 group-hover:bg-primary/20 transition-colors">
          <BookOpen className="h-5 w-5 text-primary" />
        </div>
        <span className="text-xs font-medium px-2 py-0.5 rounded-full bg-primary/10 text-primary">{modality}</span>
      </div>
      <div>
        <p className="font-semibold text-foreground text-sm leading-snug">{subjectName}</p>
        <p className="text-xs text-muted-foreground mt-0.5">{subjectCode} · Sec. {sectionCode}</p>
      </div>
      <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
        <Clock className="h-3.5 w-3.5 shrink-0" />
        <span>{DAY_LABELS[day]} · {start}–{end}</span>
      </div>
      {credits != null && (
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <GraduationCap className="h-3.5 w-3.5 shrink-0" />
          <span>{credits} créditos</span>
        </div>
      )}
      {occupancy != null && (
        <div className="space-y-1">
          <div className="flex justify-between text-xs">
            <span className="text-muted-foreground">{enrolled}/{capacity} inscritos</span>
            <span className={`font-semibold ${occupancy >= 90 ? "text-red-500" : occupancy >= 70 ? "text-amber-500" : "text-emerald-500"}`}>{occupancy}%</span>
          </div>
          <div className="h-1.5 rounded-full bg-muted overflow-hidden">
            <div className={`h-full rounded-full ${barColor}`} style={{ width: `${occupancy}%` }} />
          </div>
        </div>
      )}
      <div className="flex items-center justify-end gap-1 text-xs font-medium text-primary">
        Entrar al curso <ArrowRight className="h-3.5 w-3.5" />
      </div>
    </Link>
  )
}

function CourseGridSkeleton() {
  return (
    <div className="space-y-6">
      <div className="space-y-1 animate-pulse">
        <div className="h-6 w-32 bg-muted rounded" />
        <div className="h-3 w-56 bg-muted rounded" />
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="flex flex-col gap-3 p-5 rounded-2xl border border-border bg-card shadow-sm animate-pulse">
            <div className="flex items-start justify-between gap-2">
              <div className="h-10 w-10 rounded-xl bg-muted shrink-0" />
              <div className="h-5 w-16 rounded-full bg-muted" />
            </div>
            <div className="space-y-1.5">
              <div className="h-3.5 w-3/4 bg-muted rounded" />
              <div className="h-3 w-1/2 bg-muted rounded" />
            </div>
            <div className="h-3 w-2/3 bg-muted rounded" />
          </div>
        ))}
      </div>
    </div>
  )
}

function EmptyState({ message, sub }: { message: string; sub: string }) {
  return (
    <div className="bg-card border border-border rounded-2xl p-16 text-center">
      <BookOpen className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
      <p className="text-sm font-semibold text-foreground">{message}</p>
      <p className="text-xs text-muted-foreground mt-1">{sub}</p>
    </div>
  )
}
