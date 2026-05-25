"use client"

import { useQuery } from "@tanstack/react-query"
import { CalendarDays, Clock, MapPin, Loader2, BookOpen } from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { enrollmentsApi } from "@/lib/api"
import { ErrorState } from "@/components/error-state"
import type { EnrolledSectionDto, DayOfWeekType } from "@/types"
import { DAY_LABELS } from "@/types"

const DAYS: DayOfWeekType[] = ["Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado"]

const PALETTE = [
  { bg: "bg-blue-500/15 border-blue-500/30",    text: "text-blue-700 dark:text-blue-300"    },
  { bg: "bg-violet-500/15 border-violet-500/30", text: "text-violet-700 dark:text-violet-300" },
  { bg: "bg-emerald-500/15 border-emerald-500/30", text: "text-emerald-700 dark:text-emerald-300" },
  { bg: "bg-amber-500/15 border-amber-500/30",   text: "text-amber-700 dark:text-amber-300"  },
  { bg: "bg-rose-500/15 border-rose-500/30",     text: "text-rose-700 dark:text-rose-300"    },
  { bg: "bg-cyan-500/15 border-cyan-500/30",     text: "text-cyan-700 dark:text-cyan-300"    },
  { bg: "bg-orange-500/15 border-orange-500/30", text: "text-orange-700 dark:text-orange-300" },
  { bg: "bg-pink-500/15 border-pink-500/30",     text: "text-pink-700 dark:text-pink-300"    },
]

export default function SchedulePage() {
  const { user } = useAuth()

  const {
    data: schedule,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ["my-schedule"],
    queryFn: enrollmentsApi.getMySchedule,
    enabled: user?.role === "Estudiante",
  })

  if (user?.role !== "Estudiante") {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center space-y-3">
        <CalendarDays className="h-12 w-12 text-muted-foreground" />
        <p className="text-sm text-muted-foreground">Solo disponible para estudiantes.</p>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />
  }

  const sections = schedule?.sections ?? []
  const activeDays = DAYS.filter((d) => sections.some((s) => s.dayOfWeek === d))

  // Asignar color por materia
  const colorMap: Record<string, typeof PALETTE[number]> = {}
  sections.forEach((s) => {
    if (!colorMap[s.subjectId]) {
      const idx = Object.keys(colorMap).length % PALETTE.length
      colorMap[s.subjectId] = PALETTE[idx]
    }
  })

  const totalCredits = sections.reduce((sum, s) => sum + s.credits, 0)

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-lg font-bold text-foreground">Mi Horario</h1>
          {schedule && (
            <p className="text-sm text-muted-foreground mt-0.5">
              {schedule.periodName}
              {schedule.isEnrollmentOpen && (
                <span className="ml-2 inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-green-500/10 text-green-600 dark:text-green-400">
                  Selección abierta
                </span>
              )}
            </p>
          )}
        </div>
        {sections.length > 0 && (
          <div className="flex items-center gap-4 text-sm text-muted-foreground shrink-0">
            <span><span className="font-semibold text-foreground">{sections.length}</span> materias</span>
            <span><span className="font-semibold text-foreground">{totalCredits}</span> créditos</span>
          </div>
        )}
      </div>

      {sections.length === 0 ? (
        <div className="bg-card border border-border rounded-2xl p-16 text-center space-y-3">
          <BookOpen className="h-10 w-10 text-muted-foreground mx-auto" />
          <p className="text-sm font-medium text-foreground">No tienes materias inscritas</p>
          <p className="text-xs text-muted-foreground">Inscríbete en materias desde la página de catálogo.</p>
        </div>
      ) : (
        <>
          <div className="bg-card border border-border rounded-2xl overflow-hidden">
            <div className="overflow-x-auto">
              <div className="min-w-[600px]">
                <div className={`grid border-b border-border`} style={{ gridTemplateColumns: `repeat(${activeDays.length}, 1fr)` }}>
                  {activeDays.map((day) => (
                    <div key={day} className="px-3 py-3 text-center text-xs font-semibold text-muted-foreground uppercase tracking-wide border-r border-border last:border-r-0">
                      {DAY_LABELS[day]}
                    </div>
                  ))}
                </div>

                <div className={`grid min-h-32`} style={{ gridTemplateColumns: `repeat(${activeDays.length}, 1fr)` }}>
                  {activeDays.map((day) => {
                    const daySections = sections.filter((s) => s.dayOfWeek === day)
                    return (
                      <div key={day} className="p-2 space-y-2 border-r border-border last:border-r-0 min-h-32">
                        {daySections.length === 0 ? (
                          <div className="h-full flex items-center justify-center text-xs text-muted-foreground/40">—</div>
                        ) : (
                          daySections.map((s) => (
                            <ScheduleCard key={s.sectionId} section={s} color={colorMap[s.subjectId]} />
                          ))
                        )}
                      </div>
                    )
                  })}
                </div>
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Detalle de materias inscritas</h2>
            <div className="bg-card border border-border rounded-2xl divide-y divide-border overflow-hidden">
              {sections.map((s) => {
                const color = colorMap[s.subjectId]
                return (
                  <div key={s.sectionId} className="flex items-center gap-4 px-4 py-3">
                    <div className={`w-1.5 h-10 rounded-full shrink-0 ${color.bg.split(" ")[0].replace("/15", "").replace("bg-", "bg-")}`} />
                    <div className="flex-1 min-w-0">
                      <p className="font-semibold text-sm text-foreground truncate">{s.subjectName}</p>
                      <p className="text-xs text-muted-foreground">{s.subjectCode} · Sec. {s.sectionCode} · {s.credits} créd.</p>
                    </div>
                    <div className="flex items-center gap-4 text-xs text-muted-foreground shrink-0">
                      <span className="flex items-center gap-1">
                        <CalendarDays className="h-3.5 w-3.5" />
                        {DAY_LABELS[s.dayOfWeek]}
                      </span>
                      <span className="flex items-center gap-1">
                        <Clock className="h-3.5 w-3.5" />
                        {s.startTime}–{s.endTime}
                      </span>
                      {s.classroom && (
                        <span className="flex items-center gap-1">
                          <MapPin className="h-3.5 w-3.5" />
                          {s.classroom}
                        </span>
                      )}
                      <span className="hidden sm:block">{s.modality}</span>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        </>
      )}
    </div>
  )
}

function ScheduleCard({ section, color }: { section: EnrolledSectionDto; color: typeof PALETTE[number] }) {
  return (
    <div className={`rounded-xl border p-2.5 space-y-1 ${color.bg}`}>
      <p className={`text-xs font-bold leading-tight ${color.text}`}>{section.subjectCode}</p>
      <p className={`text-[10px] leading-tight ${color.text} opacity-80`}>{section.subjectName}</p>
      <div className={`flex items-center gap-1 text-[10px] ${color.text} opacity-70 mt-1`}>
        <Clock className="h-2.5 w-2.5" />
        <span>{section.startTime}–{section.endTime}</span>
      </div>
      {section.classroom && (
        <div className={`flex items-center gap-1 text-[10px] ${color.text} opacity-70`}>
          <MapPin className="h-2.5 w-2.5" />
          <span>{section.classroom}</span>
        </div>
      )}
    </div>
  )
}
